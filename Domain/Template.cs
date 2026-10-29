using Base.Domain;

namespace Domain;

public class Template : BaseEntity
{
    public Guid NotificationTypeId { get; set; }
    public EChannel Channel { get; set; }
    public string Tenant { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "en";
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
