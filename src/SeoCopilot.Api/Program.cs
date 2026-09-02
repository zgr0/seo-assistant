using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SeoCopilot.Api.Adapters;
using SeoCopilot.Api.Endpoints;
using SeoCopilot.Api.Infrastructure;
using SeoCopilot.Application;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Crawler;
using SeoCopilot.Infrastructure;
using SeoCopilot.Infrastructure.Auth;
using SeoCopilot.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// --- Katmanlar ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCrawler(builder.Configuration);

// Rules koprusu + kuyruklar
builder.Services.AddSingleton<IRuleRunner, RuleRunnerAdapter>();
builder.Services.AddScoped<ICrawlQueue, HangfireCrawlQueue>();
builder.Services.AddScoped<IContentQueue, HangfireContentQueue>();
builder.Services.AddScoped<IReportQueue, HangfireReportQueue>();

// --- Hangfire ---
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Postgres"))));

// Worker'i kapatmak icin Hangfire:EnableServer=false — is uretmeyen (yalniz API) dusumler
// ve testler icin. Kuyruga atma ve /hangfire panosu her halukarda calisir.
if (builder.Configuration.GetValue("Hangfire:EnableServer", true))
    builder.Services.AddHangfireServer();

// --- Auth ---
var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key en az 32 bayt olmali");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Konteyner acilisinda semayi kurmak icin. Varsayilan kapali — tek surum calistiran
// dagitimlar (compose) icindir; birden fazla replika varsa migration ayri bir adim olmali.
if (builder.Configuration.GetValue("Database:AutoMigrate", false))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<SeoCopilotDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();

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
app.MapAuthEndpoints();
app.MapSiteEndpoints();
app.MapCrawlEndpoints();
app.MapPageEndpoints();
app.MapIssueEndpoints();
app.MapBrandProfileEndpoints();
app.MapContentEndpoints();
app.MapReportEndpoints();
app.MapDashboardEndpoints();

app.Run();

public partial class Program;
