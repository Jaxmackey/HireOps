namespace HireOps.Domain.Models;

public record ProcessingStats
{
    public long TotalProcessed;
    public long SuccessCount;
    public long AvgLatencyMs;
    public double SuccessRate => TotalProcessed > 0 ? (double)SuccessCount / TotalProcessed : 0;
}