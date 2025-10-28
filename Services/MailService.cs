using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Models;
using AgentWithSPKnowledgeViaRetrieval.Models;
using Microsoft.Identity.Web;

namespace AgentWithSPKnowledgeViaRetrieval.Services;

public class GraphMailService : IMailService
{
  private readonly GraphServiceClient _graphClient;
  private readonly Microsoft365Options _microsoft365Options;
  private readonly ILogger<GraphMailService> _logger;
  private readonly ITokenAcquisition _tokenAcquisition;

  public GraphMailService(
    IOptions<Microsoft365Options> microsoft365Options,
    ILogger<GraphMailService> logger,
    ITokenAcquisition tokenAcquisition)
  {
    _microsoft365Options = microsoft365Options.Value;
    _logger = logger;
    _tokenAcquisition = tokenAcquisition;

    // Create a delegated token credential for the authenticated user
    var tokenCredential = new DelegateTokenCredential(_tokenAcquisition, _microsoft365Options.Scopes);
    _graphClient = new GraphServiceClient(tokenCredential, _microsoft365Options.Scopes);
  }

  public async Task<string?> RetrieveUsersEmail(string recipient)
  {
    try
    {
      _logger.LogInformation("Attempting to resolve email for recipient: {Recipient}", recipient);
      
      // Try multiple search strategies
      var searchQueries = new[]
      {
        $"\"displayName:{recipient}\"",
        $"\"mail:{recipient}\"", 
        $"\"userPrincipalName:{recipient}\"",
        recipient // Direct search
      };

      foreach (var searchQuery in searchQueries)
      {
        try
        {
          _logger.LogDebug("Searching with query: {SearchQuery}", searchQuery);
          
          var userResponse = await _graphClient.Users.GetAsync((requestConfiguration) =>
          {
            requestConfiguration.Headers.Add("ConsistencyLevel", new string[] { "eventual" });
            requestConfiguration.QueryParameters.Search = searchQuery;
            requestConfiguration.QueryParameters.Select = new string[] { "mail", "userPrincipalName", "displayName" };
            requestConfiguration.QueryParameters.Top = 5;
          });

          var user = userResponse?.Value?.FirstOrDefault();
          if (user != null && !string.IsNullOrEmpty(user.Mail))
          {
            _logger.LogInformation("Successfully resolved {Recipient} to email: {Email}", recipient, user.Mail);
            return user.Mail;
          }

          // If no mail, try userPrincipalName as fallback
          if (user != null && !string.IsNullOrEmpty(user.UserPrincipalName))
          {
            _logger.LogInformation("Using UserPrincipalName as email for {Recipient}: {Email}", recipient, user.UserPrincipalName);
            return user.UserPrincipalName;
          }
        }
        catch (Exception searchEx)
        {
          _logger.LogWarning(searchEx, "Search query '{SearchQuery}' failed, trying next strategy", searchQuery);
          continue;
        }
      }

      _logger.LogWarning("Could not resolve email address for recipient: {Recipient}", recipient);
      return null;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to resolve email for recipient: {Recipient}", recipient);
      return null;
    }
  }
  public async Task<(bool Success, string Message)> SendMailAsync(string recipient, string emailContent, string fileName, CancellationToken cancellationToken = default)
  {
    try
    {
      _logger.LogInformation("Attempting to send email to recipient: {Recipient} for file: {FileName}", recipient, fileName);
      
      var resolvedEmail = await RetrieveUsersEmail(recipient);
      
      if (string.IsNullOrEmpty(resolvedEmail))
      {
        var errorMessage = $"Could not resolve email address for recipient '{recipient}'. Please verify the user exists in your organization.";
        _logger.LogWarning(errorMessage);
        return (false, errorMessage);
      }

      var requestBody = new SendMailPostRequestBody
      {
        Message = new Message
        {
          Subject = $"Compliance report for file {fileName}",
          Body = new ItemBody
          {
            ContentType = BodyType.Text,
            Content = emailContent,
          },
          ToRecipients = new List<Recipient>
          {
            new Recipient
            {
              EmailAddress = new EmailAddress
              {
                Address = resolvedEmail,
                Name = recipient
              },
            },
          }
        },
        SaveToSentItems = false,
      };
      
      await _graphClient.Me.SendMail.PostAsync(requestBody, cancellationToken: cancellationToken);
      
      var successMessage = $"Email notification successfully sent to {recipient} ({resolvedEmail})";
      _logger.LogInformation(successMessage);
      return (true, successMessage);
    }
    catch (Exception exception)
    {
      var errorMessage = $"Failed to send email to {recipient}: {exception.Message}";
      _logger.LogError(exception, "Error occurred whilst sending email with findings to {Recipient}", recipient);
      return (false, errorMessage);
    }
  }
}