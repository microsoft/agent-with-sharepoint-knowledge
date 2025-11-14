using Microsoft.Extensions.Options;
using AgentWithSPKnowledgeViaRetrieval.Models;

namespace AgentWithSPKnowledgeViaRetrieval.Services;

public interface IRuntimeSecretService
{
    /// <summary>
    /// Ensures a client secret exists for the current app registration
    /// Creates one if it doesn't exist or is about to expire
    /// </summary>
    /// <returns>The client secret value</returns>
    Task<string> EnsureClientSecretAsync();
}

public class RuntimeSecretService : IRuntimeSecretService
{
    private readonly IAppRegistrationService _appRegistrationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RuntimeSecretService> _logger;
    private readonly string _clientId;

    public RuntimeSecretService(
        IAppRegistrationService appRegistrationService,
        IConfiguration configuration,
        ILogger<RuntimeSecretService> logger)
    {
        _appRegistrationService = appRegistrationService;
        _configuration = configuration;
        _logger = logger;
        
        _clientId = _configuration["AzureAd:ClientId"] ?? throw new InvalidOperationException("AzureAd:ClientId not configured");
    }

    public async Task<string> EnsureClientSecretAsync()
    {
        try
        {
            // First check if we already have a secret in configuration
            var existingSecret = _configuration["AzureAd:ClientSecret"];
            if (!string.IsNullOrEmpty(existingSecret))
            {
                _logger.LogInformation("Using existing client secret from configuration");
                return existingSecret;
            }

            // Get the current user ID
            var userId = await _appRegistrationService.GetCurrentUserIdAsync();
            _logger.LogInformation("Creating user-specific client secret for user {UserId}", userId);

            // Clean up any existing secrets for this user
            await _appRegistrationService.CleanupUserSecretsAsync(_clientId, userId);

            // Create a new user-specific secret
            _logger.LogInformation("Creating new user-specific client secret for app registration {ClientId}", _clientId);
            var newSecret = await _appRegistrationService.CreateUserSpecificClientSecretAsync(
                _clientId, 
                userId,
                24); // 24 hours expiration

            _logger.LogInformation("Successfully created new user-specific client secret with 24-hour expiration");
            return newSecret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure client secret for app registration {ClientId}", _clientId);
            throw;
        }
    }
}