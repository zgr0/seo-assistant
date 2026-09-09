using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SeoCopilot.Application.Common;

namespace SeoCopilot.Api.Infrastructure;

/// <summary>Uygulama istisnalarini ProblemDetails'e cevirir.</summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            AuthException => (StatusCodes.Status401Unauthorized, "Kimlik doğrulama başarısız"),
            ConflictException => (StatusCodes.Status409Conflict, "Çakışma"),
            NotFoundException => (StatusCodes.Status404NotFound, "Bulunamadı"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Geçersiz istek"),
            _ => (StatusCodes.Status500InternalServerError, "Sunucu hatası")
        };

        ctx.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            Exception = ex,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == StatusCodes.Status500InternalServerError ? null : ex.Message
            }
        });
    }
}
