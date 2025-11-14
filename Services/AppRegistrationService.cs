using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Applications.Item.RemovePassword;

namespace AgentWithSPKnowledgeViaRetrieval.Services;

public class AppRegistrationService : IAppRegistrationService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<AppRegistrationService> _logger;

    public AppRegistrationService(ILogger<AppRegistrationService> logger)
    {
        _logger = logger;
        
        // Create Graph client using DefaultAzureCredential
        // This will use the same credential chain as your Key Vault setup
        var credential = new DefaultAzureCredential();
        _graphClient = new GraphServiceClient(credential);
    }

    public async Task<string> GetCurrentUserIdAsync()
    {
        try
        {
            var me = await _graphClient.Me.GetAsync();
            if (me?.Id == null)
            {
                throw new InvalidOperationException("Unable to retrieve current user ID");
            }
            
            _logger.LogInformation("Retrieved current user ID: {UserId} ({DisplayName})", me.Id, me.DisplayName);
            return me.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user ID");
            throw;
        }
    }

    public async Task<string> CreateUserSpecificClientSecretAsync(string clientId, string userId, int expirationHours = 24)
    {
        var displayName = $"{userId}-Auto-Generated-{DateTimeOffset.UtcNow:yyyy-MM-dd-HH-mm}";
        return await CreateClientSecretAsync(clientId, displayName, expirationHours);
    }

    public async Task CleanupUserSecretsAsync(string clientId, string userId)
    {
        try
        {
            _logger.LogInformation("Cleaning up existing secrets for user {UserId} in app registration {ClientId}", userId, clientId);

            // Find the application object ID using the client ID
            var objectId = await GetApplicationObjectIdAsync(clientId);
            if (string.IsNullOrEmpty(objectId))
            {
                throw new InvalidOperationException($"Application with Client ID {clientId} not found.");
            }

            var application = await _graphClient.Applications[objectId].GetAsync();
            
            if (application?.PasswordCredentials == null || !application.PasswordCredentials.Any())
            {
                _logger.LogInformation("No existing secrets found for cleanup");
                return;
            }

            // Find secrets that belong to this user (start with user ID)
            var userSecrets = application.PasswordCredentials
                .Where(pc => !string.IsNullOrEmpty(pc.DisplayName) && pc.DisplayName.StartsWith($"{userId}-Auto-Generated-"))
                .ToList();

            if (!userSecrets.Any())
            {
                _logger.LogInformation("No user-specific secrets found for cleanup");
                return;
            }

            _logger.LogInformation("Found {Count} user-specific secrets to remove", userSecrets.Count);

            // Remove each user-specific secret
            foreach (var secret in userSecrets)
            {
                if (secret.KeyId.HasValue)
                {
                    try
                    {
                        var requestBody = new RemovePasswordPostRequestBody
                        {
                            KeyId = secret.KeyId.Value
                        };

                        await _graphClient.Applications[objectId].RemovePassword.PostAsync(requestBody);
                        _logger.LogInformation("Removed secret: {DisplayName} (KeyId: {KeyId})", secret.DisplayName, secret.KeyId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to remove secret {DisplayName} (KeyId: {KeyId})", secret.DisplayName, secret.KeyId);
                    }
                }
            }

            _logger.LogInformation("Completed cleanup of user-specific secrets");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup user secrets for user {UserId} in app registration {ClientId}", userId, clientId);
            throw;
        }
    }



    private async Task<string?> GetApplicationObjectIdAsync(string clientId)
    {
        try
        {
            // Search for the application by client ID (appId in Graph API terms)
            var applications = await _graphClient.Applications.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = $"appId eq '{clientId}'";
                requestConfiguration.QueryParameters.Select = new[] { "id", "appId", "displayName" };
            });

            var app = applications?.Value?.FirstOrDefault();
            if (app != null)
            {
                _logger.LogInformation("Found application '{DisplayName}' with Object ID: {ObjectId}", app.DisplayName, app.Id);
                return app.Id;
            }

            _logger.LogWarning("Application with Client ID {ClientId} not found", clientId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find application with Client ID {ClientId}", clientId);
            throw;
        }
    }

    public async Task<string> CreateClientSecretAsync(string clientId, string displayName = "Auto-generated", int expirationHours = 24)
    {
        try
        {
            _logger.LogInformation("Creating client secret for app registration with Client ID {ClientId}", clientId);



            // Find the application object ID using the client ID
            var objectId = await GetApplicationObjectIdAsync(clientId);
            if (string.IsNullOrEmpty(objectId))
            {
                throw new InvalidOperationException($"Application with Client ID {clientId} not found. This could mean the application doesn't exist or you don't have permission to access it.");
            }

            var passwordCredential = new PasswordCredential
            {
                DisplayName = displayName,
                EndDateTime = DateTimeOffset.UtcNow.AddHours(expirationHours)
            };

            var requestBody = new AddPasswordPostRequestBody
            {
                PasswordCredential = passwordCredential
            };

            // Call Microsoft Graph to add the password credential using the object ID
            var result = await _graphClient.Applications[objectId].AddPassword.PostAsync(requestBody);

            if (result?.SecretText != null)
            {
                _logger.LogInformation("Successfully created client secret for app with Client ID {ClientId} (Object ID: {ObjectId}) with key ID {KeyId}", 
                    clientId, objectId, result.KeyId);
                return result.SecretText;
            }

            throw new InvalidOperationException("Failed to create client secret - no secret returned");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create client secret for app registration with Client ID {ClientId}", clientId);
            throw;
        }
    }


}