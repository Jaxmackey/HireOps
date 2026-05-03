namespace HireOps.Infrastructure.Models;

public class SimulationDbModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ProcessedCount { get; set; }
}