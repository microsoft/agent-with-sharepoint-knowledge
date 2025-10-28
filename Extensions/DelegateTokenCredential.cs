using Azure.Core;
using Microsoft.Identity.Web;

namespace AgentWithSPKnowledgeViaRetrieval.Services;

public class DelegateTokenCredential : TokenCredential
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly string[] _scopes;

    public DelegateTokenCredential(ITokenAcquisition tokenAcquisition, string[] scopes)
    {
        _tokenAcquisition = tokenAcquisition;
        _scopes = scopes;
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();
    }

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(_scopes);
            return new AccessToken(token, DateTimeOffset.UtcNow.AddHours(1)); // Assuming 1-hour expiry
        }
        catch (Exception)
        {
            // If getting user token fails, we could fall back to app token
            // or rethrow the exception based on requirements
            throw;
        }
    }
}