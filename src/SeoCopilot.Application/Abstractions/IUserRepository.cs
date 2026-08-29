using SeoCopilot.Domain.Entities.Tenancy;

namespace SeoCopilot.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>E-posta ile kullanici (Tenant dahil). E-posta global benzersiz varsayilir.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> FindByIdAsync(Guid userId, CancellationToken ct = default);

    Task AddTenantAsync(Tenant tenant, CancellationToken ct = default);
    Task AddUserAsync(User user, CancellationToken ct = default);

    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>Hash ile refresh token (User dahil).</summary>
    Task<RefreshToken?> FindRefreshTokenAsync(byte[] tokenHash, CancellationToken ct = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
