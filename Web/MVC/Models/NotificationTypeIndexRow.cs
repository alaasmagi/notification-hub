using DTO.Web;

namespace Web.MVC.Models;

public class NotificationTypeIndexRow
{
    public NotificationTypeDto NotificationType { get; set; } = default!;
    public string ClientSource { get; set; } = default!;
}
