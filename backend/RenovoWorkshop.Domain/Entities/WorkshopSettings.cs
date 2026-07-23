namespace RenovoWorkshop.Domain.Entities;

public class WorkshopSettings
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "Renovo Workshop";
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Logo { get; set; }
}
