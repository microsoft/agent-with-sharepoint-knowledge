namespace AgentWithSPKnowledgeViaRetrieval.Services;

public interface IMailService
{
  Task<(bool Success, string Message)> SendMailAsync(
    string recipient,
    string emailContent,
    string fileName,
    CancellationToken cancellationToken = default
  );
}