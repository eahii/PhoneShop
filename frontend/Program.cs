using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using frontend;
using Microsoft.AspNetCore.Components.Authorization;
using frontend.Authentication;
using frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with default handler
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7032") // Backend's HTTPS URL
});

// Add Blazored.LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Register AuthService
builder.Services.AddScoped<AuthService>();

// Register CustomAuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Add Authorization Core
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();