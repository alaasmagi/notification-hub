using System.Dynamic;
using Base.Contracts.DTO;
using Domain;

namespace External.RazorLight;

public class EmailTemplateContentMapper : IMapper<ExpandoObject, TemplateContent>
{
    public ExpandoObject? Map(TemplateContent? entity)
    {
        if (entity is null) return null;
        IDictionary<string, object?> model = new ExpandoObject();
        foreach (var property in entity.Properties)
        {
            model[property.Key] = ToTemplate(property.Value);
        }
        return (ExpandoObject)model;
    }

    public TemplateContent? Map(ExpandoObject? entity)
    {
        if (entity is null) return null;
        return new TemplateContent
        {
            Properties = ((IDictionary<string, object?>)entity).ToDictionary(x => x.Key, x => ToDomain(x.Value))
        };
    }

    public IEnumerable<ExpandoObject>? Map(IEnumerable<TemplateContent>? entities)
        => entities?.Select(Map).OfType<ExpandoObject>().ToList();

    public IEnumerable<TemplateContent>? Map(IEnumerable<ExpandoObject>? entities)
        => entities?.Select(Map).OfType<TemplateContent>().ToList();

    private object? ToTemplate(object? value) => value switch
    {
        TemplateContent content => Map(content),
        List<object?> items => items.Select(ToTemplate).ToList(),
        _ => value
    };

    private object? ToDomain(object? value) => value switch
    {
        ExpandoObject content => Map(content),
        List<object?> items => items.Select(ToDomain).ToList(),
        _ => value
    };
}
