using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.DataAccess;

public class TemplateEntity : BaseEntityWithMeta
{
    public Guid NotificationTypeId { get; set; }
    public EChannel Channel { get; set; }
    [Required]
    [MaxLength(64)]
    public string Tenant { get; set; } = string.Empty;
    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; set; } = "en";
    [Required]
    [MaxLength(1024)]
    public string Title { get; set; } = default!;
    [Required]
    [MaxLength(524288)]
    public string Body { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
