namespace Web.MVC.Models;

public class NotificationTypeOption
{
    public Guid Id { get; set; }
    public string ClientSource { get; set; } = default!;
    public string Key { get; set; } = default!;
}
