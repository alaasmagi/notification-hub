using Base.Domain;

namespace Domain;

public class Client : BaseEntity
{
    public string Source { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
