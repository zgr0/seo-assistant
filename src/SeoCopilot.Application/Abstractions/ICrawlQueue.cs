namespace SeoCopilot.Application.Abstractions;

/// <summary>Arka plan is kuyruguna crawl atmak icin. Infrastructure/Api Hangfire ile implemente eder.</summary>
public interface ICrawlQueue
{
    void Enqueue(Guid crawlId);
}
