using AgentWithSPKnowledgeViaRetrieval.Models;

namespace AgentWithSPKnowledgeViaRetrieval.Services;

public interface IRetrievalService
{
    Task<List<RetrievedContent>> SearchAsync(string query, string filterExpression, CancellationToken cancellationToken = default);
}
