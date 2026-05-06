using HireOps.Application.Simulations.Commands.ProcessHrDecision;
using HireOps.Application.Simulations.Commands.ProcessScreening;
using HireOps.Application.Simulations.Commands.ProcessTechReview;
using HireOps.Application.Workers;
using HireOps.Domain.Interfaces;
using HireOps.Domain.Simulations;
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

    // 🔹 НОВОЕ: параметр tenantId (nullable, чтобы поддержать старый вызов без тенанта)
    public async Task AddWorker(string queue, Guid? tenantId = null)
    {
        try
        {
            _logger.LogInformation("➕ AddWorker requested for queue: {Queue}, tenant: {TenantId}", 
                queue, tenantId?.ToString("N") ?? "all");
            
            switch (queue)
            {
                case "sim.received":
                    await _workerManager.AddMediatedWorkerAsync<ApplicantMessage, ProcessScreeningCommand>(
                        queue,
                        tenantId, // 👈 Передаём tenantId
                        msg => new ProcessScreeningCommand(msg.Id, msg.TenantId, msg.Skills));
                    break;
                    
                case "sim.screening":
                    await _workerManager.AddMediatedWorkerAsync<ScreeningResult, ProcessTechReviewCommand>(
                        queue,
                        tenantId, // 👈 Передаём tenantId
                        msg => new ProcessTechReviewCommand(msg.ApplicantId, msg.TenantId, msg.Score));
                    break;
                    
                case "sim.tech":
                    await _workerManager.AddMediatedWorkerAsync<TechReviewResult, ProcessHrDecisionCommand>(
                        queue,
                        tenantId, // 👈 Передаём tenantId
                        msg => new ProcessHrDecisionCommand(msg.ApplicantId, msg.TenantId, msg.Recommended));
                    break;
                    
                default:
                    _logger.LogWarning("⚠️ Unknown queue: {Queue}", queue);
                    return;
            }

            await Clients.All.SendAsync("WorkersUpdated", _workerManager.GetStats());
            _logger.LogInformation("✅ Worker added to {Queue}, total: {Count}", 
                queue, _workerManager.GetWorkerCount(queue));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in AddWorker for queue {Queue}", queue);
            throw;
        }
    }

    // 🔹 Аналогично для RemoveWorker
    public async Task RemoveWorker(string queue, Guid? tenantId = null)
    {
        try
        {
            _logger.LogInformation("➖ RemoveWorker requested for queue: {Queue}, tenant: {TenantId}", 
                queue, tenantId?.ToString("N") ?? "all");
            
            await _workerManager.RemoveWorkerAsync(queue);
            
            await Clients.All.SendAsync("WorkersUpdated", _workerManager.GetStats());
            _logger.LogInformation("✅ Worker removed from {Queue}, remaining: {Count}", 
                queue, _workerManager.GetWorkerCount(queue));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in RemoveWorker for queue {Queue}", queue);
            throw;
        }
    }

    public async Task UpdatePrefetch(int prefetchCount)
    {
        await Clients.All.SendAsync("PrefetchUpdated", prefetchCount);
        _logger.LogInformation("🔧 Prefetch updated to {Count}", prefetchCount);
    }
    
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("WorkersUpdated", _workerManager.GetStats());
        await base.OnConnectedAsync();
    }
}