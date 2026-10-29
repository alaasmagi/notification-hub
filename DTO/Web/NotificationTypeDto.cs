using Base.Domain;
using Domain;

namespace DTO.Web;

public class NotificationTypeDto : BaseEntity
{
    public Guid ClientId { get; set; }
    public string Key { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
