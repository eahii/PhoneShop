using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Backend.Data;
using Backend.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
    });

// Configure CORS to allow credentials and specific origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("https://localhost:7133") // Frontend's HTTPS URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure JWT Authentication
var jwtKey = "Your_Secret_Key_Should_Be_Long_Enough"; // Ensure this is stored securely
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, // Set to true and specify valid issuers in production
        ValidateAudience = false, // Set to true and specify valid audiences in production
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// Add Authorization
builder.Services.AddAuthorization();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Used Phones API", Version = "v1" });
    c.SwaggerDoc("accountmanagerapi", new OpenApiInfo { Title = "Account Manager API", Version = "v1" });
    c.SwaggerDoc("loginapi", new OpenApiInfo { Title = "Login API", Version = "v1" });
    c.SwaggerDoc("phonesapi", new OpenApiInfo { Title = "Phones API", Version = "v1" });
    c.SwaggerDoc("cartapi", new OpenApiInfo { Title = "Cart API", Version = "v1" });
    c.SwaggerDoc("offerapi", new OpenApiInfo { Title = "Offer API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Used Phones API V1");
        c.SwaggerEndpoint("/swagger/accountmanagerapi/swagger.json", "Account Manager API V1");
        c.SwaggerEndpoint("/swagger/loginapi/swagger.json", "Login API V1");
        c.SwaggerEndpoint("/swagger/phonesapi/swagger.json", "Phones API V1");
        c.SwaggerEndpoint("/swagger/cartapi/swagger.json", "Cart API V1");
        c.SwaggerEndpoint("/swagger/offerapi/swagger.json", "Offer API V1");
    });
}

app.UseHttpsRedirection();

// Apply CORS policy before Authentication and Authorization
app.UseCors("AllowBlazorClient");

app.UseAuthentication();
app.UseAuthorization();

// Map API endpoints
app.MapLoginApi();
app.MapAccountManagerApi();
app.MapPhonesApi();
app.MapCartApi();
app.MapOfferApi();

app.Run();