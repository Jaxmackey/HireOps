using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;
using HireOps.Domain.Interfaces;
using HireOps.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HireOps.Infrastructure.Services;

public class RabbitMqMetricsService : IRabbitMqMetricsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;
    private readonly string _authHeader;
    private readonly ILogger<RabbitMqMetricsService> _logger;
    private readonly Timer _pollingTimer;
    
    // Кэш последних метрик: queue_name -> stats
    private readonly ConcurrentDictionary<string, QueueStats> _cache = new();

    public RabbitMqMetricsService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<RabbitMqMetricsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        
        var host = config["RabbitMQ:HostName"] ?? "localhost";
        var port = config["RabbitMQ:ManagementPort"] ?? "15672"; // Отдельный порт для Management!
        var user = config["RabbitMQ:Username"] ?? "guest";
        var pass = config["RabbitMQ:Password"] ?? "guest";
        
        _baseUrl = $"http://{host}:{port}/api/queues/%2F"; // %2F = root vhost "/"
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
        _authHeader = $"Basic {credentials}";
        
        // Запускаем опрос сразу
        _pollingTimer = new Timer(PollAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }

    private async void PollAsync(object? state)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", _authHeader);
            
            var queues = await client.GetFromJsonAsync<QueueInfo[]>(
                _baseUrl, 
                cancellationToken: CancellationToken.None);
            
            if (queues != null)
            {
                foreach (var q in queues)
                {
                    if (q.Name.StartsWith("sim.")) // Фильтруем только наши очереди
                    {
                        _cache[q.Name] = new QueueStats
                        {
                            MessagesReady = q.MessagesReady,
                            MessagesUnacknowledged = q.MessagesUnacknowledged,
                            Consumers = q.Consumers,
                            MessageStats = q.MessageStats
                        };
                    }
                }
                _logger.LogDebug("📊 RabbitMQ metrics cached: {Count} queues", _cache.Count);
            }
        }
        catch (Exception ex)
        {
            // Не роняем сервис, просто логируем (Management API может быть недоступен)
            _logger.LogDebug(ex, "⚠️ Failed to poll RabbitMQ metrics");
        }
    }

    public QueueStats? GetStats(string queueName) => 
        _cache.TryGetValue(queueName, out var stats) ? stats : null;

    public Dictionary<string, QueueStats> GetAllStats() => 
        _cache.ToDictionary(k => k.Key, v => v.Value);

    public void Dispose() => _pollingTimer?.Dispose();
}