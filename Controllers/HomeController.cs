using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using AgentWithSPKnowledgeViaRetrieval.Models;
using AgentWithSPKnowledgeViaRetrieval.Services;
using AgentWithSPKnowledgeViaRetrieval.Filters;
using System.Diagnostics;

namespace AgentWithSPKnowledgeViaRetrieval.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IChatService _chatService;
    private readonly IMailService _mailService;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IConfiguration _configuration;

    public HomeController(
        ILogger<HomeController> logger,
        IChatService chatService,
        IMailService mailService,
        ITokenAcquisition tokenAcquisition,
        IConfiguration configuration)
    {
        _logger = logger;
        _chatService = chatService;
        _mailService = mailService;
        _tokenAcquisition = tokenAcquisition;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        // Step 1: Check if user is authenticated
        if (!User?.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogInformation("User not authenticated, showing sign-in interface");
            ViewBag.UserName = null;
            ViewBag.RequiresAuthentication = true;
            return View();
        }

        _logger.LogInformation("User authenticated: {User}", User?.Identity?.Name);
        ViewBag.UserName = User?.Identity?.Name;
        ViewBag.RequiresAuthentication = false;

        // Skip consent step - all scopes are requested during authentication
        ViewBag.RequiresConsent = false;
        ViewBag.ReadyForCompliance = true;
        
        return View();
    }

    [Authorize]
    [AuthorizeForScopes(Scopes = new string[] { 
        "https://graph.microsoft.com/Files.Read.All", 
        "https://graph.microsoft.com/Sites.Read.All", 
        "https://graph.microsoft.com/Mail.Send", 
        "https://graph.microsoft.com/User.Read.All",
        // This will trigger another consent
        "https://cognitiveservices.azure.com/user_impersonation"
    })]
    [EnsureTokensAcquired]
    [HttpPost]
    public async Task<IActionResult> RunComplianceCheck(CancellationToken cancellationToken)
    {
        return await ProcessComplianceCheck(cancellationToken);
    }

    private async Task<IActionResult> ProcessComplianceCheck(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing compliance check for user: {User}", User?.Identity?.Name);

            var chatRequest = new ChatRequest { FileName = "compliance-check" };
            var response = await _chatService.ProcessChatAsync(chatRequest, cancellationToken);

            ViewBag.ComplianceResult = response.LlmResponse;
            ViewBag.FileAuthor = response.FileAuthor;
            ViewBag.Timestamp = response.Timestamp;
            ViewBag.UserName = User?.Identity?.Name;
            ViewBag.ReadyForCompliance = true;

            // Send email with results
            // if (!string.IsNullOrEmpty(response.FileAuthor))
            // {
            //     try
            //     {
            //         var emailResult = await _mailService.SendMailAsync(
            //             response.FileAuthor,
            //             response.LlmResponse,
            //             "compliance-check",
            //             cancellationToken);
                    
            //         if (emailResult.Success)
            //         {
            //             ViewBag.EmailSent = true;
            //             ViewBag.EmailMessage = emailResult.Message;
            //         }
            //         else
            //         {
            //             ViewBag.EmailSent = false;
            //             ViewBag.EmailError = emailResult.Message;
            //         }
            //     }
            //     catch (Exception ex)
            //     {
            //         _logger.LogWarning(ex, "Unexpected error while sending email notification");
            //         ViewBag.EmailSent = false;
            //         ViewBag.EmailError = "An unexpected error occurred while sending email notification";
            //     }
            // }
            // else
            // {
            //     ViewBag.EmailSent = false;
            //     ViewBag.EmailError = "No file author found - unable to send email notification";
            // }

            return View("Index");
        }
        catch (Microsoft.Identity.Web.MicrosoftIdentityWebChallengeUserException)
        {
            // Re-throw this exception so AuthorizeForScopes can handle it
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing compliance check");
            ViewBag.Error = "An error occurred while processing the compliance check. Please try again.";
            ViewBag.UserName = User?.Identity?.Name;
            ViewBag.ReadyForCompliance = true;
            return View("Index");
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}