using HireOps.Application.Simulations.DTOs;
using HireOps.Application.Simulations.Mappings;
using HireOps.Domain.Interfaces;
using HireOps.Domain.Simulations;
using MediatR;

namespace HireOps.Application.Simulations.Commands.StartWave;

public class StartWaveCommandHandler(ISimulationRepository repo, ISimulationEngine engine)
    : IRequestHandler<StartWaveCommand, SimulationResultDto>
{
    public async Task<SimulationResultDto> Handle(StartWaveCommand request, CancellationToken ct)
    {
        var simulation = new Simulation(request.TenantId);
        simulation.Start();
        await repo.AddAsync(simulation, ct);

        var processed = await engine.ProcessWaveAsync(simulation.TenantId, request.ApplicantCount, ct);
        
        simulation.Complete(processed);
        await repo.SaveChangesAsync(ct);

        return simulation.ToDto();
    }
}