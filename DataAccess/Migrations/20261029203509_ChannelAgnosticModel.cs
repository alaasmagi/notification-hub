using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations;

public partial class ChannelAgnosticModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            LOCK TABLE "notification"."Clients", "notification"."SenderIdentities",
                "notification"."Templates", "notification"."Emails" IN ACCESS EXCLUSIVE MODE;

            CREATE TEMP TABLE "ClientMap" ON COMMIT DROP AS
            SELECT "Id" AS "OldId", split_part("ServiceName", '.', 1) AS "Source",
                CASE WHEN strpos("ServiceName", '.') > 0
                    THEN substring("ServiceName" FROM strpos("ServiceName", '.') + 1)
                    ELSE NULL END AS "DerivedTenant"
            FROM "notification"."Clients";

            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM "ClientMap" WHERE "Source" = '' OR "DerivedTenant" = '') THEN
                    RAISE EXCEPTION 'Client consolidation stopped: an empty source or tenant was derived.';
                END IF;
                IF EXISTS (
                    SELECT 1 FROM "notification"."SenderIdentities" s
                    JOIN "ClientMap" m ON m."OldId" = s."ClientId"
                    WHERE m."DerivedTenant" IS NOT NULL AND s."Tenant" <> ''
                        AND s."Tenant" <> m."DerivedTenant"
                ) THEN
                    RAISE EXCEPTION 'Client consolidation stopped: stored sender Tenant conflicts with the client suffix.';
                END IF;
                IF EXISTS (
                    SELECT m."Source" FROM "notification"."Clients" c
                    JOIN "ClientMap" m ON m."OldId" = c."Id"
                    GROUP BY m."Source" HAVING count(DISTINCT c."IsActive") > 1
                ) THEN
                    RAISE EXCEPTION 'Client consolidation stopped: clients for the same source have conflicting IsActive values.';
                END IF;
            END $$;

            CREATE TEMP TABLE "ConsolidatedClients" ON COMMIT DROP AS
            SELECT DISTINCT ON (m."Source")
                CASE WHEN c."ServiceName" = m."Source" THEN c."Id" ELSE gen_random_uuid() END AS "Id",
                m."Source",
                CASE WHEN c."ServiceName" = m."Source" THEN c."DisplayName" ELSE m."Source" END AS "DisplayName",
                c."IsActive", c."CreatedAt", c."CreatedBy", c."UpdatedAt", c."UpdatedBy"
            FROM "notification"."Clients" c JOIN "ClientMap" m ON m."OldId" = c."Id"
            ORDER BY m."Source", (c."ServiceName" = m."Source") DESC, c."CreatedAt", c."Id";

            CREATE TEMP TABLE "SenderMap" ON COMMIT DROP AS
            SELECT s."Id", c."Id" AS "ClientId", coalesce(m."DerivedTenant", s."Tenant") AS "Tenant"
            FROM "notification"."SenderIdentities" s
            JOIN "ClientMap" m ON m."OldId" = s."ClientId"
            JOIN "ConsolidatedClients" c ON c."Source" = m."Source";

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT m."ClientId", m."Tenant"
                    FROM "notification"."SenderIdentities" s JOIN "SenderMap" m ON m."Id" = s."Id"
                    GROUP BY m."ClientId", m."Tenant"
                    HAVING count(DISTINCT ROW(s."FromAddress", s."DisplayName", s."ReplyTo", s."IsActive")) > 1
                ) THEN
                    RAISE EXCEPTION 'Sender consolidation stopped: branding or IsActive conflicts after client consolidation.';
                END IF;
                IF EXISTS (
                    SELECT m."ClientId", s."EmailType", m."Tenant", t."LanguageCode"
                    FROM "notification"."Templates" t
                    JOIN "notification"."SenderIdentities" s ON s."Id" = t."SenderIdentityId"
                    JOIN "SenderMap" m ON m."Id" = s."Id"
                    GROUP BY m."ClientId", s."EmailType", m."Tenant", t."LanguageCode"
                    HAVING count(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Template consolidation stopped: multiple templates would share the new unique key.';
                END IF;
            END $$;

            INSERT INTO "notification"."Clients"
                ("Id", "ServiceName", "DisplayName", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
            SELECT "Id", "Source", "DisplayName", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
            FROM "ConsolidatedClients" c
            WHERE NOT EXISTS (SELECT 1 FROM "notification"."Clients" old WHERE old."Id" = c."Id");
            """);

        migrationBuilder.CreateTable(
            name: "NotificationTypes", schema: "notification",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationTypes", x => x.Id);
                table.ForeignKey("FK_NotificationTypes_Clients_ClientId", x => x.ClientId,
                    principalSchema: "notification", principalTable: "Clients", principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_NotificationTypes_ClientId_Key", "NotificationTypes",
            new[] { "ClientId", "Key" }, schema: "notification", unique: true);
        migrationBuilder.Sql("""
            INSERT INTO "notification"."NotificationTypes"
                ("Id", "ClientId", "Key", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
            SELECT gen_random_uuid(), "ClientId", "EmailType", true, "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
            FROM (
                SELECT DISTINCT ON (m."ClientId", s."EmailType")
                    m."ClientId", s."EmailType", s."CreatedAt", s."CreatedBy", s."UpdatedAt", s."UpdatedBy"
                FROM "notification"."SenderIdentities" s JOIN "SenderMap" m ON m."Id" = s."Id"
                ORDER BY m."ClientId", s."EmailType", s."CreatedAt", s."Id"
            ) types;
            """);

        migrationBuilder.AddColumn<Guid>("NotificationTypeId", "Templates", schema: "notification", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<int>("Channel", "Templates", schema: "notification", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>("Tenant", "Templates", schema: "notification", type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.Sql("""
            UPDATE "notification"."Templates" t SET "NotificationTypeId" = n."Id", "Tenant" = m."Tenant"
            FROM "notification"."SenderIdentities" s
            JOIN "SenderMap" m ON m."Id" = s."Id"
            JOIN "notification"."NotificationTypes" n ON n."ClientId" = m."ClientId" AND n."Key" = s."EmailType"
            WHERE t."SenderIdentityId" = s."Id";
            """);
        migrationBuilder.RenameColumn("SubjectTemplate", "Templates", "Title", schema: "notification");
        migrationBuilder.RenameColumn("HtmlBodyTemplate", "Templates", "Body", schema: "notification");
        migrationBuilder.DropForeignKey("FK_Templates_SenderIdentities_SenderIdentityId", "Templates", schema: "notification");
        migrationBuilder.DropIndex("IX_Templates_SenderIdentityId_LanguageCode", "Templates", schema: "notification");
        migrationBuilder.DropColumn("SenderIdentityId", "Templates", schema: "notification");
        migrationBuilder.AlterColumn<Guid>("NotificationTypeId", "Templates", schema: "notification", type: "uuid",
            nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.AlterColumn<string>("Tenant", "Templates", schema: "notification", type: "character varying(64)", maxLength: 64,
            nullable: false, oldClrType: typeof(string), oldType: "character varying(64)", oldMaxLength: 64, oldNullable: true);
        migrationBuilder.CreateIndex("IX_Templates_NotificationTypeId_Channel_Tenant_LanguageCode", "Templates",
            new[] { "NotificationTypeId", "Channel", "Tenant", "LanguageCode" }, schema: "notification", unique: true);
        migrationBuilder.AddForeignKey("FK_Templates_NotificationTypes_NotificationTypeId", "Templates", "NotificationTypeId",
            "NotificationTypes", schema: "notification", principalSchema: "notification", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

        migrationBuilder.RenameTable("SenderIdentities", schema: "notification", newName: "EmailSenders", newSchema: "notification");
        migrationBuilder.DropIndex("IX_SenderIdentities_ClientId_EmailType_Tenant", "EmailSenders", schema: "notification");
        migrationBuilder.Sql("""
            ALTER TABLE "notification"."EmailSenders" RENAME CONSTRAINT "PK_SenderIdentities" TO "PK_EmailSenders";
            ALTER TABLE "notification"."EmailSenders" RENAME CONSTRAINT "FK_SenderIdentities_Clients_ClientId" TO "FK_EmailSenders_Clients_ClientId";
            UPDATE "notification"."EmailSenders" s SET "ClientId" = m."ClientId", "Tenant" = m."Tenant"
            FROM "SenderMap" m WHERE s."Id" = m."Id";
            DELETE FROM "notification"."EmailSenders" s USING (
                SELECT "Id", row_number() OVER (PARTITION BY "ClientId", "Tenant" ORDER BY "CreatedAt", "Id") AS ordinal
                FROM "notification"."EmailSenders"
            ) duplicates WHERE s."Id" = duplicates."Id" AND duplicates.ordinal > 1;
            DELETE FROM "notification"."Clients" c USING "ClientMap" m, "ConsolidatedClients" target
            WHERE c."Id" = m."OldId" AND target."Source" = m."Source" AND c."Id" <> target."Id";
            """);
        migrationBuilder.DropColumn("EmailType", "EmailSenders", schema: "notification");
        migrationBuilder.CreateIndex("IX_EmailSenders_ClientId_Tenant", "EmailSenders",
            new[] { "ClientId", "Tenant" }, schema: "notification", unique: true);

        migrationBuilder.RenameTable("Emails", schema: "notification", newName: "Deliveries", newSchema: "notification");
        migrationBuilder.RenameColumn("ServiceName", "Deliveries", "Source", schema: "notification");
        migrationBuilder.RenameColumn("EmailType", "Deliveries", "NotificationType", schema: "notification");
        migrationBuilder.RenameColumn("ToRecipients", "Deliveries", "Recipient", schema: "notification");
        migrationBuilder.RenameColumn("Subject", "Deliveries", "Title", schema: "notification");
        migrationBuilder.AddColumn<int>("Channel", "Deliveries", schema: "notification", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>("Error", "Deliveries", schema: "notification", type: "text", nullable: true);
        migrationBuilder.DropColumn("ReplyTo", "Deliveries", schema: "notification");
        migrationBuilder.DropIndex("IX_Emails_MessageId", "Deliveries", schema: "notification");
        migrationBuilder.CreateIndex("IX_Deliveries_MessageId_Channel", "Deliveries",
            new[] { "MessageId", "Channel" }, schema: "notification", unique: true);
        migrationBuilder.RenameIndex("IX_Emails_ServiceName_EmailType", "IX_Deliveries_Source_NotificationType", "Deliveries", schema: "notification");
        migrationBuilder.RenameIndex("IX_Emails_Status", "IX_Deliveries_Status", "Deliveries", schema: "notification");
        migrationBuilder.Sql("""ALTER TABLE "notification"."Deliveries" RENAME CONSTRAINT "PK_Emails" TO "PK_Deliveries";""");

        migrationBuilder.RenameColumn("ServiceName", "Clients", "Source", schema: "notification");
        migrationBuilder.RenameIndex("IX_Clients_ServiceName", "IX_Clients_Source", "Clients", schema: "notification");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Client consolidation, sender deduplication, and removal of delivery ReplyTo are irreversible. Restore the pre-migration backup instead.");
    }
}
