namespace Contracts.External.Models;

public class SendTemplatedEmailRequest
{
    public string MessageId { get; set; } = default!;

    public string Source { get; set; } = default!;

    public string Tenant { get; set; } = default!;

    public string ClientServiceName => BuildClientServiceName(Source, Tenant);

    public static string BuildClientServiceName(string source, string? tenant)
    {
        return string.IsNullOrWhiteSpace(tenant) ? source : $"{source}.{tenant}";
    }

    public string EmailType { get; set; } = default!;

    public string ToEmail { get; set; } = default!;
    public string LanguageCode { get; set; } = "en";
    public string? CorrelationId { get; set; }

    public object Content { get; set; } = default!;
}