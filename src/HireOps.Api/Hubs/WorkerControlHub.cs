using HireOps.Application.Simulations.Commands.ProcessScreening;
using HireOps.Application.Workers;
using HireOps.Domain.Simulations;
using Microsoft.AspNetCore.SignalR;

namespace HireOps.Api.Hubs;

public class WorkerControlHub(
    WorkerManagerService workerManager,
    ILogger<WorkerControlHub> logger)
    : Hub
{
    public async Task AddWorker(string queue)
    {
        if (queue == "sim.received")
        {
            await workerManager.AddMediatedWorkerAsync<ApplicantMessage, ProcessScreeningCommand>(
                queue,
                msg => 
                    new ProcessScreeningCommand(msg.Id, msg.TenantId, msg.Skills));
        }
        
        await Clients.All.SendAsync("WorkersUpdated", workerManager.GetStats());
    }
    
    public async Task RemoveWorker(string queue)
    {
        await workerManager.RemoveWorkerAsync(queue);
        await Clients.All.SendAsync("WorkersUpdated", workerManager.GetStats());
    }

    public async Task UpdatePrefetch(int prefetchCount)
    {
        // Применяем глобально для канала (можно доработать на очередь)
        // В реальном проекте: хранить конфиг на воркера
        logger.LogInformation("🔧 Updated prefetch to {Prefetch}", prefetchCount);
        await Clients.All.SendAsync("PrefetchUpdated", prefetchCount);
    }
}