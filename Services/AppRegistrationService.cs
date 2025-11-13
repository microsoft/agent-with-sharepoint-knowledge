using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Applications.Item.AddPassword;

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

    public async Task<string> CreateClientSecretAsync(string clientId, string displayName = "Auto-generated", int expirationMonths = 24)
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
                EndDateTime = DateTimeOffset.UtcNow.AddMonths(expirationMonths)
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