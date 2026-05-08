using System.Collections.Concurrent;
using HireOps.Application.Hubs;
using HireOps.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace HireOps.Application.Services;

public class WaveTrackerService(IHubContext<DashboardHub> hub) : IWaveTrackerService
{
    // 🔹 Храним: waveId → (total, currentCredit)
    // credit = сумма "весов" обработанных сообщений (максимум = total × 1.0)
    private readonly ConcurrentDictionary<string, (int total, double credit)> _waves = new();
    
    private string? _activeWaveId;

    // 🔹 Веса этапов (в сумме = 1.0)
    private static readonly Dictionary<string, double> StageWeights = new()
    {
        { "sim.received", 0.25 },
        { "sim.screening", 0.25 },
        { "sim.tech", 0.25 },
        { "sim.hr", 0.25 }
    };

    public void StartWave(string waveId, int totalCount)
    {
        _waves[waveId] = (totalCount, 0.0);
        _activeWaveId = waveId;
        
        hub.Clients.All.SendAsync("waveStarted", new { waveId, totalCount, processed = 0, percent = 0 });
    }
    
    /// <summary>
    /// Добавляет "кредит" прогресса за прохождение сообщения через этап.
    /// </summary>
    public async Task IncrementStageProgressAsync(string waveId, string stageName, CancellationToken ct)
    {
        if (!_waves.TryGetValue(waveId, out var stats))
            return;
            
        if (!StageWeights.TryGetValue(stageName, out var weight))
        {
            // Неизвестный этап — игнорируем или логируем
            return;
        }
        
        // 🔹 Добавляем вес этапа к кредиту волны
        var newCredit = stats.credit + weight;
        _waves[waveId] = (stats.total, newCredit);
        
        // 🔹 Считаем процент (ограничиваем 100%)
        var percent = Math.Min(100, (int)(newCredit * 100.0 / stats.total));
        
        // 🔹 Отправляем прогресс (не чаще чем раз в 100мс можно добавить дебаунс, но для демо ок)
        await hub.Clients.All.SendAsync("waveProgress", new 
        { 
            waveId, 
            total = stats.total, 
            // Для отображения "обработано" показываем целые сообщения: кредит / 1.0
            processed = (int)(newCredit / 1.0), 
            percent 
        }, ct);

        // 🔹 Если кредит достиг максимума — волна завершена
        if (newCredit >= stats.total * 1.0)
        {
            _waves.TryRemove(waveId, out _);
            if (_activeWaveId == waveId) 
                _activeWaveId = null;
                
            await hub.Clients.All.SendAsync("waveCompleted", new { waveId }, ct);
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