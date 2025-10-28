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

        var endpoint = new Uri(_foundryOptions.ProjectEndpoint);
        var credential = new AzureKeyCredential(_foundryOptions.APIKey);

        _chatClient = new ChatCompletionsClient(
            endpoint,
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
                Model = _foundryOptions.ModelName,
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
        var systemMessageBuilder = new StringBuilder();

        systemMessageBuilder
            .AppendLine("You are an compliance agent that detects policy violations in policy documents. You will be provided the relevant policy rules alongside the file contents at the end of these instructions."
            + "Your job is to identify and classify issues with the file contents and produce a summary of all the violations."
            + "Use the given rules only as the definitive source of rules. For each violation add a citation to the relevant rule violation. The citation must include the name of the rule book which contains the rule that has been violated"
            + "and a brief summary of why you think there is a violation. Do not include any other section (such as recommendations, or a total tally count for number of violations) that does not correspond to the sections above");

        systemMessageBuilder.AppendLine();
        systemMessageBuilder.AppendLine("Instructions:");
        systemMessageBuilder.AppendLine("- Complete task based on the provided context");
        systemMessageBuilder.AppendLine("- Be concise and accurate");
        systemMessageBuilder.AppendLine("- If asked about sources, reference the titles and URLs provided");
        systemMessageBuilder.AppendLine("- If the context doesn't contain enough information, be honest about limitations");

        return systemMessageBuilder.ToString();
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
