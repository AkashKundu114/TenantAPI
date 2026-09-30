using Microsoft.EntityFrameworkCore;
using TenantAPI.Models;

namespace TenantAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ScreeningResult> Screenings => Set<ScreeningResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Subdomain).IsUnique();
        });

        modelBuilder.Entity<ScreeningResult>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.TenantId);
            e.HasOne(s => s.Tenant)
             .WithMany(t => t.Screenings)
             .HasForeignKey(s => s.TenantId);
        });
    }
}
