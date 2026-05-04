namespace HireOps.Domain.Simulations;

public record TechReviewResult(Guid ApplicantId, Guid TenantId, bool Recommended);