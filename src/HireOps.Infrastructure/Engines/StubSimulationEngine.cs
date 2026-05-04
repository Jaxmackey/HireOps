using HireOps.Domain.Interfaces;
using HireOps.Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace HireOps.Infrastructure.Engines;

public class StubSimulationEngine : ISimulationEngine
{
    private readonly IRabbitMqService _rabbitMq;
    private readonly ILogger<StubSimulationEngine> _logger;
    private const string Exchange = "sim.pipeline";

    public StubSimulationEngine(IRabbitMqService rabbitMq, ILogger<StubSimulationEngine> logger)
    {
        _rabbitMq = rabbitMq;
        _logger = logger;
    }

    public async Task<int> ProcessWaveAsync(Guid tenantId, int applicantCount, CancellationToken ct)
    {
        // Генерируем "отклики" и пушим в очередь
        var applicants = Enumerable.Range(1, applicantCount)
            .Select(i => new ApplicantMessage 
            { 
                Id = Guid.NewGuid(), 
                TenantId = tenantId,
                Skills = "csharp", 
                Experience = i % 10,
                ReceivedAt = DateTime.UtcNow 
            });

        foreach (var app in applicants)
        {
            if (ct.IsCancellationRequested) break;
            
            await _rabbitMq.PublishAsync(
                exchange: Exchange,
                routingKey: "sim.received",
                message: app,
                ct: ct);
        }

        _logger.LogInformation("Published {Count} applicants to queue for tenant {TenantId}", 
            applicantCount, tenantId);
        
        return applicantCount;
    }
}