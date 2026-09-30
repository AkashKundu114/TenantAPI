namespace TenantAPI.Models;

public class ScreeningResult
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string PatientId { get; set; }
    public required string Diagnosis { get; set; }
    public double Confidence { get; set; }
    public required string ModelVersion { get; set; }
    public required string ImagePath { get; set; }
    public DateTime ScreenedAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
}
