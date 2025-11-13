using Azure.AI.Inference;
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using AgentWithSPKnowledgeViaRetrieval.Models;
using System.Text;
using Azure;

namespace AgentWithSPKnowledgeViaRetrieval.Services;

public class FoundryService : IFoundryService
{
    private readonly ChatCompletionsClient _chatClient;
    private readonly AzureAIFoundryOptions _foundryOptions;
    private readonly ChatSettingsOptions _chatSettings;
    private readonly ILogger<FoundryService> _logger;

    public FoundryService(
        IOptions<AzureAIFoundryOptions> foundryOptions,
        IOptions<ChatSettingsOptions> chatSettings,
        ILogger<FoundryService> logger)
    {
        _foundryOptions = foundryOptions.Value;
        _chatSettings = chatSettings.Value;
        _logger = logger;

        var deployedModelInferenceEndpoint = new Uri(_foundryOptions.DeployedModelEndpoint);
        var credential = new AzureKeyCredential(_foundryOptions.APIKey);

        _chatClient = new ChatCompletionsClient(
            deployedModelInferenceEndpoint,
            credential,
            new AzureAIInferenceClientOptions()
        );
    }

    public async Task<string> GenerateResponseAsync(
        List<RetrievedContent> rulesContext,
        RetrievedContent fileContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var systemMessage = BuildSystemMessage();
            var userMessage = BuildUserMessage(rulesContext, fileContext);

            var requestOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatRequestSystemMessage(systemMessage),
                    new ChatRequestUserMessage(userMessage)
                },
                Model = _foundryOptions.DeployedModelName,
                MaxTokens = _chatSettings.MaxTokens,
                Temperature = _chatSettings.Temperature
            };

            var response = await _chatClient.CompleteAsync(requestOptions, cancellationToken);
            
            var assistantResponse = response.Value?.Content;
            
            return assistantResponse ?? "I apologize, but I couldn't generate a response at this time.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating response");
            return "I apologize, but an error occurred while processing your request.";
        }
    }

    private string BuildSystemMessage()
    {

        return _foundryOptions.SystemMessage;
    }
    
    private string BuildUserMessage(List<RetrievedContent> rulesContext, RetrievedContent filesContext)
    {
        var userMessageBuilder = new StringBuilder();
        userMessageBuilder.AppendLine("Rules: ");
        userMessageBuilder.AppendLine();
        
        foreach (var item in rulesContext)
        {
            userMessageBuilder.AppendLine($"Source: {item.Title} ({item.Source})");
            userMessageBuilder.AppendLine($"Rules to enforce: {item.Content}");
            if (!string.IsNullOrEmpty(item.Url))
                userMessageBuilder.AppendLine();
        }
        
        userMessageBuilder.AppendLine("File contents: ");
        userMessageBuilder.AppendLine();

        userMessageBuilder.AppendLine($"{filesContext.Content}");
        return userMessageBuilder.ToString();
    }
}
