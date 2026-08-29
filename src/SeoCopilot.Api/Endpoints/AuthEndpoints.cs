using SeoCopilot.Application.Dtos;
using SeoCopilot.Application.Services;

namespace SeoCopilot.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        group.MapPost("/register", async (RegisterRequest req, AuthService auth, CancellationToken ct) =>
        {
            var result = await auth.RegisterAsync(req, ct);
            return Results.Created($"/api/users/{result.User.Id}", result);
        });

        group.MapPost("/login", async (LoginRequest req, AuthService auth, CancellationToken ct) =>
            Results.Ok(await auth.LoginAsync(req, ct)));

        group.MapPost("/refresh", async (RefreshRequest req, AuthService auth, CancellationToken ct) =>
            Results.Ok(await auth.RefreshAsync(req, ct)));

        group.MapPost("/logout", async (LogoutRequest req, AuthService auth, CancellationToken ct) =>
        {
            await auth.LogoutAsync(req, ct);
            return Results.NoContent();
        });

        return app;
    }
}
