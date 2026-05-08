using HireOps.Application.Workers;
using HireOps.Domain.Interfaces;
using HireOps.Domain.Simulations;
using Microsoft.AspNetCore.Mvc;

namespace HireOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationsController(IWaveTrackerService waveTracker, 
    IChaosService chaosService,
    IWorkerManagerService workerManager,
    IConfiguration config,
    IRabbitMqService rabbitMq,
    ILogger<SimulationsController> logger) : ControllerBase
{
    [HttpPost("wave")]
    public async Task<IActionResult> StartWave(
        [FromQuery] int applicantCount, CancellationToken ct)
    {
        var stats = workerManager.GetStats();
        var requiredStages = new[] { "sim.received", "sim.screening", "sim.tech" };
        var emptyStages = requiredStages.Where(stage => 
                stats.GetValueOrDefault(stage, 0) == 0)
            .ToList();
    
        if (emptyStages.Any())
        {
            return BadRequest(new 
            { 
                error = "Cannot start wave: some stages have no workers",
                emptyStages,
                message = $"Add at least 1 worker to: {string.Join(", ", emptyStages)}"
            });
        }
    
        // Запуск трекинга
        var waveId = Guid.NewGuid().ToString("N");
        waveTracker.StartWave(waveId, applicantCount);
    
        // Публикация сообщений
        var exchange = config["RabbitMQ:ExchangeName"] ?? "sim.pipeline";
    
        for (int i = 0; i < applicantCount; i++)
        {
            await rabbitMq.PublishAsync(
                exchange: exchange,
                routingKey: "sim.received",
                message: new ApplicantMessage 
                { 
                    Id = Guid.NewGuid(), 
                    TenantId = Guid.NewGuid(),
                    Skills = new[] { "csharp", "angular", "react", "python" }[Random.Shared.Next(4)],
                    WaveId = waveId // 👈 Передаём волна-айди в сообщение
                }, ct);
        }
    
        return Ok(new { waveId, message = $"Wave started: {applicantCount} applicants" });
    }
    
    [HttpPost("chaos/toggle")]
    public IActionResult ToggleChaos()
    {
        if (chaosService.IsEnabled())
        {
            chaosService.Disable();
            return Ok(new { enabled = false, message = "✅ Normal mode" });
        }
        else
        {
            chaosService.Enable();
            return Ok(new { enabled = true, message = "🔥 Chaos mode activated" });
        }
    }
    
    [HttpPost("test/tenants")]
    public async Task<IActionResult> TestMultiTenant()
    {
        var tenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Публикуем по сообщению от каждого тенанта
        await rabbitMq.PublishAsync("sim.pipeline", "sim.received", 
            new ApplicantMessage { Id = Guid.NewGuid(), TenantId = tenantA, Skills = "csharp" }, CancellationToken.None);
    
        await rabbitMq.PublishAsync("sim.pipeline", "sim.received", 
            new ApplicantMessage { Id = Guid.NewGuid(), TenantId = tenantB, Skills = "java"  }, CancellationToken.None);

        logger.LogInformation("🧪 Published test messages for TenantA={A} and TenantB={B}", tenantA, tenantB);
    
        return Ok(new { 
            message = "Check logs: each handler should log its TenantId",
            tenantA = tenantA.ToString("N"), 
            tenantB = tenantB.ToString("N")
        });
    }
}