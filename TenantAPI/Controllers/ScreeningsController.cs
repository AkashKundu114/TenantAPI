using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenantAPI.Models;
using TenantAPI.Services;

namespace TenantAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScreeningsController(IScreeningService service) : ControllerBase
{
    private Guid GetTenantId() =>
        HttpContext.Items["TenantId"] is Guid id ? id : Guid.Empty;

    /// <summary>List screening results for the current tenant.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ScreeningDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ScreeningDto>>> GetScreenings()
    {
        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty)
            return Problem("Tenant context could not be resolved.", statusCode: 403);

        return Ok(await service.GetScreeningsAsync(tenantId));
    }

    /// <summary>Get a specific screening result by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ScreeningDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScreeningDto>> GetScreening(Guid id)
    {
        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty)
            return Problem("Tenant context could not be resolved.", statusCode: 403);

        var result = await service.GetScreeningByIdAsync(tenantId, id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new screening result.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ScreeningDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ScreeningDto>> CreateScreening(CreateScreeningDto dto)
    {
        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty)
            return Problem("Tenant context could not be resolved.", statusCode: 403);

        var result = await service.CreateScreeningAsync(tenantId, dto);
        return CreatedAtAction(nameof(GetScreening), new { id = result.Id }, result);
    }

    /// <summary>Get aggregated screening statistics for the current tenant.</summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(List<ScreeningStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ScreeningStatsDto>>> GetStatistics()
    {
        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty)
            return Problem("Tenant context could not be resolved.", statusCode: 403);

        return Ok(await service.GetStatisticsAsync(tenantId));
    }
}
