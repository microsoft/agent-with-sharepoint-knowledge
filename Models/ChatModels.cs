namespace AgentWithSPKnowledgeViaRetrieval.Models;

public class ChatRequest
{
    public string FileName { get; set; } = string.Empty;

}

public class ChatResponse
{
    public string LlmResponse { get; set; } = string.Empty;
    public string FileAuthor { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class RetrievedContent
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string FileAuthor { get; set; } = string.Empty;
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
