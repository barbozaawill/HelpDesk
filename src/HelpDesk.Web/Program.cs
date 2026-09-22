using Blazored.LocalStorage;
using HelpDesk.Web;
using HelpDesk.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Endereco da API. Vem de wwwroot/appsettings.json pra dar pra trocar sem recompilar
// (no Docker o navegador acessa a API pela porta publicada no host).
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<ProvedorDeAutenticacao>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ProvedorDeAutenticacao>());

builder.Services.AddScoped<ClienteApi>();
builder.Services.AddScoped<ServicoDeSessao>();

await builder.Build().RunAsync();
