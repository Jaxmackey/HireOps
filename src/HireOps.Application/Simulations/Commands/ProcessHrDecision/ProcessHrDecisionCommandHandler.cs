using MediatR;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Simulations.Commands.ProcessHrDecision;

public class ProcessHrDecisionCommandHandler : IRequestHandler<ProcessHrDecisionCommand>
{
    private readonly ILogger<ProcessHrDecisionCommandHandler> _logger;

    public ProcessHrDecisionCommandHandler(ILogger<ProcessHrDecisionCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(ProcessHrDecisionCommand request, CancellationToken ct)
    {
        if (request.Recommended)
        {
            _logger.LogInformation("🤝 HR HIRED applicant {ApplicantId}", request.ApplicantId);
        }
        else
        {
            _logger.LogInformation("❌ HR REJECTED applicant {ApplicantId}", request.ApplicantId);
        }
        await Task.CompletedTask;
    }
}