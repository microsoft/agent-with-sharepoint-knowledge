using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.HttpOverrides;
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

        // Add Microsoft Identity platform authentication
        builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
            .EnableTokenAcquisitionToCallDownstreamApi(new[] {
                "https://graph.microsoft.com/Files.Read.All",
                "https://graph.microsoft.com/Sites.Read.All",
                "https://graph.microsoft.com/Mail.Send",
                "https://graph.microsoft.com/User.Read.All"
            })
            .AddInMemoryTokenCaches();

        // Configure options
        builder.Services.Configure<AzureAIFoundryOptions>(builder.Configuration.GetSection("AzureAIFoundry"));
        builder.Services.Configure<Microsoft365Options>(builder.Configuration.GetSection("Microsoft365"));
        builder.Services.Configure<ChatSettingsOptions>(builder.Configuration.GetSection("ChatSettings"));

        // Register services
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
