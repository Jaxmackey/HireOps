namespace HireOps.Infrastructure.Models;

public record ApplicantMessage
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Skills { get; init; } = string.Empty;
    public int Experience { get; init; }
    public DateTime ReceivedAt { get; init; }
};