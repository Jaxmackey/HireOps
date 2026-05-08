using System.Collections.Concurrent;
using HireOps.Domain.Interfaces;
using HireOps.Domain.Simulations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Workers;

/// <summary>
/// Управляет консьюмерами с поддержкой multi-tenancy.
/// Хранит подписки по составному ключу "queue:tenantId" для точного управления.
/// </summary>
public class WorkerManagerService : IWorkerManagerService
{
    private readonly IRabbitMqService _rabbitMq;
    private readonly ILogger<WorkerManagerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    // 🔹 Ключ: "queue:tenantId" (tenantId = "*" для общих консьюмеров)
    // Значение: список тегов консьюмеров для этой пары
    private readonly ConcurrentDictionary<string, List<string>> _activeConsumers = new();

    public WorkerManagerService(IRabbitMqService rabbitMq, ILogger<WorkerManagerService> logger, 
        IServiceProvider serviceProvider)
    {
        _rabbitMq = rabbitMq;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // 🔹 Helper: формирует составной ключ
    private static string MakeKey(string queue, Guid? tenantId) => 
        tenantId.HasValue ? $"{queue}:{tenantId.Value:N}" : $"{queue}:*";

    /// <summary>
    /// Добавляет общего воркера (без привязки к тенанту).
    /// </summary>
    public async Task<ConsumerSubscription> AddWorkerAsync<T>(
        string queue, 
        Guid? tenantId,
        Func<T, CancellationToken, Task> handler,
        CancellationToken ct = default) where T : class
    {
        return await AddWorkerInternalAsync(queue, tenantId, handler, ct);
    }

    /// <summary>
    /// Добавляет воркера с медиацией и привязкой к тенанту.
    /// </summary>
    public async Task<ConsumerSubscription> AddMediatedWorkerAsync<TMessage, TCommand>(
        string queue,
        Guid? tenantId,
        Func<TMessage, TCommand> mapToCommand,
        CancellationToken ct = default) where TMessage : class where TCommand : IRequest
    {
        // 🔹 ИСПРАВЛЕНИЕ: явно указываем <TMessage> чтобы компилятор понял тип
        return await AddWorkerInternalAsync<TMessage>(queue, tenantId, async (msg, token) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var command = mapToCommand(msg);
            await mediator.Send(command, token);
        }, ct);
    }

    // 🔹 Внутренний метод для избежания дублирования кода
    private async Task<ConsumerSubscription> AddWorkerInternalAsync<T>(
        string queue,
        Guid? tenantId,
        Func<T, CancellationToken, Task> handler,
        CancellationToken ct) where T : class
    {
        var subscription = await _rabbitMq.ConsumeAsync(queue, handler, ct);
        
        var key = MakeKey(queue, tenantId);
        
        _activeConsumers.AddOrUpdate(
            key,
            _ => new() { subscription.ConsumerTag },
            (_, list) => { lock(list) { list.Add(subscription.ConsumerTag); } return list; });
        
        _logger.LogInformation("➕ Added worker to queue '{Queue}' for tenant {TenantId} (tag: {Tag})", 
            queue, tenantId?.ToString("N") ?? "*", subscription.ConsumerTag);
        
        return subscription;
    }

    /// <summary>
    /// Удаляет воркера. Если указан tenantId — удаляет только консьюмера этого тенанта.
    /// </summary>
    public async Task<bool> RemoveWorkerAsync(string queue, Guid? tenantId = null, string? consumerTag = null, 
        CancellationToken ct = default)
    {
        var key = MakeKey(queue, tenantId);
        
        if (!_activeConsumers.TryGetValue(key, out var tags) || tags.Count == 0)
        {
            _logger.LogWarning("⚠️ No active workers found for queue '{Queue}' tenant {TenantId}", 
                queue, tenantId?.ToString("N") ?? "*");
            return false;
        }

        var tagToRemove = consumerTag ?? tags.Last();
        
        await _rabbitMq.CancelConsumerAsync(tagToRemove, ct);
        
        lock(tags) { tags.Remove(tagToRemove); }
        
        if (tags.Count == 0)
            _activeConsumers.TryRemove(key, out _);
        
        _logger.LogInformation("➖ Removed worker from queue '{Queue}' for tenant {TenantId} (tag: {Tag})", 
            queue, tenantId?.ToString("N") ?? "*", tagToRemove);
        
        return true;
    }

    /// <summary>
    /// Возвращает количество активных воркеров на очереди (для всех тенантов или конкретного).
    /// </summary>
    public int GetWorkerCount(string queue, Guid? tenantId = null)
    {
        var key = MakeKey(queue, tenantId);
        return _activeConsumers.TryGetValue(key, out var tags) ? tags.Count : 0;
    }

    /// <summary>
    /// Возвращает статистику по всем очередям (сумма по всем тенантам для простоты демо).
    /// </summary>
    public Dictionary<string, int> GetStats()
    {
        var knownQueues = new[] { "sim.received", "sim.screening", "sim.tech" };
        return knownQueues.ToDictionary(
            q => q,
            q => _activeConsumers
                .Where(kvp => kvp.Key.StartsWith(q + ":"))
                .Sum(kvp => kvp.Value.Count)
        );
    }
}