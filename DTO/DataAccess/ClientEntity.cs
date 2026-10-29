using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Domain;

namespace DTO.DataAccess;

public class ClientEntity : BaseEntityWithMeta
{
    [Required]
    [MaxLength(64)]
    public string Source { get; set; } = default!;
    [Required]
    [MaxLength(64)]
    public string DisplayName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
