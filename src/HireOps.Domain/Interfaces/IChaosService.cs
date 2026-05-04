namespace HireOps.Domain.Interfaces;

public interface IChaosService
{
    Task SimulateAsync(CancellationToken ct = default);
    void Enable();
    void Disable();
    bool IsEnabled();
}