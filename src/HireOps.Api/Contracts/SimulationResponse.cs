namespace HireOps.Api.Contracts;

public record SimulationResponse(Guid Id, string State, int ProcessedCount, DateTime CreatedAt);