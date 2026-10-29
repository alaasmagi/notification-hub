using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.Web;

public class TemplateDto : BaseEntity
{
    public Guid NotificationTypeId { get; set; }
    public EChannel Channel { get; set; }
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string Tenant { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "en";
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
