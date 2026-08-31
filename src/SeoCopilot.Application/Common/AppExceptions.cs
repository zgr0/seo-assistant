namespace SeoCopilot.Application.Common;

public sealed class AuthException(string message) : Exception(message);

public sealed class ConflictException(string message) : Exception(message);

public sealed class NotFoundException(string message) : Exception(message);
