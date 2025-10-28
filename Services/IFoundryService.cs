using AgentWithSPKnowledgeViaRetrieval.Models;

namespace AgentWithSPKnowledgeViaRetrieval.Services;

public interface IFoundryService
{
    Task<string> GenerateResponseAsync(
        List<RetrievedContent> rulesContext,
        RetrievedContent filesContext,
        CancellationToken cancellationToken = default);
}
