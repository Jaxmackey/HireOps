using HireOps.Application.Simulations.DTOs;
using HireOps.Application.Simulations.Mappings;
using HireOps.Domain.Interfaces;
using HireOps.Domain.Simulations;
using MediatR;

namespace HireOps.Application.Simulations.Commands;

public class StartWaveCommandHandler : IRequestHandler<StartWaveCommand, SimulationResultDto>
{
    private readonly ISimulationRepository _repo;
    private readonly ISimulationEngine _engine;

    public StartWaveCommandHandler(ISimulationRepository repo, ISimulationEngine engine)
    {
        _repo = repo;
        _engine = engine;
    }

    public async Task<SimulationResultDto> Handle(StartWaveCommand request, CancellationToken ct)
    {
        var simulation = new Simulation(request.TenantId);
        simulation.Start();
        await _repo.AddAsync(simulation, ct);

        var processed = await _engine.ProcessWaveAsync(simulation.TenantId, request.ApplicantCount, ct);
        
        simulation.Complete(processed);
        await _repo.SaveChangesAsync(ct);

        return simulation.ToDto();
    }
}