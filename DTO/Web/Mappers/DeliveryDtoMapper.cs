using Base.Contracts.DTO;
using Domain;

namespace DTO.Web.Mappers;

public class DeliveryDtoMapper : IMapper<DeliveryDto, Delivery>
{
    public DeliveryDto? Map(Delivery? entity)
    {
        return entity == null
            ? null
            : new DeliveryDto
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

    public IEnumerable<DeliveryDto>? Map(IEnumerable<Delivery>? entities)
    {
        return entities?.Select(Map).OfType<DeliveryDto>().ToList();
    }

    public Delivery? Map(DeliveryDto? entity)
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

    public IEnumerable<Delivery>? Map(IEnumerable<DeliveryDto>? entities)
    {
        return entities?.Select(Map).OfType<Delivery>().ToList();
    }
}
