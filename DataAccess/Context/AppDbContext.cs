using DTO.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context;

public class AppDbContext : DbContext
{
    public DbSet<ClientEntity> Clients { get; set; } = default!;
    public DbSet<NotificationTypeEntity> NotificationTypes { get; set; } = default!;
    public DbSet<EmailSenderEntity> EmailSenders { get; set; } = default!;
    public DbSet<TemplateEntity> Templates { get; set; } = default!;
    public DbSet<DeliveryEntity> Deliveries { get; set; } = default!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("notification");
        modelBuilder.Entity<ClientEntity>().HasIndex(x => x.Source).IsUnique();
        modelBuilder.Entity<NotificationTypeEntity>().HasIndex(x => new { x.ClientId, x.Key }).IsUnique();
        modelBuilder.Entity<NotificationTypeEntity>().HasOne<ClientEntity>().WithMany()
            .HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EmailSenderEntity>().HasIndex(x => new { x.ClientId, x.Tenant }).IsUnique();
        modelBuilder.Entity<EmailSenderEntity>().HasOne<ClientEntity>().WithMany()
            .HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TemplateEntity>()
            .HasIndex(x => new { x.NotificationTypeId, x.Channel, x.Tenant, x.LanguageCode }).IsUnique();
        modelBuilder.Entity<TemplateEntity>().HasOne<NotificationTypeEntity>().WithMany()
            .HasForeignKey(x => x.NotificationTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DeliveryEntity>().HasIndex(x => new { x.MessageId, x.Channel }).IsUnique();
        modelBuilder.Entity<DeliveryEntity>().HasIndex(x => new { x.Source, x.NotificationType });
        modelBuilder.Entity<DeliveryEntity>().HasIndex(x => x.Status);
    }
}
