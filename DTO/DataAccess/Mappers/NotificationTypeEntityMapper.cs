using Base.Contracts.DTO;
using Domain;

namespace DTO.DataAccess.Mappers;

public class NotificationTypeEntityMapper : IMapper<NotificationType, NotificationTypeEntity>
{
    public NotificationType? Map(NotificationTypeEntity? entity)
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

    public IEnumerable<NotificationType>? Map(IEnumerable<NotificationTypeEntity>? entities)
    {
        return entities?.Select(Map).OfType<NotificationType>().ToList();
    }

    public NotificationTypeEntity? Map(NotificationType? entity)
    {
        return entity == null
            ? null
            : new NotificationTypeEntity
            {
                Id = entity.Id,
                ClientId = entity.ClientId,
                Key = entity.Key,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<NotificationTypeEntity>? Map(IEnumerable<NotificationType>? entities)
    {
        return entities?.Select(Map).OfType<NotificationTypeEntity>().ToList();
    }
}
