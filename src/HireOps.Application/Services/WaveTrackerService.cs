using System.Collections.Concurrent;
using HireOps.Application.Hubs;
using HireOps.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace HireOps.Application.Services;

public class WaveTrackerService(IHubContext<DashboardHub> hub) : IWaveTrackerService
{
    private readonly ConcurrentDictionary<string, (int total, double credit)> _waves = new();
    private string? _activeWaveId;

    // 🔹 Веса этапов: 3 этапа = по 1/3 каждый (~0.3333)
    // Сумма должна быть ровно 1.0 для корректного завершения
    private static readonly Dictionary<string, double> StageWeights = new()
    {
        { "sim.received", 1.0 / 3.0 },   // ~0.3333
        { "sim.screening", 1.0 / 3.0 },  // ~0.3333
        { "sim.tech", 1.0 / 3.0 }        // ~0.3334 (последний забирает остаток)
    };

    public void StartWave(string waveId, int totalCount)
    {
        _waves[waveId] = (totalCount, 0.0);
        _activeWaveId = waveId;
        
        hub.Clients.All.SendAsync("waveStarted", new { waveId, totalCount, processed = 0, percent = 0 });
    }
    
    public async Task IncrementStageProgressAsync(string waveId, string stageName, CancellationToken ct)
    {
        if (!_waves.TryGetValue(waveId, out var stats))
            return;
            
        if (!StageWeights.TryGetValue(stageName, out var weight))
            return; // Неизвестный этап — игнорируем
        
        var newCredit = stats.credit + weight;
        _waves[waveId] = (stats.total, newCredit);
        
        // 🔹 Считаем процент (ограничиваем 100%)
        var percent = Math.Min(100, (int)(newCredit * 100.0 / stats.total));
        
        await hub.Clients.All.SendAsync("WaveProgress", new 
        { 
            waveId, 
            total = stats.total, 
            processed = (int)(newCredit / 1.0), 
            percent 
        }, ct);

        // 🔹 Завершение: кредит >= общего количества (с небольшим допуском на погрешность)
        if (newCredit >= stats.total * 0.999)
        {
            _waves.TryRemove(waveId, out _);
            if (_activeWaveId == waveId) 
                _activeWaveId = null;
                
            await hub.Clients.All.SendAsync("WaveCompleted", new { waveId }, ct);
        }
    }
    
    public object? GetActiveWaveProgress()
    {
        if (_activeWaveId != null && _waves.TryGetValue(_activeWaveId, out var stats))
        {
            var percent = Math.Min(100, (int)(stats.credit * 100.0 / stats.total));
            return new 
            { 
                waveId = _activeWaveId,
                total = stats.total,
                processed = (int)(stats.credit / 1.0),
                percent
            };
        }
        return null;
    }
    
    public string? GetActiveWaveId() => _activeWaveId;
    public bool IsWaveActive() => _activeWaveId != null;
}