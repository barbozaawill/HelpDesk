using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using HelpDesk.Api.Autenticacao;
using HelpDesk.Api.Configuracao;
using HelpDesk.Api.Middleware;
using HelpDesk.Api.Servicos;
using HelpDesk.Infrastructure;
using HelpDesk.Infrastructure.Persistencia;
using HelpDesk.Infrastructure.Seguranca;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Infra (banco, redis, rabbit)
builder.Services.AddInfraestrutura(builder.Configuration);

// ---------------------------------------------------------------- Servicos da aplicacao
builder.Services.Configure<OpcoesJwt>(builder.Configuration.GetSection(OpcoesJwt.Secao));
builder.Services.AddSingleton<ServicoDeToken>();
builder.Services.AddScoped<ServicoDeAutenticacao>();
builder.Services.AddScoped<ServicoDeChamados>();
builder.Services.AddScoped<ServicoDeDashboard>();

// ---------------------------------------------------------------- Autenticacao JWT
var jwt = builder.Configuration.GetSection(OpcoesJwt.Secao).Get<OpcoesJwt>() ?? new OpcoesJwt();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Deixa os claims chegarem com o nome original do token ("sub", "email", "nome"...)
        // em vez de o handler renomear pra URLs gigantes do WS-Federation.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Emissor,
            ValidAudience = jwt.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.ChaveSecreta)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = "nome"
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------- CORS (o Blazor chama a API do navegador)
const string PoliticaCors = "blazor";
var origensPermitidas = builder.Configuration.GetSection("Cors:Origens").Get<string[]>()
                        ?? new[] { "http://localhost:5001" };

builder.Services.AddCors(options => options.AddPolicy(PoliticaCors, policy =>
    policy.WithOrigins(origensPermitidas).AllowAnyHeader().AllowAnyMethod()));

// ---------------------------------------------------------------- Controllers + Swagger
builder.Services
    .AddControllers()
    .AddJsonOptions(opt =>
        // Aceita/retorna enums como texto ("Alta") em vez de numero (2).
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HelpDesk API", Version = "v1" });

    // Habilita o botao "Authorize" no Swagger pra colar o token do /api/auth/login.
    var esquema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Cole o token retornado no /api/auth/login (sem escrever 'Bearer').",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", esquema);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquema] = Array.Empty<string>() });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// ---------------------------------------------------------------- Migrations + seed no boot
// Pratico pra estudo/demo. Num sistema grande isso seria um passo separado do deploy.
await PrepararBancoAsync(app);

// ---------------------------------------------------------------- Pipeline HTTP
app.UseMiddleware<MiddlewareDeErros>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(PoliticaCors);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();


static async Task PrepararBancoAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var ctx = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
        var hash = scope.ServiceProvider.GetRequiredService<IHashDeSenha>();
        await InicializadorDoBanco.PrepararAsync(ctx, hash);
        log.LogInformation("Banco pronto (migrations aplicadas + seed).");
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Falha ao preparar o banco no boot da API.");
        throw;
    }
}

// Deixa a classe Program visivel para um projeto de teste de integracao
// (WebApplicationFactory<Program>), caso seja adicionado depois.
public partial class Program;
