using DTO.Web;

namespace Web.MVC.Models;

public class TemplateIndexRow
{
    public TemplateDto Template { get; set; } = default!;
    public string ClientSource { get; set; } = default!;
    public string NotificationTypeKey { get; set; } = default!;
}
