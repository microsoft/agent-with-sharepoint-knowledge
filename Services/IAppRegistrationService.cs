namespace AgentWithSPKnowledgeViaRetrieval.Services;

public interface IAppRegistrationService
{
    /// <summary>
    /// Creates a new client secret for the specified app registration
    /// </summary>
    /// <param name="clientId">The application (client) ID</param>
    /// <param name="displayName">Display name for the secret</param>
    /// <param name="expirationMonths">Expiration in months (default 24)</param>
    /// <returns>The secret value (only available at creation time)</returns>
    Task<string> CreateClientSecretAsync(string clientId, string displayName = "Auto-generated", int expirationMonths = 24);
}

