using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SeoCopilot.Api.Adapters;
using SeoCopilot.Api.Endpoints;
using SeoCopilot.Api.Infrastructure;
using SeoCopilot.Application;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Crawler;
using SeoCopilot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// --- Katmanlar ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCrawler();

// Rules koprüsü + kuyruk
builder.Services.AddSingleton<IRuleRunner, RuleRunnerAdapter>();
builder.Services.AddScoped<ICrawlQueue, HangfireCrawlQueue>();

// --- Hangfire ---
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Postgres"))));
builder.Services.AddHangfireServer();

// --- Auth ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-insecure-key-change-me-please-32b";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "seocopilot",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "seocopilot",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthFilter(app.Environment)]
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapCrawlEndpoints();

app.Run();

public partial class Program;
