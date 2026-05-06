namespace HireOps.Domain.Models;

public record MessageStats
{
    public int? Ack { get; init; } // Всего подтверждено
    public int? AckDetailsRate { get; init; } // Скорость ack/sec
}