using System.Collections.Concurrent;
using HireOps.Domain.Interfaces;
using HireOps.Domain.Models;

namespace HireOps.Application.Services;

public class ProcessingMetricsStore : IProcessingMetricsStore
{
    private readonly ConcurrentDictionary<string, ProcessingStats> _stats = new();

    public void Record(string stage, TimeSpan duration, bool success)
    {
        var stats = _stats.GetOrAdd(stage, _ => new ProcessingStats());
        
        // Атомарно обновляем счётчики (Interlocked для потокобезопасности)
        Interlocked.Increment(ref stats.TotalProcessed);
        if (success) Interlocked.Increment(ref stats.SuccessCount);
        
        // Скользящее среднее для латенси (простая экспоненциальная)
        var currentAvg = Interlocked.Read(ref stats.AvgLatencyMs);
        var newAvg = (long)(currentAvg * 0.9 + duration.TotalMilliseconds * 0.1);
        Interlocked.Exchange(ref stats.AvgLatencyMs, newAvg);
    }

    public ProcessingStats? GetStats(string stage) => 
        _stats.GetValueOrDefault(stage);

    public Dictionary<string, ProcessingStats> GetAll() => 
        _stats.ToDictionary(k => k.Key, v => v.Value);
}