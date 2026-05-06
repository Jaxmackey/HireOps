using HireOps.Domain.Models;

namespace HireOps.Domain.Interfaces;

public interface IRabbitMqMetricsService
{
    QueueStats? GetStats(string queueName);
    Dictionary<string, QueueStats> GetAllStats();
    void Dispose();
}