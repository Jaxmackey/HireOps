using HireOps.Domain.Simulations;
using MediatR;

namespace HireOps.Domain.Interfaces;

public interface IWorkerManagerService
{
    Task<ConsumerSubscription> AddWorkerAsync<T>(
        string queue,
        Func<T, CancellationToken, Task> handler,
        CancellationToken ct = default) where T : class;

    Task<ConsumerSubscription> AddMediatedWorkerAsync<TMessage, TCommand>(
        string queue,
        Guid? tenantId,
        Func<TMessage, TCommand> mapToCommand,
        CancellationToken ct = default) where TMessage : class where TCommand : IRequest;

    Task<bool> RemoveWorkerAsync(string queue, string? consumerTag = null,
        CancellationToken ct = default);

    int GetWorkerCount(string queue);
    Dictionary<string, int> GetStats();
}