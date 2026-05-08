using Serilog;
using HireOps.Api.DependencyInjection;
using HireOps.Application;
using HireOps.Application.Hubs;
using HireOps.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Host.UseSerilog((ctx, cfg) => cfg
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:53499")
);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddApi();

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
var app = builder.Build();
app.MapHub<DashboardHub>("/hubs/dashboard");
app.UseHttpsRedirection();
app.MapControllers();

Log.Information("🚀 HireOps API started. Phase 0: DB + Seq connected.");
app.Run();