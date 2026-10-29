using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.DataAccess;

public class NotificationTypeEntity : BaseEntityWithMeta
{
    public Guid ClientId { get; set; }
    [Required]
    [MaxLength(64)]
    public string Key { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
