using Base.Contracts.DTO;
using Domain;

namespace DTO.Web.Mappers;

public class TemplateDtoMapper : IMapper<TemplateDto, Template>
{
    public TemplateDto? Map(Template? entity)
    {
        return entity == null
            ? null
            : new TemplateDto
            {
                Id = entity.Id,
                NotificationTypeId = entity.NotificationTypeId,
                Channel = entity.Channel,
                Tenant = entity.Tenant,
                LanguageCode = entity.LanguageCode,
                Title = entity.Title,
                Body = entity.Body,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<TemplateDto>? Map(IEnumerable<Template>? entities)
    {
        return entities?.Select(Map).OfType<TemplateDto>().ToList();
    }

    public Template? Map(TemplateDto? entity)
    {
        return entity == null
            ? null
            : new Template
            {
                Id = entity.Id,
                NotificationTypeId = entity.NotificationTypeId,
                Channel = entity.Channel,
                Tenant = entity.Tenant,
                LanguageCode = entity.LanguageCode,
                Title = entity.Title,
                Body = entity.Body,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<Template>? Map(IEnumerable<TemplateDto>? entities)
    {
        return entities?.Select(Map).OfType<Template>().ToList();
    }
}
