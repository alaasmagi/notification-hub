namespace Domain;

public record TemplateContent
{
    public Dictionary<string, object?> Properties { get; init; } = new();
}
