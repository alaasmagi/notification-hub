using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.DataAccess;

public class EmailSenderEntity : BaseEntityWithMeta
{
    public Guid ClientId { get; set; }
    [Required]
    [MaxLength(64)]
    public string Tenant { get; set; } = string.Empty;
    [Required]
    [MaxLength(64)]
    public string FromAddress { get; set; } = default!;
    [Required]
    [MaxLength(64)]
    public string DisplayName { get; set; } = default!;
    [MaxLength(64)]
    public string? ReplyTo { get; set; }
    public bool IsActive { get; set; } = true;
}
