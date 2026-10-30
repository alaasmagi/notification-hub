using Domain;

namespace Contracts.External.Models;

/// <summary>Combines email content, branding and locale formatting for rendering.</summary>
public class EmailTemplateModel
{
    public TemplateContent Content { get; init; } = default!;
    public EmailBranding Branding { get; init; } = default!;
    public LocaleFormatter Fmt { get; init; } = default!;
    public string Locale { get; init; } = default!;
}
