using HireOps.Domain.Interfaces;
using HireOps.Domain.Simulations;
using MediatR;

namespace HireOps.Application.Workers;

public interface IWorkerManagerService
{
    /// <summary>
    /// Добавляет общего воркера (без привязки к тенанту).
    /// </summary>
    Task<ConsumerSubscription> AddWorkerAsync<T>(
        string queue, 
        Func<T, CancellationToken, Task> handler,
        CancellationToken ct = default) where T : class;

    /// <summary>
    /// Добавляет воркера с медиацией и привязкой к тенанту.
    /// </summary>
    Task<ConsumerSubscription> AddMediatedWorkerAsync<TMessage, TCommand>(
        string queue,
        Guid? tenantId,
        Func<TMessage, TCommand> mapToCommand,
        CancellationToken ct = default) where TMessage : class where TCommand : IRequest;

    /// <summary>
    /// Удаляет воркера. Если указан tenantId — удаляет только консьюмера этого тенанта.
    /// </summary>
    Task<bool> RemoveWorkerAsync(
        string queue, 
        Guid? tenantId = null, 
        string? consumerTag = null, 
        CancellationToken ct = default);

    /// <summary>
    /// Возвращает количество активных воркеров на очереди (для всех тенантов или конкретного).
    /// </summary>
    int GetWorkerCount(string queue, Guid? tenantId = null);

    /// <summary>
    /// Возвращает статистику по всем очередям (сумма по всем тенантам).
    /// </summary>
    Dictionary<string, int> GetStats();
}