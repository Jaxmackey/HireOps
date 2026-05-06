using HireOps.Domain.Models;

namespace HireOps.Domain.Interfaces;

public interface IProcessingMetricsStore
{
    void Record(string stage, TimeSpan duration, bool success);
    ProcessingStats? GetStats(string stage);
    Dictionary<string, ProcessingStats> GetAll();
}