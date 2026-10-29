using Base.Contracts.DTO;
using Domain;

namespace DTO.DataAccess.Mappers;

public class TemplateEntityMapper : IMapper<Template, TemplateEntity>
{
    public Template? Map(TemplateEntity? entity)
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

    public IEnumerable<Template>? Map(IEnumerable<TemplateEntity>? entities)
    {
        return entities?.Select(Map).OfType<Template>().ToList();
    }

    public TemplateEntity? Map(Template? entity)
    {
        return entity == null
            ? null
            : new TemplateEntity
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

    public IEnumerable<TemplateEntity>? Map(IEnumerable<Template>? entities)
    {
        return entities?.Select(Map).OfType<TemplateEntity>().ToList();
    }
}
