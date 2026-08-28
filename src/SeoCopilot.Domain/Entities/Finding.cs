using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Domain.Entities;

/// <summary>Kural motorunun bir sayfada tespit ettigi tek bir sorun.</summary>
public class Finding
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PageId { get; private set; }
    public string RuleCode { get; private set; } = string.Empty;
    public Severity Severity { get; private set; }
    public string Message { get; private set; } = string.Empty;

    private Finding() { } // EF

    public Finding(string ruleCode, Severity severity, string message)
    {
        RuleCode = ruleCode;
        Severity = severity;
        Message = message;
    }
}
