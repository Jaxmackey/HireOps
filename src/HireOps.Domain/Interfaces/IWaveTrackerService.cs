namespace HireOps.Domain.Interfaces;

public interface IWaveTrackerService
{
    void StartWave(string waveId, int totalCount);
    Task IncrementStageProgressAsync(string waveId, string stageName, CancellationToken ct);
    object? GetActiveWaveProgress();
    bool IsWaveActive();
    string? GetActiveWaveId();
}