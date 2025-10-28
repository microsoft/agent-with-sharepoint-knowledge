using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using AgentWithSPKnowledgeViaRetrieval.Models;

namespace AgentWithSPKnowledgeViaRetrieval.Services;

public class ChatService : IChatService
{
    private readonly IRetrievalService _retrievalService;
    private readonly IFoundryService _foundryService;
    private readonly ILogger<ChatService> _logger;
    private readonly Microsoft365Options _microsoft365Options;


    public ChatService(
        IRetrievalService retrievalService,
        IFoundryService foundryService,
        IOptions<Microsoft365Options> microsoft365Options,
        ILogger<ChatService> logger)
    {
        _retrievalService = retrievalService;
        _foundryService = foundryService;
        _logger = logger;
        _microsoft365Options = microsoft365Options.Value;
    }

    public async Task<ChatResponse> ProcessChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var rulesFilter = _microsoft365Options.FilterExpression;
            var fileContentFilter = _microsoft365Options.FilterExpression;

            string rulesQuery = "What are all the rules in rulebooks that apply to new policies";

            // Step 1: Retrieve relevant content from Microsoft 365
            var retrievedRulesContext = await _retrievalService.SearchAsync(
                rulesQuery,
                rulesFilter,
                cancellationToken);

            string filesContentQuery = "What are all the relevant policies";

            var retrievedFilesContext = await _retrievalService.SearchAsync(
                filesContentQuery,
                fileContentFilter,
                cancellationToken);

            var fileContentToAudit = retrievedFilesContext.FirstOrDefault();

            if (fileContentToAudit == null)
            {
                return new ChatResponse
                {
                    LlmResponse = "I apologize, I could not find any relevant policy documents to audit.",
                    Timestamp = DateTime.UtcNow
                };
            }

            var fileAuthor = fileContentToAudit.FileAuthor;

            // Step 2: Generate response using Azure AI Foundry with retrieved content as context
            var response = await _foundryService
                .GenerateResponseAsync(
                    retrievedRulesContext,
                    fileContentToAudit,
                    cancellationToken);

            // Step 3: Return the complete chat response
            return new ChatResponse
            {
                LlmResponse = response,
                FileAuthor = fileAuthor,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Microsoft.Identity.Web.MicrosoftIdentityWebChallengeUserException ex)
        {
            _logger.LogWarning(ex, "Authentication challenge required for chat request");
            throw; // Re-throw to let the controller's AuthorizeForScopes handle this
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return new ChatResponse
            {
                LlmResponse = "I apologize, but I encountered an error while processing your request. Please try again.",
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
