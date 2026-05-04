using HireOps.Domain.Simulations;
using MediatR;

namespace HireOps.Application.Simulations.Commands.ProcessTechReview;

public record ProcessTechReviewCommand(Guid ApplicantId, Guid TenantId, int Score) : IRequest;