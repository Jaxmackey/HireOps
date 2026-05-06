using HireOps.Api.Contracts;
using HireOps.Api.Mappings;
using HireOps.Application.Services;
using HireOps.Domain.Interfaces;
using HireOps.Domain.Simulations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HireOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationsController(IMediator mediator, ITenantContext tenantContext,
    IChaosService chaosService) : ControllerBase
{
    [HttpPost("wave")]
    public async Task<ActionResult<SimulationResponse>> StartWave([FromQuery] int applicantCount, CancellationToken ct)
    {
        var cmd = ApiMapper.ToCommand(applicantCount, tenantContext.GetTenantId());
        var result = await mediator.Send(cmd, ct);
        return Ok(result.ToResponse());
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
    public async Task<IActionResult> TestMultiTenant(
        [FromServices] IRabbitMqService rabbitMq,
        [FromServices] ILogger<SimulationsController> logger)
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