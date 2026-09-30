using TenantAPI.Models;

namespace TenantAPI.Services;

public interface IScreeningService
{
    Task<List<ScreeningDto>> GetScreeningsAsync(Guid tenantId);
    Task<ScreeningDto?> GetScreeningByIdAsync(Guid tenantId, Guid id);
    Task<ScreeningDto> CreateScreeningAsync(Guid tenantId, CreateScreeningDto dto);
    Task<List<ScreeningStatsDto>> GetStatisticsAsync(Guid tenantId);
}
