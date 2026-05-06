namespace HireOps.Domain.Models;

public record QueueInfo
{
    public string Name { get; init; } = string.Empty;
    public int MessagesReady { get; init; }
    public int MessagesUnacknowledged { get; init; }
    public int Consumers { get; init; }
    public MessageStats? MessageStats { get; init; }
}