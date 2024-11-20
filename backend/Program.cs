using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Backend.Data;
using Backend.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
    });

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", builder =>
    {
        builder.WithOrigins("http://localhost:5058") // Adjust the origin to match your frontend URL
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Used Phones API", Version = "v1" });

    // Ensure APIs are accessible in Swagger
    c.SwaggerDoc("accountmanagerapi", new OpenApiInfo { Title = "Account Manager API", Version = "v1" });
    c.SwaggerDoc("loginapi", new OpenApiInfo { Title = "Login API", Version = "v1" });
    c.SwaggerDoc("phonesapi", new OpenApiInfo { Title = "Phones API", Version = "v1" });
    c.SwaggerDoc("cartapi", new OpenApiInfo { Title = "Cart API", Version = "v1" });
    c.SwaggerDoc("offerapi", new OpenApiInfo { Title = "Offer API", Version = "v1" });
});

var app = builder.Build();

// Initialize the database asynchronously
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    DatabaseInitializer.Initialize().GetAwaiter().GetResult();
}

// Use CORS policy before registering API routes
app.UseCors("AllowBlazorClient");

// Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Used Phones API V1");
    c.SwaggerEndpoint("/swagger/accountmanagerapi/swagger.json", "Account Manager API V1");
    c.SwaggerEndpoint("/swagger/loginapi/swagger.json", "Login API V1");
    c.SwaggerEndpoint("/swagger/phonesapi/swagger.json", "Phones API V1");
    c.SwaggerEndpoint("/swagger/cartapi/swagger.json", "Cart API V1");
    c.SwaggerEndpoint("/swagger/offerapi/swagger.json", "Offer API V1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
});

// Map API endpoints
app.MapLoginApi();
app.MapAccountManagerApi();
app.MapPhonesApi();
app.MapCartApi();      // Added mapping for Cart API
app.MapOfferApi();     // Added mapping for Offer API

app.Run();