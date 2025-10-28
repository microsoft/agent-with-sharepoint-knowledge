namespace AgentWithSPKnowledgeViaRetrieval.Models;

public class AzureAIFoundryOptions
{
    public const string SectionName = "AzureAIFoundry";
    public string ProjectEndpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string APIKey { get; set; } = string.Empty;
    public string SystemMessage { get; set; } = @"You are an compliance agent that detects policy violations in policy documents. You will be provided the relevant policy rules alongside the file contents at the end of these instructions
    Your job is to identify and classify issues with the file contents and produce a summary of all the violations.Use the given rules only as the definitive source of rules. For each violation add a citation to the relevant rule violation. The citation must include the name of the rule book which contains the rule that has been violated and a brief summary of why you think there is a violation. Do not include any other section (such as recommendations, or a total tally count for number of violations) that does not correspond to the sections above
    Instructions:
    - Complete task based on the provided context
    - Be concise and accurate
    - If asked about sources, reference the titles and URLs provided
    - If the context doesn't contain enough information, be honest about limitations";
}

public class Microsoft365Options
{
    public const string SectionName = "Microsoft365";

    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string CopilotRetrievalEndpoint { get; set; } = "https://graph.microsoft.com/beta/copilot/retrieval";
    public string FilterExpression { get; set; } = string.Empty;
    public bool UseUserAuthentication { get; set; } = true;
    public string[] Scopes { get; set; } = { "https://graph.microsoft.com/Files.Read.All", "https://graph.microsoft.com/Sites.Read.All" };

    public string FileContextQuery { get; set; } = "What are all the relevant policies";
    public string RulesContextQuery { get; set; } = "What are all the rules in rulebooks that apply to new policies";
}

public class ChatSettingsOptions
{
    public const string SectionName = "ChatSettings";
    
    public int MaxTokens { get; set; } = 1000;
    public float Temperature { get; set; } = 0.7f;
    public int TopK { get; set; } = 5;
}
