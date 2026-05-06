using HireOps.Application.Simulations.Commands.ProcessHrDecision;
using HireOps.Application.Simulations.Commands.ProcessScreening;
using HireOps.Application.Simulations.Commands.ProcessTechReview;
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

    public async Task AddWorker(string queue)
    {
        try
        {
            _logger.LogInformation("➕ AddWorker requested for queue: {Queue}", queue);
            
            switch (queue)
            {
                case "sim.received":
                    await _workerManager.AddMediatedWorkerAsync<ApplicantMessage, ProcessScreeningCommand>(
                        queue,
                        msg => new ProcessScreeningCommand(msg.Id, msg.TenantId, msg.Skills));
                    break;
                    
                case "sim.screening":
                    await _workerManager.AddMediatedWorkerAsync<ScreeningResult, ProcessTechReviewCommand>(
                        queue,
                        msg => new ProcessTechReviewCommand(msg.ApplicantId, msg.TenantId, msg.Score));
                    break;
                    
                case "sim.tech":
                    await _workerManager.AddMediatedWorkerAsync<TechReviewResult, ProcessHrDecisionCommand>(
                        queue,
                        msg => new ProcessHrDecisionCommand(msg.ApplicantId, msg.TenantId, msg.Recommended));
                    break;
                    
                case "sim.hr":
                    // Для финального этапа просто логгируем (или можно создать свой хендлер)
                    await _workerManager.AddWorkerAsync<HrDecisionResult>(
                        queue,
                        async (msg, ct) => 
                        {
                            _logger.LogInformation("🤝 Final decision for applicant {Id}: {Decision}", 
                                msg.ApplicantId, msg.Hired ? "HIRED" : "REJECTED");
                            await Task.CompletedTask;
                        });
                    break;
                    
                default:
                    _logger.LogWarning("⚠️ Unknown queue: {Queue}", queue);
                    return;
            }

            // 🔹 Пушим обновлённую статистику всем клиентам
            await Clients.All.SendAsync("WorkersUpdated", _workerManager.GetStats());
            _logger.LogInformation("✅ Worker added to {Queue}, total: {Count}", 
                queue, _workerManager.GetWorkerCount(queue));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in AddWorker for queue {Queue}", queue);
            throw; // Пробрасываем ошибку клиенту, чтобы он увидел её в консоли
        }
    }

    public async Task RemoveWorker(string queue)
    {
        try
        {
            _logger.LogInformation("➖ RemoveWorker requested for queue: {Queue}", queue);
            
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
        // Пока просто пушим обновление (реальное применение — в RabbitMqService)
        await Clients.All.SendAsync("PrefetchUpdated", prefetchCount);
        _logger.LogInformation("🔧 Prefetch updated to {Count}", prefetchCount);
    }
}