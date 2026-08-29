namespace SeoCopilot.Application.Common;

/// <summary>Kimlik dogrulama basarisiz — HTTP 401.</summary>
public sealed class AuthException(string message) : Exception(message);

/// <summary>Kaynak cakismasi (orn. e-posta zaten kayitli) — HTTP 409.</summary>
public sealed class ConflictException(string message) : Exception(message);
