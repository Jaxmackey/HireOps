using HireOps.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace HireOps.Application.Services;

public class ChaosService : IChaosService
{
    private bool _isEnabled;
    private readonly double _failureRate;
    private readonly int _maxDelayMs;

    public ChaosService(IConfiguration config)
    {
        _failureRate = double.Parse(config["Chaos:FailureRate"] ?? "0,05"); // 5% шанс сбоя
        _maxDelayMs = int.Parse(config["Chaos:MaxDelayMs"] ?? "2000"); // до 2 сек задержки
    }

    public void Enable() => _isEnabled = true;
    public void Disable() => _isEnabled = false;
    public bool IsEnabled() => _isEnabled;
    
    public async Task SimulateAsync(CancellationToken ct = default)
    {
        if (!_isEnabled) return;

        var roll = Random.Shared.NextDouble();
        
        if (roll < _failureRate)
        {
            // 5% шанс "сбоя" (таймаут или ошибка)
            throw new InvalidOperationException("Chaos: Simulated failure");
        }
        else if (roll < _failureRate * 3)
        {
            // 10% шанс задержки
            var delay = Random.Shared.Next(500, _maxDelayMs);
            await Task.Delay(delay, ct);
        }
        // 85% — всё работает как обычно
    }
}