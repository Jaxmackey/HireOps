using HireOps.Api.Contracts;
using HireOps.Api.Mappings;
using HireOps.Application.Services;
using HireOps.Domain.Interfaces;
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
}