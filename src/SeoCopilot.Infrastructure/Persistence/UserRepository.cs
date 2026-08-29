using Microsoft.EntityFrameworkCore;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Domain.Entities.Tenancy;

namespace SeoCopilot.Infrastructure.Persistence;

public sealed class UserRepository(SeoCopilotDbContext db) : IUserRepository
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Email == email, ct);

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.Include(u => u.Tenant).FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
        db.Users.Include(u => u.Tenant).FirstOrDefaultAsync(u => u.Id == userId, ct);

    public async Task AddTenantAsync(Tenant tenant, CancellationToken ct = default) =>
        await db.Tenants.AddAsync(tenant, ct);

    public async Task AddUserAsync(User user, CancellationToken ct = default) =>
        await db.Users.AddAsync(user, ct);

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(token, ct);

    public Task<RefreshToken?> FindRefreshTokenAsync(byte[] tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        db.Tenants.AnyAsync(t => t.Slug == slug, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
