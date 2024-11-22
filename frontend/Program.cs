// PhoneShop/frontend/Program.cs
using Blazored.LocalStorage;
using frontend.Authentication;
using frontend.Handlers;
using frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using frontend; // Added using directive for the 'frontend' namespace

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add Blazored.LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Register CustomAuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Register AuthService
builder.Services.AddScoped<AuthService>();

// Register TokenAuthorizationHandler
builder.Services.AddTransient<TokenAuthorizationHandler>();

// Configure HttpClient with TokenAuthorizationHandler
builder.Services.AddHttpClient("BackendAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7032/");
})
.AddHttpMessageHandler<TokenAuthorizationHandler>();

// Register a default HttpClient that uses the named client
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("BackendAPI"));

// Add Authorization Core
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();