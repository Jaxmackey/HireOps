using HireOps.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireOps.Infrastructure.Data.Configurations;

public class SimulationConfiguration : IEntityTypeConfiguration<SimulationDbModel>
{
    public void Configure(EntityTypeBuilder<SimulationDbModel> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.State).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique();
    }
}