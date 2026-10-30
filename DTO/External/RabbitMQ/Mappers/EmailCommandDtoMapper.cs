using System.Text.Json;
using Base.Contracts.DTO;
using Domain;

namespace DTO.External.RabbitMQ.Mappers;

public class EmailCommandDtoMapper : IMapper<EmailCommandDto, SendTemplatedEmailRequest>
{
    public SendTemplatedEmailRequest? Map(EmailCommandDto? entity)
    {
        if (entity?.Content is null) return null;
        var route = entity.RoutingKey.Split('.');
        if (route.Length != 3 || entity.Content.Model.ValueKind != JsonValueKind.Object) return null;
        return new SendTemplatedEmailRequest
        {
            MessageId = entity.MessageId,
            Source = route[0],
            Tenant = route[1],
            NotificationType = route[2],
            ToEmail = entity.Content.To,
            LanguageCode = string.IsNullOrWhiteSpace(entity.Content.LanguageCode) ? "en" : entity.Content.LanguageCode,
            CorrelationId = entity.MessageId,
            ExpiresAt = entity.Content.ExpiresAt,
            Content = ReadObject(entity.Content.Model)
        };
    }

    public EmailCommandDto? Map(SendTemplatedEmailRequest? entity)
    {
        return entity is null ? null : new EmailCommandDto
        {
            RoutingKey = $"{entity.Source}.{entity.Tenant}.{entity.NotificationType}",
            MessageId = entity.MessageId,
            Source = entity.Source,
            Tenant = entity.Tenant,
            Action = entity.NotificationType,
            ContentVersion = "1.0",
            Content = new EmailContentDto
            {
                To = entity.ToEmail,
                LanguageCode = entity.LanguageCode,
                ExpiresAt = entity.ExpiresAt,
                Model = JsonSerializer.SerializeToElement(WriteObject(entity.Content))
            }
        };
    }

    public IEnumerable<SendTemplatedEmailRequest>? Map(IEnumerable<EmailCommandDto>? entities)
        => entities?.Select(Map).OfType<SendTemplatedEmailRequest>().ToList();

    public IEnumerable<EmailCommandDto>? Map(IEnumerable<SendTemplatedEmailRequest>? entities)
        => entities?.Select(Map).OfType<EmailCommandDto>().ToList();

    private static TemplateContent ReadObject(JsonElement element)
    {
        var model = new TemplateContent();
        foreach (var property in element.EnumerateObject())
        {
            var name = property.Name.Length == 0 ? "" : char.ToUpperInvariant(property.Name[0]) + property.Name[1..];
            model.Properties.Add(name, ReadValue(property.Value));
        }
        return model;
    }

    private static object? ReadValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object: return ReadObject(element);
            case JsonValueKind.Array: return element.EnumerateArray().Select(ReadValue).ToList();
            case JsonValueKind.String: return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var integer)) return integer;
                if (element.TryGetDecimal(out var number)) return number;
                return element.GetDouble();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            default: return null;
        }
    }

    private static Dictionary<string, object?> WriteObject(TemplateContent content)
        => content.Properties.ToDictionary(x => JsonNamingPolicy.CamelCase.ConvertName(x.Key), x => WriteValue(x.Value));

    private static object? WriteValue(object? value) => value switch
    {
        TemplateContent content => WriteObject(content),
        List<object?> items => items.Select(WriteValue).ToList(),
        _ => value
    };
}
