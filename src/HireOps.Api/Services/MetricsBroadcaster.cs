using HireOps.Api.Hubs;
using HireOps.Application.Workers;
using HireOps.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace HireOps.Api.Services;

public class MetricsBroadcaster : BackgroundService
{
    private readonly IHubContext<DashboardHub> _hub;
    private readonly IWorkerManagerService _workerManager;
    private readonly IRabbitMqMetricsService _rabbitMetrics;
    private readonly IProcessingMetricsStore _processingMetrics;
    private readonly ILogger<MetricsBroadcaster> _logger;

    public MetricsBroadcaster(
        IHubContext<DashboardHub> hub,
        IWorkerManagerService workerManager,
        IRabbitMqMetricsService rabbitMetrics,
        IProcessingMetricsStore processingMetrics,
        ILogger<MetricsBroadcaster> logger)
    {
        _hub = hub;
        _workerManager = workerManager;
        _rabbitMetrics = rabbitMetrics;
        _processingMetrics = processingMetrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("📡 MetricsBroadcaster запущен (REAL MODE)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 🔹 1. Собираем данные из RabbitMQ
                var rabbitStats = _rabbitMetrics.GetAllStats();
                var totalQueueDepth = rabbitStats.Values.Sum(s => s.MessagesReady);
                var totalConsumers = rabbitStats.Values.Sum(s => s.Consumers);
                
                // 🔹 2. Собираем данные обработки
                var processingStats = _processingMetrics.GetAll();
                var totalProcessed = processingStats.Values.Sum(s => s.TotalProcessed);
                var avgLatency = processingStats.Values.Any() 
                    ? (int)processingStats.Values.Average(s => s.AvgLatencyMs) 
                    : 0;
                
                // 🔹 3. Считаем пропускную способность (упрощённо: дельта за последнюю секунду)
                // Для прода: хранить историю в кольцевом буфере
                var throughput = processingStats.Values.Sum(s => 
                    s.TotalProcessed > 0 ? (int)(s.SuccessCount / Math.Max(1, DateTime.UtcNow.Second)) : 0);

                var metrics = new
                {
                    queueDepth = totalQueueDepth,
                    latency = avgLatency,
                    processedPerSec = throughput,
                    processedTotal = totalProcessed,
                    throughput = throughput,
                    // Бонус: технические детали для отладки
                    rabbitConsumers = totalConsumers,
                    stages = processingStats.ToDictionary(
                        k => k.Key, 
                        v => new { 
                            processed = v.Value.TotalProcessed, 
                            successRate = v.Value.SuccessRate,
                            avgLatency = v.Value.AvgLatencyMs 
                        })
                };

                await _hub.Clients.All.SendAsync("MetricsUpdate", metrics, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error broadcasting real metrics");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}