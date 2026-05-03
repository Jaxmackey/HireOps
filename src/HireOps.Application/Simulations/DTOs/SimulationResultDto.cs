namespace HireOps.Application.Simulations.DTOs;

public record SimulationResultDto(Guid Id, string State, int ProcessedCount, DateTime CreatedAt);