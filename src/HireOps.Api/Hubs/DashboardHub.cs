using HireOps.Application.Simulations.Commands.ProcessScreening;
using HireOps.Application.Workers;
using HireOps.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace HireOps.Api.Hubs;

public class DashboardHub : Hub
{
    private readonly IWorkerManagerService _workerManager;
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(IWorkerManagerService workerManager, ILogger<DashboardHub> logger)
    {
        _workerManager = workerManager;
        _logger = logger;
    }

    public async Task AddWorker(string queue)
    {
        if (queue == "sim.received")
        {
            await _workerManager.AddMediatedWorkerAsync<Domain.Simulations.ApplicantMessage, ProcessScreeningCommand>(
                queue, msg => new ProcessScreeningCommand(msg.Id, msg.TenantId, msg.Skills));
        }
        // Для демо: остальные очереди пока заглушки или аналогичная логика
        await Clients.All.SendAsync("WorkersUpdated", _workerManager.GetStats());
        _logger.LogInformation("➕ Worker added to {Queue}", queue);
    }

    public async Task RemoveWorker(string queue)
    {
        await _workerManager.RemoveWorkerAsync(queue);
        await Clients.All.SendAsync("WorkersUpdated", _workerManager.GetStats());
        _logger.LogInformation("➖ Worker removed from {Queue}", queue);
    }

    public async Task UpdatePrefetch(int prefetchCount)
    {
        // Применяем глобально к каналу RabbitMQ
        // В реальном проекте: хранить конфиг на воркера
        await Clients.All.SendAsync("PrefetchUpdated", prefetchCount);
        _logger.LogInformation("🔧 Prefetch updated to {Count}", prefetchCount);
    }
}