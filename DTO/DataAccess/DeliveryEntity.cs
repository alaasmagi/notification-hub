using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.DataAccess;

public class DeliveryEntity : BaseEntityWithMeta
{
    [Required]
    [MaxLength(64)]
    public string MessageId { get; set; } = default!;
    [Required]
    [MaxLength(64)]
    public string Source { get; set; } = default!;
    [Required]
    [MaxLength(64)]
    public string Tenant { get; set; } = string.Empty;
    [Required]
    [MaxLength(64)]
    public string NotificationType { get; set; } = default!;
    public EChannel Channel { get; set; }
    [Required]
    [MaxLength(2048)]
    public string Recipient { get; set; } = default!;
    [MaxLength(1024)]
    public string? Title { get; set; }
    public EDeliveryStatus Status { get; set; }
    public string? Error { get; set; }
    public DateTime? SentAt { get; set; }
}
