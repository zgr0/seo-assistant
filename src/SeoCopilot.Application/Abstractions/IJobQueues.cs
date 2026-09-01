namespace SeoCopilot.Application.Abstractions;

/// <summary>Icerik uretimini arka plana atar (LLM cagrisi istek suresini asar).</summary>
public interface IContentQueue
{
    void Enqueue(Guid jobId);
}

/// <summary>Rapor uretimini arka plana atar.</summary>
public interface IReportQueue
{
    void Enqueue(Guid reportId);
}
