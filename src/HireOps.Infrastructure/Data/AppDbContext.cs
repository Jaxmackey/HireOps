using HireOps.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace HireOps.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<SimulationDbModel> Simulations => Set<SimulationDbModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}