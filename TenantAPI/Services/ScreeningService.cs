using Microsoft.EntityFrameworkCore;
using TenantAPI.Data;
using TenantAPI.Models;

namespace TenantAPI.Services;

public class ScreeningService(AppDbContext db) : IScreeningService
{
    public async Task<List<ScreeningDto>> GetScreeningsAsync(Guid tenantId)
    {
        return await db.Screenings
            .Where(s => s.TenantId == tenantId)                    // LINQ: Filtering
            .OrderByDescending(s => s.ScreenedAt)                  // LINQ: Ordering
            .Select(s => new ScreeningDto(                         // LINQ: Projection
                s.Id, s.PatientId, s.Diagnosis,
                s.Confidence, s.ModelVersion,
                s.ImagePath, s.ScreenedAt))
            .ToListAsync();
    }

    public async Task<ScreeningDto?> GetScreeningByIdAsync(Guid tenantId, Guid id)
    {
        return await db.Screenings
            .Where(s => s.TenantId == tenantId && s.Id == id)      // LINQ: Filtering
            .Select(s => new ScreeningDto(
                s.Id, s.PatientId, s.Diagnosis,
                s.Confidence, s.ModelVersion,
                s.ImagePath, s.ScreenedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<ScreeningDto> CreateScreeningAsync(Guid tenantId, CreateScreeningDto dto)
    {
        var entity = new ScreeningResult
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PatientId = dto.PatientId,
            Diagnosis = dto.Diagnosis,
            Confidence = dto.Confidence,
            ModelVersion = dto.ModelVersion,
            ImagePath = dto.ImagePath
        };

        db.Screenings.Add(entity);
        await db.SaveChangesAsync();

        return new ScreeningDto(
            entity.Id, entity.PatientId, entity.Diagnosis,
            entity.Confidence, entity.ModelVersion,
            entity.ImagePath, entity.ScreenedAt);
    }

    public async Task<List<ScreeningStatsDto>> GetStatisticsAsync(Guid tenantId)
    {
        return await db.Screenings
            .Where(s => s.TenantId == tenantId)                    // LINQ: Filtering
            .GroupBy(s => s.Diagnosis)                             // LINQ: Grouping
            .Select(g => new ScreeningStatsDto(                    // LINQ: Projection
                g.Key,
                g.Count(),                                         // LINQ: Aggregation
                g.Average(s => s.Confidence)))                     // LINQ: Aggregation
            .ToListAsync();
    }
}
