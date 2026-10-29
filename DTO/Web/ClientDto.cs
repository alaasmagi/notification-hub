using Base.Domain;
using Domain;

namespace DTO.Web;

public class ClientDto : BaseEntity
{
    public string Source { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
