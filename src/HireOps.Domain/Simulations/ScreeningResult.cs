namespace HireOps.Domain.Simulations;

public record ScreeningResult(Guid ApplicantId, Guid TenantId, int Score, string? WaveId = null );