using Base.Contracts.DTO;
using Domain;

namespace DTO.DataAccess.Mappers;

public class EmailSenderEntityMapper : IMapper<EmailSender, EmailSenderEntity>
{
    public EmailSender? Map(EmailSenderEntity? entity)
    {
        return entity == null
            ? null
            : new EmailSender
            {
                Id = entity.Id,
                ClientId = entity.ClientId,
                Tenant = entity.Tenant,
                FromAddress = entity.FromAddress,
                DisplayName = entity.DisplayName,
                ReplyTo = entity.ReplyTo,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<EmailSender>? Map(IEnumerable<EmailSenderEntity>? entities)
    {
        return entities?.Select(Map).OfType<EmailSender>().ToList();
    }

    public EmailSenderEntity? Map(EmailSender? entity)
    {
        return entity == null
            ? null
            : new EmailSenderEntity
            {
                Id = entity.Id,
                ClientId = entity.ClientId,
                Tenant = entity.Tenant,
                FromAddress = entity.FromAddress,
                DisplayName = entity.DisplayName,
                ReplyTo = entity.ReplyTo,
                IsActive = entity.IsActive
            };
    }

    public IEnumerable<EmailSenderEntity>? Map(IEnumerable<EmailSender>? entities)
    {
        return entities?.Select(Map).OfType<EmailSenderEntity>().ToList();
    }
}
