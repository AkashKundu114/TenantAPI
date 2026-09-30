namespace TenantAPI.Models;

public record CreateScreeningDto(
    string PatientId,
    string Diagnosis,
    double Confidence,
    string ModelVersion,
    string ImagePath);

public record ScreeningDto(
    Guid Id,
    string PatientId,
    string Diagnosis,
    double Confidence,
    string ModelVersion,
    string ImagePath,
    DateTime ScreenedAt);

public record TenantDto(Guid Id, string Name, string Subdomain, DateTime CreatedAt);

public record ScreeningStatsDto(string Diagnosis, int Count, double AverageConfidence);
