namespace HireOps.Domain.Simulations;

public class Simulation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public SimulationState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ProcessedCount { get; set; }

    private Simulation() { } // EF Core

    public Simulation(Guid tenantId)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        State = SimulationState.Created;
        CreatedAt = DateTime.UtcNow;
    }

    public void Start() => State = SimulationState.Running;
    public void Complete(int count) 
    { 
        State = SimulationState.Completed; 
        ProcessedCount = count; 
    }
    public void Fail() => State = SimulationState.Failed;
}