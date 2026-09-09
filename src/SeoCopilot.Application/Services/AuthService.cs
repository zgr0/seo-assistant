using System.Text.RegularExpressions;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Domain.Entities.Tenancy;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services;

/// <summary>Kayit / giris / refresh akislari. Sifre bcrypt, oturum JWT + rotasyonlu refresh token.</summary>
public sealed partial class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokens)
{
    private const int TrialPageQuota = 100;
    private const long TrialAiTokenQuota = 200_000;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);

        if (request.Password.Length < 8)
            throw new AuthException("Sifre en az 8 karakter olmali");
        if (string.IsNullOrWhiteSpace(request.TenantName))
            throw new AuthException("Kiracı adı zorunlu");
        if (await users.EmailExistsAsync(email, ct))
            throw new ConflictException("Bu e-posta zaten kayıtlı");

        var tenant = new Tenant
        {
            Name = request.TenantName.Trim(),
            Slug = await UniqueSlugAsync(request.TenantName, ct),
            Plan = TenantPlan.Trial,
            PageQuota = TrialPageQuota,
            AiTokenQuota = TrialAiTokenQuota,
            QuotaResetAt = DateTimeOffset.UtcNow.AddMonths(1),
            IsActive = true
        };
        await users.AddTenantAsync(tenant, ct);

        var user = new User
        {
            TenantId = tenant.Id,
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Role = UserRole.Owner
        };
        await users.AddUserAsync(user, ct);
        await users.SaveChangesAsync(ct);

        return await IssueAsync(user, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var user = await users.FindByEmailAsync(email, ct);

        if (user is null)
        {
            // Sabit maliyetli islem — kullanici var/yok zamanlama sizintisini azalt.
            passwordHasher.Hash(request.Password);
            throw new AuthException("E-posta veya şifre hatalı");
        }
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new AuthException("E-posta veya şifre hatalı");
        if (user.Tenant is { IsActive: false })
            throw new AuthException("Kiracı pasif durumda");

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await users.SaveChangesAsync(ct);

        return await IssueAsync(user, ct);
    }

    public async Task<AuthResult> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var incomingHash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await users.FindRefreshTokenAsync(incomingHash, ct)
            ?? throw new AuthException("Refresh token geçersiz");

        if (!stored.IsActive)
            throw new AuthException("Refresh token süresi dolmuş veya iptal edilmiş");

        // Rotasyon: eskiyi iptal et, yenisini uret.
        stored.RevokedAt = DateTimeOffset.UtcNow;

        var user = stored.User
            ?? await users.FindByIdAsync(stored.UserId, ct)
            ?? throw new AuthException("Kullanıcı bulunamadı");

        return await IssueAsync(user, ct);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await users.FindRefreshTokenAsync(hash, ct);
        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await users.SaveChangesAsync(ct);
        }
    }

    private async Task<AuthResult> IssueAsync(User user, CancellationToken ct)
    {
        var access = tokens.CreateAccessToken(user);
        var refresh = tokens.CreateRefreshToken();

        await users.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refresh.TokenHash,
            ExpiresAt = refresh.ExpiresAt
        }, ct);
        await users.SaveChangesAsync(ct);

        return new AuthResult(
            access.Token,
            access.ExpiresAt,
            refresh.RawToken,
            refresh.ExpiresAt,
            new UserInfo(user.Id, user.Email, user.FullName, user.Role.ToString().ToLowerInvariant(), user.TenantId));
    }

    private async Task<string> UniqueSlugAsync(string name, CancellationToken ct)
    {
        var baseSlug = Slugify(name);
        if (baseSlug.Length == 0) baseSlug = "tenant";

        var slug = baseSlug;
        var i = 1;
        while (await users.SlugExistsAsync(slug, ct))
            slug = $"{baseSlug}-{++i}";
        return slug;
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static string Slugify(string value)
    {
        var lower = value.Trim().ToLowerInvariant()
            .Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g')
            .Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c');
        lower = NonSlug().Replace(lower, "-").Trim('-');
        return DoubleDash().Replace(lower, "-");
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlug();

    [GeneratedRegex("-{2,}")]
    private static partial Regex DoubleDash();
}
