using HireOps.Api.DependencyInjection;
using HireOps.Application;
using HireOps.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Регистрация слоёв через статические расширения
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddApi();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();