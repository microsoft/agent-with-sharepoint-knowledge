using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using AgentWithSPKnowledgeViaRetrieval.Models;
using AgentWithSPKnowledgeViaRetrieval.Services;
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
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogInformation("User not authenticated, showing sign-in interface");
            ViewBag.UserName = null;
            ViewBag.RequiresAuthentication = true;
            return View();
        }

        _logger.LogInformation("User authenticated: {User}", User.Identity?.Name);
        ViewBag.UserName = User.Identity.Name;
        ViewBag.RequiresAuthentication = false;

        // For authenticated users, we'll check consent when they actually try to use Graph API
        // For now, just show them as ready to grant consent
        ViewBag.RequiresConsent = true;
        ViewBag.ReadyForCompliance = false;
        
        return View();
    }

    [Authorize]
    [AuthorizeForScopes(ScopeKeySection = "Microsoft365:Scopes")]
    public IActionResult GrantConsent()
    {
        // This action will trigger the consent flow
        _logger.LogInformation("Graph API consent granted for user: {User}", User.Identity?.Name);
        
        // After successful consent, redirect back to index with consent granted
        TempData["ConsentGranted"] = true;
        return RedirectToAction("Index");
    }

    public IActionResult ConsentGranted()
    {
        // This action shows the interface after consent has been granted
        ViewBag.UserName = User.Identity.Name;
        ViewBag.RequiresAuthentication = false;
        ViewBag.RequiresConsent = false;
        ViewBag.ReadyForCompliance = true;
        
        return View("Index");
    }

    [Authorize]
    [AuthorizeForScopes(Scopes = new string[] { 
        "https://graph.microsoft.com/Files.Read.All", 
        "https://graph.microsoft.com/Sites.Read.All", 
        "https://graph.microsoft.com/Mail.Send", 
        "https://graph.microsoft.com/User.Read.All" 
    })]
    [HttpPost]
    public async Task<IActionResult> RunComplianceCheck(CancellationToken cancellationToken)
    {
        return await ProcessComplianceCheck(cancellationToken);
    }

    [AuthorizeForScopes(Scopes = new string[] { 
        "https://graph.microsoft.com/Files.Read.All", 
        "https://graph.microsoft.com/Sites.Read.All", 
        "https://graph.microsoft.com/Mail.Send", 
        "https://graph.microsoft.com/User.Read.All" 
    })]
    [HttpGet]
    public async Task<IActionResult> RunComplianceCheck()
    {
        // This handles the GET redirect after OAuth consent
        return await ProcessComplianceCheck(CancellationToken.None);
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
            ViewBag.UserName = User.Identity.Name;
            ViewBag.ReadyForCompliance = true;

            // Send email with results
            if (!string.IsNullOrEmpty(response.FileAuthor))
            {
                try
                {
                    var emailResult = await _mailService.SendMailAsync(
                        response.FileAuthor,
                        response.LlmResponse,
                        "compliance-check",
                        cancellationToken);
                    
                    if (emailResult.Success)
                    {
                        ViewBag.EmailSent = true;
                        ViewBag.EmailMessage = emailResult.Message;
                    }
                    else
                    {
                        ViewBag.EmailSent = false;
                        ViewBag.EmailError = emailResult.Message;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unexpected error while sending email notification");
                    ViewBag.EmailSent = false;
                    ViewBag.EmailError = "An unexpected error occurred while sending email notification";
                }
            }
            else
            {
                ViewBag.EmailSent = false;
                ViewBag.EmailError = "No file author found - unable to send email notification";
            }

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
            ViewBag.UserName = User.Identity.Name;
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