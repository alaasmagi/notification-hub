using DTO.Web;

namespace Web.MVC.Models;

public class EmailSenderIndexRow
{
    public EmailSenderDto EmailSender { get; set; } = default!;
    public string ClientSource { get; set; } = default!;
}
