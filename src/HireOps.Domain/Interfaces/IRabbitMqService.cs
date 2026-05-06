using HireOps.Domain.Simulations;
using MediatR;

namespace HireOps.Domain.Interfaces;

public interface IRabbitMqService : IAsyncDisposable
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken ct = default) where T : class;
    
    // 🔹 Возвращаем подписку для управления
    Task<ConsumerSubscription> ConsumeAsync<T>(string queue, Func<T, CancellationToken, Task> handler, CancellationToken ct = default) where T : class;
    
    Task<ConsumerSubscription> ConsumeAndMediateAsync<TMessage, TCommand>(
        string queue, Guid? tenantId, Func<TMessage, TCommand> mapToCommand, CancellationToken ct = default) 
        where TMessage : class where TCommand : IRequest;
    
    // 🔹 Новые методы управления
    Task SetQosAsync(int prefetchCount, CancellationToken ct = default);
    Task CancelConsumerAsync(string consumerTag, CancellationToken ct = default);
}