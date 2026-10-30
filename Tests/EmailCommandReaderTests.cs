using System.Text;
using System.Text.Json;
using Domain;
using External.RabbitMQ;
using DTO.External.RabbitMQ;
using DTO.External.RabbitMQ.Mappers;
using External.RazorLight;
using Contracts.External.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests;

public class EmailCommandReaderTests
{
    private const string Reset = """
        {"messageId":"6f1c2e4a-0b7d-4c55-9a3e-2d8f1b0c9e71","source":"identity-hub","tenant":"food-app","action":"password-reset",
        "timestamp":"2026-10-30T12:00:00Z","contentVersion":"1.0",
        "content":{"to":"user@example.com","languageCode":"et","model":{"userName":"Test User","resetLink":"https://identity.example.com/..."}}}
        """;
    private const string Invoice = """
        {"messageId":"0a9b8c7d-6e5f-4a3b-8c2d-1e0f9a8b7c6d","source":"invoice-service","tenant":"invoice-service","action":"monthly-invoice",
        "timestamp":"2026-10-30T12:00:00Z","contentVersion":"1.0",
        "content":{"to":"client@example.com","languageCode":"en","model":{"invoiceNumber":"2026-10","amount":"97.87","currency":"EUR","dueDate":"2026-11-14"}}}
        """;

    [Theory]
    [InlineData(Reset, "identity-hub.food-app.password-reset", "user@example.com", "et", "UserName", "Test User")]
    [InlineData(Invoice, "invoice-service.invoice-service.monthly-invoice", "client@example.com", "en", "InvoiceNumber", "2026-10")]
    public void SuppliedContracts_MapToDomain(string json, string route, string recipient, string language, string field, string value)
    {
        var request = Read(route, json);
        Assert.NotNull(request);
        Assert.True(request.IsValid());
        Assert.Equal(route.Split('.')[0], request.Source);
        Assert.Equal(route.Split('.')[1], request.Tenant);
        Assert.Equal(route.Split('.')[2], request.NotificationType);
        Assert.Equal(recipient, request.ToEmail);
        Assert.Equal(language, request.LanguageCode);
        Assert.Equal(value, request.Content.Properties[field]);
        Assert.Equal(JsonDocument.Parse(json).RootElement.GetProperty("messageId").GetString(), request.MessageId);
        Assert.Equal(request.MessageId, request.CorrelationId);
    }

    [Fact]
    public async Task SuppliedModels_RenderPascalCaseProperties()
    {
        var renderer = new RazorEmailTemplateRenderer();
        foreach (var (json, route, title, body, expected) in new[]
        {
            (Reset, "identity-hub.food-app.password-reset", "Hello @Model.UserName", "<a href=\"@Model.ResetLink\">Reset</a>", "https://identity.example.com/..."),
            (Invoice, "invoice-service.invoice-service.monthly-invoice", "Invoice @Model.InvoiceNumber", "@Model.Amount @Model.Currency due @Model.DueDate", "97.87 EUR due 2026-11-14")
        })
        {
            var request = Read(route, json)!;
            var rendered = await renderer.RenderAsync(new Template { Id = Guid.NewGuid(), Title = title, Body = body },
                new EmailTemplateModel { Content = request.Content, Locale = request.LanguageCode,
                    Fmt = new LocaleFormatter(request.LanguageCode), Branding = new EmailBranding() });
            Assert.Contains(expected, rendered.HtmlBody);
            Assert.DoesNotContain("@Model", rendered.Subject);
        }
    }

    [Theory]
    [InlineData("wrong.food-app.password-reset")]
    [InlineData("identity-hub.wrong.password-reset")]
    [InlineData("identity-hub.food-app.wrong")]
    [InlineData("identity-hub.food-app")]
    public void MismatchedOrMalformedRoute_IsRejected(string route)
        => Assert.Null(Read(route, Reset));

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{broken")]
    public void InvalidEnvelope_IsRejected(string json)
        => Assert.Null(Read("identity-hub.food-app.password-reset", json));

    [Fact]
    public void UnsupportedVersionAndMissingModel_AreRejected()
    {
        Assert.Null(Read("identity-hub.food-app.password-reset", Reset.Replace("1.0", "2.0")));
        Assert.Null(Read("identity-hub.food-app.password-reset", Reset.Replace("\"model\"", "\"missing\"")));
    }

    [Fact]
    public void Mapper_PreservesNestedValuesAndMapsBackToWireNames()
    {
        var dto = JsonSerializer.Deserialize<EmailCommandDto>(Reset)! with
        {
            RoutingKey = "identity-hub.food-app.password-reset",
            Content = new EmailContentDto
            {
                To = "user@example.com",
                Model = JsonSerializer.Deserialize<JsonElement>("""
                    {"rows":[{"amount":12.5,"count":2,"enabled":true,"optional":null}]}
                    """)
            }
        };
        var mapper = new EmailCommandDtoMapper();
        var domain = mapper.Map(dto)!;
        var rows = Assert.IsType<List<object?>>(domain.Content.Properties["Rows"]);
        var row = Assert.IsType<TemplateContent>(rows[0]);
        Assert.Equal(12.5m, row.Properties["Amount"]);
        Assert.Equal(2L, row.Properties["Count"]);
        Assert.Equal(true, row.Properties["Enabled"]);
        Assert.Null(row.Properties["Optional"]);
        var wire = mapper.Map(domain)!;
        Assert.Equal(12.5m, wire.Content!.Model.GetProperty("rows")[0].GetProperty("amount").GetDecimal());
        var templateMapper = new EmailTemplateContentMapper();
        dynamic model = templateMapper.Map(domain.Content)!;
        Assert.Equal(12.5m, (decimal)model.Rows[0].Amount);
        var restored = templateMapper.Map((System.Dynamic.ExpandoObject)model)!;
        Assert.IsType<List<object?>>(restored.Properties["Rows"]);
    }

    private static SendTemplatedEmailRequest? Read(string route, string json)
        => new EmailCommandReader(new EmailCommandDtoMapper(), NullLogger<EmailCommandReader>.Instance)
            .Read(route, Encoding.UTF8.GetBytes(json));
}
