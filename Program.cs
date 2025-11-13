using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.HttpOverrides;
using Azure.Identity;
using AgentWithSPKnowledgeViaRetrieval.Models;
using AgentWithSPKnowledgeViaRetrieval.Services;

namespace AgentWithSPKnowledgeViaRetrieval;

class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Build configuration
        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Add user secrets in development
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddUserSecrets<Program>();
        }

        // Handle runtime secret creation BEFORE configuring authentication
        var clientSecret = builder.Configuration["AzureAd:ClientSecret"];
        
        if (string.IsNullOrEmpty(clientSecret))
        {
            try
            {
                Console.WriteLine("No client secret found. Attempting to create one using runtime secret creation...");
                
                // Create services needed for secret creation
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var logger = loggerFactory.CreateLogger<AppRegistrationService>();
                var appRegistrationService = new AppRegistrationService(logger);
                
                var clientId = builder.Configuration["AzureAd:ClientId"];
                if (!string.IsNullOrEmpty(clientId))
                {
                    // Create the secret synchronously (since we're in startup)
                    var newSecret = appRegistrationService.CreateClientSecretAsync(
                        clientId, 
                        $"Auto-generated-{DateTimeOffset.UtcNow:yyyy-MM-dd-HH-mm}",
                        24).GetAwaiter().GetResult();
                    
                    // Add the secret to configuration as an in-memory source
                    var memoryConfig = new Dictionary<string, string?>
                    {
                        ["AzureAd:ClientSecret"] = newSecret
                    };
                    builder.Configuration.AddInMemoryCollection(memoryConfig);
                    
                    Console.WriteLine("Successfully created and configured client secret.");
                }
                else
                {
                    Console.WriteLine("Warning: AzureAd:ClientId not found in configuration. Cannot create client secret.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create client secret: {ex.Message}");
                Console.WriteLine("Application will continue but may fail during authentication.");
            }
        }

        // Register runtime secret services for ongoing management
        builder.Services.AddScoped<IAppRegistrationService, AppRegistrationService>();
        builder.Services.AddScoped<IRuntimeSecretService, RuntimeSecretService>();

        // Add Microsoft Identity platform authentication
        builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
            .EnableTokenAcquisitionToCallDownstreamApi(new[] {
                "https://graph.microsoft.com/Files.Read.All",
                "https://graph.microsoft.com/Sites.Read.All",
                "https://graph.microsoft.com/Mail.Send",
                "https://graph.microsoft.com/User.Read.All"
            })
            .AddInMemoryTokenCaches();

        // Configure options with validation
        builder.Services.Configure<AzureAIFoundryOptions>(builder.Configuration.GetSection("AzureAIFoundry"));
        
        builder.Services.Configure<Microsoft365Options>(builder.Configuration.GetSection("Microsoft365"));
        builder.Services.AddOptions<Microsoft365Options>()
            .Bind(builder.Configuration.GetSection("Microsoft365"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        builder.Services.Configure<ChatSettingsOptions>(builder.Configuration.GetSection("ChatSettings"));

        // Register services
        builder.Services.AddScoped<IMultiResourceTokenService, MultiResourceTokenService>();
        builder.Services.AddScoped<IRetrievalService, CopilotRetrievalService>();
        builder.Services.AddScoped<IFoundryService, FoundryService>();
        builder.Services.AddScoped<IChatService, ChatService>();
        builder.Services.AddScoped<IMailService, GraphMailService>();

        // Add web services - Remove global authorization requirement
        builder.Services.AddControllersWithViews()
            .AddMicrosoftIdentityUI();

        builder.Services.AddRazorPages();

        // Add logging
        builder.Services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var app = builder.Build();

        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
            RequireHeaderSymmetry = false
        };
        forwardedHeadersOptions.KnownNetworks.Clear();
        forwardedHeadersOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardedHeadersOptions);

        // Configure the HTTP request pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();

        app.Run();
    }
}
