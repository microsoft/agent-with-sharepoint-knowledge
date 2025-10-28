using AgentWithSPKnowledgeViaRetrieval.Models;

namespace AgentWithSPKnowledgeViaRetrieval.Services;

public interface IChatService
{
    Task<ChatResponse> ProcessChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
