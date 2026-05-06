using System.Collections.Concurrent;
using HireOps.Domain.Interfaces;
using HireOps.Domain.Simulations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Workers;

public class WorkerManagerService : IWorkerManagerService
{
    private readonly IRabbitMqService _rabbitMq;
    private readonly ILogger<WorkerManagerService> _logger;
    
    // Храним: QueueName -> List<ConsumerTag>
    private readonly ConcurrentDictionary<string, List<string>> _activeConsumers = new();

    public WorkerManagerService(IRabbitMqService rabbitMq, ILogger<WorkerManagerService> logger)
    {
        _rabbitMq = rabbitMq;
        _logger = logger;
    }

    /// <summary>
    /// Добавляет одного воркера (консьюмера) на очередь.
    /// </summary>
    public async Task<ConsumerSubscription> AddWorkerAsync<T>(
        string queue, 
        Func<T, CancellationToken, Task> handler,
        CancellationToken ct = default) where T : class
    {
        var subscription = await _rabbitMq.ConsumeAsync(queue, handler, ct);
        
        _activeConsumers.AddOrUpdate(
            queue,
            _ => new() { subscription.ConsumerTag },
            (_, list) => { list.Add(subscription.ConsumerTag); return list; });
        
        _logger.LogInformation("➕ Added worker to queue '{Queue}' (total: {Count})", 
            queue, _activeConsumers[queue].Count);
        
        return subscription;
    }

    /// <summary>
    /// Добавляет воркера с медиацией (для конвейера).
    /// </summary>
    public async Task<ConsumerSubscription> AddMediatedWorkerAsync<TMessage, TCommand>(
        string queue,
        Guid? tenantId,
        Func<TMessage, TCommand> mapToCommand,
        CancellationToken ct = default) where TMessage : class where TCommand : IRequest
    {
        var subscription = await _rabbitMq.ConsumeAndMediateAsync(queue, tenantId, mapToCommand, ct);
        
        _activeConsumers.AddOrUpdate(
            queue,
            _ => new() { subscription.ConsumerTag },
            (_, list) => { list.Add(subscription.ConsumerTag); return list; });
        
        _logger.LogInformation("➕ Added mediated worker to queue '{Queue}' (total: {Count})", 
            queue, _activeConsumers[queue].Count);
        
        return subscription;
    }

    /// <summary>
    /// Удаляет одного воркера с очереди (по тегу или последний).
    /// </summary>
    public async Task<bool> RemoveWorkerAsync(string queue, string? consumerTag = null, 
        CancellationToken ct = default)
    {
        if (!_activeConsumers.TryGetValue(queue, out var tags) || tags.Count == 0)
            return false;

        var tagToRemove = consumerTag ?? tags.Last();
        
        await _rabbitMq.CancelConsumerAsync(tagToRemove, ct);
        tags.Remove(tagToRemove);
        
        if (tags.Count == 0)
            _activeConsumers.TryRemove(queue, out _);
        
        _logger.LogInformation("➖ Removed worker from queue '{Queue}' (remaining: {Count})", 
            queue, tags.Count);
        
        return true;
    }

    /// <summary>
    /// Возвращает количество активных воркеров на очереди.
    /// </summary>
    public int GetWorkerCount(string queue) => 
        _activeConsumers.TryGetValue(queue, out var tags) ? tags.Count : 0;

    /// <summary>
    /// Возвращает статистику по всем очередям (для метрик).
    /// </summary>
    public Dictionary<string, int> GetStats() => 
        _activeConsumers.ToDictionary(k => k.Key, v => v.Value.Count);
}