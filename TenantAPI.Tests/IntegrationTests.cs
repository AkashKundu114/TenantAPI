using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TenantAPI.Data;
using TenantAPI.Models;

namespace TenantAPI.Tests;

public class ApiTests : IClassFixture<ApiTests.TenantApiFactory>
{
    // ── Shared test constants ──────────────────────────────────────────
    private static readonly Guid TenantA = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ScreeningA1 = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ScreeningB1 = new("22222222-2222-2222-2222-222222222222");
    private const string JwtKey = "DevSecret_ChangeInProduction_MinLength32Chars!";

    private readonly TenantApiFactory _factory;

    public ApiTests(TenantApiFactory factory) => _factory = factory;

    // ── Custom WebApplicationFactory ───────────────────────────────────
    public class TenantApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                // Replace DB with InMemory
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(o =>
                    o.UseInMemoryDatabase("TenantApiTests"));

                // Seed data
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                if (!db.Tenants.Any())
                {
                    db.Tenants.AddRange(
                        new Tenant { Id = TenantA, Name = "Hospital Alpha", Subdomain = "alpha" },
                        new Tenant { Id = TenantB, Name = "Clinic Beta", Subdomain = "beta" });

                    db.Screenings.AddRange(
                        new ScreeningResult
                        {
                            Id = ScreeningA1, TenantId = TenantA, PatientId = "PA-001",
                            Diagnosis = "Diabetic Retinopathy", Confidence = 0.94,
                            ModelVersion = "OphthalmoAI-v2", ImagePath = "/img/pa001.jpg"
                        },
                        new ScreeningResult
                        {
                            Id = Guid.NewGuid(), TenantId = TenantA, PatientId = "PA-002",
                            Diagnosis = "Normal", Confidence = 0.87,
                            ModelVersion = "OphthalmoAI-v2", ImagePath = "/img/pa002.jpg"
                        },
                        new ScreeningResult
                        {
                            Id = ScreeningB1, TenantId = TenantB, PatientId = "PB-001",
                            Diagnosis = "Glaucoma", Confidence = 0.91,
                            ModelVersion = "OphthalmoAI-v2", ImagePath = "/img/pb001.jpg"
                        });

                    db.SaveChanges();
                }
            });
        }
    }

    // ── JWT helper ─────────────────────────────────────────────────────
    private static string GenerateToken(Guid tenantId, string role = "User")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "TenantAPI",
            audience: "TenantAPI",
            claims:
            [
                new Claim("tenant_id", tenantId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Sub, $"test-{role.ToLower()}")
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateTokenWithoutTenant(string role = "User")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "TenantAPI",
            audience: "TenantAPI",
            claims:
            [
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Sub, "test-no-tenant")
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private HttpClient CreateClient(Guid tenantId, string role = "User")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(tenantId, role));
        return client;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TESTS
    // ═══════════════════════════════════════════════════════════════════

    // 1. Unauthenticated → 401
    [Fact]
    public async Task GetScreenings_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/screenings");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // 2. POST unauthenticated → 401
    [Fact]
    public async Task CreateScreening_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var dto = new CreateScreeningDto("PX-001", "DR", 0.9, "v2", "/img/x.jpg");
        var response = await client.PostAsJsonAsync("/api/screenings", dto);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // 3. Admin endpoint with User role → 403
    [Fact]
    public async Task GetTenants_NonAdmin_Returns403()
    {
        using var client = CreateClient(TenantA, "User");
        var response = await client.GetAsync("/api/tenants");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // 4. Admin endpoint with Admin role → 200
    [Fact]
    public async Task GetTenants_Admin_ReturnsAllTenants()
    {
        using var client = CreateClient(TenantA, "Admin");
        var tenants = await client.GetFromJsonAsync<List<TenantDto>>("/api/tenants");

        Assert.NotNull(tenants);
        Assert.Equal(2, tenants.Count);
    }

    // 5. Tenant A sees only its own screenings
    [Fact]
    public async Task GetScreenings_TenantA_SeesOnlyOwnData()
    {
        using var client = CreateClient(TenantA);
        var results = await client.GetFromJsonAsync<List<ScreeningDto>>("/api/screenings");

        Assert.NotNull(results);
        Assert.True(results.Count >= 2, "Tenant A should have at least 2 screenings");
        Assert.All(results, r => Assert.NotEqual("Glaucoma", r.Diagnosis));
    }

    // 6. Tenant B sees only its own screenings
    [Fact]
    public async Task GetScreenings_TenantB_SeesOnlyOwnData()
    {
        using var client = CreateClient(TenantB);
        var results = await client.GetFromJsonAsync<List<ScreeningDto>>("/api/screenings");

        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Glaucoma", results[0].Diagnosis);
    }

    // 7. Tenant isolation: A cannot see B's screening by ID
    [Fact]
    public async Task TenantIsolation_CrossTenantAccess_Returns404()
    {
        using var client = CreateClient(TenantA);
        var response = await client.GetAsync($"/api/screenings/{ScreeningB1}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // 8. Get screening by ID — own tenant
    [Fact]
    public async Task GetScreeningById_OwnTenant_ReturnsOk()
    {
        using var client = CreateClient(TenantA);
        var result = await client.GetFromJsonAsync<ScreeningDto>(
            $"/api/screenings/{ScreeningA1}");

        Assert.NotNull(result);
        Assert.Equal("PA-001", result.PatientId);
        Assert.Equal("Diabetic Retinopathy", result.Diagnosis);
    }

    // 9. Create screening → 201
    [Fact]
    public async Task CreateScreening_ValidDto_Returns201()
    {
        using var client = CreateClient(TenantA);
        var dto = new CreateScreeningDto(
            "PA-099", "Age-Related Macular Degeneration", 0.82, "OphthalmoAI-v3", "/img/pa099.jpg");

        var response = await client.PostAsJsonAsync("/api/screenings", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ScreeningDto>();
        Assert.NotNull(created);
        Assert.Equal("PA-099", created.PatientId);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    // 10. No tenant_id claim → 403
    [Fact]
    public async Task GetScreenings_NoTenantClaim_Returns403()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTokenWithoutTenant());

        var response = await client.GetAsync("/api/screenings");
        // Controller returns Problem with 403 when tenant can't be resolved
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // 11. Statistics endpoint returns grouped data
    [Fact]
    public async Task GetStatistics_ReturnsGroupedResults()
    {
        using var client = CreateClient(TenantA);
        var stats = await client.GetFromJsonAsync<List<ScreeningStatsDto>>(
            "/api/screenings/statistics");

        Assert.NotNull(stats);
        Assert.True(stats.Count >= 1);
        Assert.All(stats, s =>
        {
            Assert.False(string.IsNullOrEmpty(s.Diagnosis));
            Assert.True(s.Count > 0);
            Assert.InRange(s.AverageConfidence, 0.0, 1.0);
        });
    }

    // 12. GET non-existent screening → 404
    [Fact]
    public async Task GetScreeningById_NonExistent_Returns404()
    {
        using var client = CreateClient(TenantA);
        var response = await client.GetAsync($"/api/screenings/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
