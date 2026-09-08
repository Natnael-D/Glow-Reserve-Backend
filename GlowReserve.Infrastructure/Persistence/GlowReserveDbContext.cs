using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GlowReserve.Domain.Entities;
using GlowReserve.Infrastructure.Identity;

namespace GlowReserve.Infrastructure.Persistence;

public class GlowReserveDbContext : IdentityDbContext<ApplicationUser>
{
    public GlowReserveDbContext(DbContextOptions<GlowReserveDbContext> options) : base(options)
    {
    }

    public DbSet<Salon> Salons => Set<Salon>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Staff> StaffMembers => Set<Staff>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GlowReserveDbContext).Assembly);
    }
}