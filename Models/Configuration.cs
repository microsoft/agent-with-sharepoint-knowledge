using System.ComponentModel.DataAnnotations;

namespace AgentWithSPKnowledgeViaRetrieval.Models;

public class AzureAIFoundryOptions
{
    public const string SectionName = "AzureAIFoundry";
    public string DeployedModelEndpoint { get; set; } = string.Empty;
    public string DeployedModelName { get; set; } = string.Empty;
    public string APIKey { get; set; } = string.Empty;
    public string SystemMessage { get; set; } = @"You are an compliance agent that detects policy violations in policy documents.
    You will be provided the relevant policy rules alongside the file contents at the end of these instructions.
    Your job is to identify and classify issues with the file contents and produce a summary of all the violations.
    Use the given rules only as the definitive source of rules. For each violation add a citation to the relevant rule violation. 
    The citation must include the name of the rule book which contains the rule that has been violated and a brief summary of why you think there is a violation.
    Do not include any other section (such as recommendations, or a total tally count for number of violations) that does not correspond to the sections above
    Instructions:
    - Complete task based on the provided context
    - Be concise and accurate
    - If asked about sources, reference the titles and URLs provided
    - If the context doesn't contain enough information, be honest about limitations";
}

public class Microsoft365Options
{
    public const string SectionName = "Microsoft365";
    public string CopilotRetrievalEndpoint { get; set; } = "https://graph.microsoft.com/beta/copilot/retrieval";
    public string FilterExpression { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = { 
        "https://graph.microsoft.com/Files.Read.All",
        "https://graph.microsoft.com/Sites.Read.All",
        "https://graph.microsoft.com/Mail.Send",
        "https://graph.microsoft.com/User.Read.All"
    };

    [Required(ErrorMessage = "FileContextQuery is required and cannot be empty")]
    [MinLength(1, ErrorMessage = "FileContextQuery cannot be empty")]
    public string FileContextQuery { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "RulesContextQuery is required and cannot be empty")]
    [MinLength(1, ErrorMessage = "RulesContextQuery cannot be empty")]
    public string RulesContextQuery { get; set; } = string.Empty;
}

public class ChatSettingsOptions
{
    public const string SectionName = "ChatSettings";
    
    public int MaxTokens { get; set; } = 1000;
    public float Temperature { get; set; } = 0.7f;
    public int TopK { get; set; } = 5;
}
