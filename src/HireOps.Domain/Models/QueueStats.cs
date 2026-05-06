namespace HireOps.Domain.Models;

public record QueueStats
{
    public int MessagesReady { get; init; }
    public int MessagesUnacknowledged { get; init; }
    public int Consumers { get; init; }
    public MessageStats? MessageStats { get; init; }
}