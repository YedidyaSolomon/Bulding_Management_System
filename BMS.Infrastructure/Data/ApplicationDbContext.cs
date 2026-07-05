using BMS.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BMS.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Building>      Buildings      { get; set; }
    public DbSet<Floor>         Floors         { get; set; }
    public DbSet<Unit>          Units          { get; set; }
    public DbSet<Tenant>        Tenants        { get; set; }
    public DbSet<LegalDocument> LegalDocuments { get; set; }
    public DbSet<Lease>         Leases         { get; set; }
    public DbSet<Invoice>       Invoices       { get; set; }
    public DbSet<Payment>       Payments       { get; set; }
    public DbSet<Notification>  Notifications  { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified && entry.Entity.GetType().GetProperty("UpdatedAt") != null)
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
