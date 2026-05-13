using HireOps.Application.Services;
using HireOps.Application.Simulations.Commands.ProcessHrDecision;
using HireOps.Application.Simulations.Commands.ProcessScreening;
using HireOps.Application.Simulations.Commands.ProcessTechReview;
using HireOps.Application.Workers;
using HireOps.Domain.Interfaces;
using HireOps.Domain.Simulations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace HireOps.Application.Hubs;

public class DashboardHub(
    IWorkerManagerService workerManager,
    ILogger<DashboardHub> logger,
    IWaveTrackerService? waveTracker = null)
    : Hub
{
    public async Task AddWorker(string queue, Guid? tenantId = null)
    {
        try
        {
            logger.LogInformation("➕ AddWorker requested for queue: {Queue}, tenant: {TenantId}", 
                queue, tenantId?.ToString("N") ?? "*");
            
            switch (queue)
            {
                case "sim.received":
                    await workerManager.AddMediatedWorkerAsync<ApplicantMessage, ProcessScreeningCommand>(
                        queue,
                        tenantId,
                        msg => new ProcessScreeningCommand(
                            msg.Id, 
                            msg.TenantId, 
                            msg.Skills,
                            waveTracker?.GetActiveWaveId())); // 👈 Передаём активный WaveId
                    break;
                    
                case "sim.tech":
                    await workerManager.AddMediatedWorkerAsync<ScreeningResult, ProcessTechReviewCommand>(
                        queue,
                        tenantId,
                        msg => new ProcessTechReviewCommand(
                            msg.ApplicantId, 
                            msg.TenantId, 
                            msg.Score,
                            waveTracker?.GetActiveWaveId())); // 👈 Передаём активный WaveId
                    break;
                
                case "sim.screening":
                    await workerManager.AddMediatedWorkerAsync<TechReviewResult, ProcessHrDecisionCommand>(
                        queue,
                        tenantId,
                        msg => new ProcessHrDecisionCommand(
                            msg.ApplicantId,
                            msg.TenantId,
                            msg.Recommended,
                            waveTracker?.GetActiveWaveId())); // 👈 Передаём активный WaveId
                    break;
                    
                default:
                    logger.LogWarning("⚠️ Unknown queue: {Queue}", queue);
                    return;
            }

            await Clients.All.SendAsync("WorkersUpdated", workerManager.GetStats());
            logger.LogInformation("✅ Worker added to {Queue}, total: {Count}", 
                queue, workerManager.GetWorkerCount(queue, tenantId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error in AddWorker for queue {Queue}", queue);
            throw;
        }
    }

    public async Task RemoveWorker(string queue, Guid? tenantId = null)
    {
        try
        {
            logger.LogInformation("➖ RemoveWorker requested for queue: {Queue}, tenant: {TenantId}", 
                queue, tenantId?.ToString("N") ?? "*");
            
            await workerManager.RemoveWorkerAsync(queue, tenantId);
            
            await Clients.All.SendAsync("WorkersUpdated", workerManager.GetStats());
            logger.LogInformation("✅ Worker removed from {Queue}, remaining: {Count}", 
                queue, workerManager.GetWorkerCount(queue, tenantId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error in RemoveWorker for queue {Queue}", queue);
            throw;
        }
    }
    
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("WorkersUpdated", workerManager.GetStats());
        
        // 🔹 Если есть активная волна — отправляем прогресс новому клиенту
        if (waveTracker != null)
        {
            var progress = waveTracker.GetActiveWaveProgress();
            if (progress != null)
            {
                await Clients.Caller.SendAsync("WaveProgress", progress);
            }
        }
        
        await base.OnConnectedAsync();
    }
}