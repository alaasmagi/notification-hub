using Base.Contracts.DTO;
using Domain;

namespace DTO.Web.Mappers;

public class NotificationTypeDtoMapper : IMapper<NotificationTypeDto, NotificationType>
{
    public NotificationTypeDto? Map(NotificationType? entity)
    {
        return entity == null
            ? null
            : new NotificationTypeDto
            {
                Id = entity.Id,
                ClientId = entity.ClientId,
                Key = entity.Key,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<NotificationTypeDto>? Map(IEnumerable<NotificationType>? entities)
    {
        return entities?.Select(Map).OfType<NotificationTypeDto>().ToList();
    }

    public NotificationType? Map(NotificationTypeDto? entity)
    {
        return entity == null
            ? null
            : new NotificationType
            {
                Id = entity.Id,
                ClientId = entity.ClientId,
                Key = entity.Key,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<NotificationType>? Map(IEnumerable<NotificationTypeDto>? entities)
    {
        return entities?.Select(Map).OfType<NotificationType>().ToList();
    }
}
