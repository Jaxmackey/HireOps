namespace HireOps.Domain.Simulations;

public record HrDecisionResult(Guid ApplicantId, Guid TenantId, bool Hired);