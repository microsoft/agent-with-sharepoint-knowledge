namespace AgentWithSPKnowledgeViaRetrieval.Services;

public interface IAppRegistrationService
{
    /// <summary>
    /// Gets the current authenticated user's ID
    /// </summary>
    /// <returns>The user's GUID</returns>
    Task<string> GetCurrentUserIdAsync();

    /// <summary>
    /// Creates a new client secret for the specified app registration with user-specific naming
    /// </summary>
    /// <param name="clientId">The application (client) ID</param>
    /// <param name="userId">The user ID to include in the secret name</param>
    /// <param name="expirationHours">Expiration in hours (default 24)</param>
    /// <returns>The secret value (only available at creation time)</returns>
    Task<string> CreateUserSpecificClientSecretAsync(string clientId, string userId, int expirationHours = 24);

    /// <summary>
    /// Removes all existing client secrets that belong to the specified user
    /// </summary>
    /// <param name="clientId">The application (client) ID</param>
    /// <param name="userId">The user ID whose secrets should be removed</param>
    Task CleanupUserSecretsAsync(string clientId, string userId);

    /// <summary>
    /// Creates a new client secret for the specified app registration
    /// </summary>
    /// <param name="clientId">The application (client) ID</param>
    /// <param name="displayName">Display name for the secret</param>
    /// <param name="expirationHours">Expiration in hours (default 24)</param>
    /// <returns>The secret value (only available at creation time)</returns>
    Task<string> CreateClientSecretAsync(string clientId, string displayName = "Auto-generated", int expirationHours = 24);
}

