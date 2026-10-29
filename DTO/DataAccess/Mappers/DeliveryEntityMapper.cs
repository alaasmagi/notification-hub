using Base.Contracts.DTO;
using Domain;

namespace DTO.DataAccess.Mappers;

public class DeliveryEntityMapper : IMapper<Delivery, DeliveryEntity>
{
    public Delivery? Map(DeliveryEntity? entity)
    {
        return entity == null
            ? null
            : new Delivery
            {
                Id = entity.Id,
                MessageId = entity.MessageId,
                Source = entity.Source,
                Tenant = entity.Tenant,
                NotificationType = entity.NotificationType,
                Channel = entity.Channel,
                Recipient = entity.Recipient,
                Title = entity.Title,
                Status = entity.Status,
                Error = entity.Error,
                SentAt = entity.SentAt
            };
    }

    public IEnumerable<Delivery>? Map(IEnumerable<DeliveryEntity>? entities)
    {
        return entities?.Select(Map).OfType<Delivery>().ToList();
    }

    public DeliveryEntity? Map(Delivery? entity)
    {
        return entity == null
            ? null
            : new DeliveryEntity
            {
                Id = entity.Id,
                MessageId = entity.MessageId,
                Source = entity.Source,
                Tenant = entity.Tenant,
                NotificationType = entity.NotificationType,
                Channel = entity.Channel,
                Recipient = entity.Recipient,
                Title = entity.Title,
                Status = entity.Status,
                Error = entity.Error,
                SentAt = entity.SentAt
            };
    }

    public IEnumerable<DeliveryEntity>? Map(IEnumerable<Delivery>? entities)
    {
        return entities?.Select(Map).OfType<DeliveryEntity>().ToList();
    }
}
