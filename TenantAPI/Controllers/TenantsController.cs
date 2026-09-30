using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TenantAPI.Data;
using TenantAPI.Models;

namespace TenantAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TenantsController(AppDbContext db) : ControllerBase
{
    /// <summary>List all tenants (admin only).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TenantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TenantDto>>> GetTenants()
    {
        var tenants = await db.Tenants
            .OrderBy(t => t.Name)
            .Select(t => new TenantDto(t.Id, t.Name, t.Subdomain, t.CreatedAt))
            .ToListAsync();

        return Ok(tenants);
    }
}
