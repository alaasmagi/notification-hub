using Base.Contracts.DTO;
using Domain;

namespace DTO.Web.Mappers;

public class EmailSenderDtoMapper : IMapper<EmailSenderDto, EmailSender>
{
    public EmailSenderDto? Map(EmailSender? entity)
    {
        return entity == null
            ? null
            : new EmailSenderDto
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

    public IEnumerable<EmailSenderDto>? Map(IEnumerable<EmailSender>? entities)
    {
        return entities?.Select(Map).OfType<EmailSenderDto>().ToList();
    }

    public EmailSender? Map(EmailSenderDto? entity)
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

    public IEnumerable<EmailSender>? Map(IEnumerable<EmailSenderDto>? entities)
    {
        return entities?.Select(Map).OfType<EmailSender>().ToList();
    }
}
