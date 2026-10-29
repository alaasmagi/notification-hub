using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.Web;

public class EmailSenderDto : BaseEntity
{
    public Guid ClientId { get; set; }
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string Tenant { get; set; } = string.Empty;
    public string FromAddress { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? ReplyTo { get; set; }
    public bool IsActive { get; set; } = true;
}
