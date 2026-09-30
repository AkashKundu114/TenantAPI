namespace TenantAPI.Models;

public class Tenant
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Subdomain { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ScreeningResult> Screenings { get; set; } = [];
}
