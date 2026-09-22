using HelpDesk.Infrastructure;
using HelpDesk.Worker.Consumidores;
using HelpDesk.Worker.Processadores;
using HelpDesk.Worker.Servicos;

var builder = WebApplication.CreateBuilder(args);

// Banco, Redis e RabbitMQ (a MESMA extensao que a API usa).
builder.Services.AddInfraestrutura(builder.Configuration);

builder.Services.AddSingleton<ProcessadorDeEventos>();
builder.Services.AddHostedService<ConsumidorDeChamados>();
builder.Services.AddHostedService<ReconciliadorDeMetricas>();

// O Worker e um "web app" so pra expor /health (util pro Docker/monitoramento).
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/", () => "HelpDesk.Worker esta rodando. Veja /health.");
app.MapHealthChecks("/health");

app.Run();
