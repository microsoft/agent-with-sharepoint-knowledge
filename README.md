# Agent with SharePoint Knowledge - Web Application

This project implements a **web application** chat agent using Azure AI Foundry SDK that retrieves and grounds responses on SharePoint content through Microsoft 365 Copilot Retrieval API. The application provides a web interface for compliance checking and document analysis.

## Features

- **Web Interface**: ASP.NET Core MVC web application with authentication
- **Azure AI Foundry Integration**: Uses Azure AI SDK for chat completions with configurable models
- **SharePoint Content Retrieval**: Leverages Microsoft 365 Copilot Retrieval API for content grounding
- **Microsoft Identity Integration**: OAuth/OpenID Connect authentication with Microsoft 365
- **Compliance Checking**: Automated policy compliance analysis and violation reporting
- **Email Notifications**: Send compliance findings to document authors

## Prerequisites

- .NET 8.0 SDK
- Azure AI Foundry resource
- Microsoft 365 tenant with SharePoint
- Azure App Registration with delegated permissions

## Getting Started

### 1. Clone the Repository

```bash
git clone <your-repo-url>
cd AgentWithSPKnowledgeViaRetrieval
```

### 2. Configure Azure App Registration

1. **Create an Azure App Registration**:
   - Go to [Azure Portal](https://portal.azure.com)
   - Navigate to **Azure Active Directory** → **App registrations**
   - Click **New registration**
   - Name: your app name
   - Supported account types: **Accounts in this organizational directory only (Single tenant)**
   - Redirect URI: **Web** platform with `https://localhost:5001/signin-oidc`
   - Click **Register**

2. **Configure Authentication**:
   - Go to **Authentication** in the left menu
   - Under **Platform configurations**:
     - Ensure **Web** platform is configured with redirect URI: `https://localhost:5001/signin-oidc`
     - Add logout URL: `https://localhost:5001/signout-callback-oidc`
     - Enable **ID tokens** under Implicit grant and hybrid flows
   - **Do NOT** enable "Allow public client flows" for web applications

3. **Create Client Secret**:
   - Go to **Certificates & secrets**
   - Click **New client secret**
   - Add description and set expiration
   - **Copy the secret value** immediately (you won't see it again)

4. **Configure API Permissions**:
   - Go to **API permissions** → **Add a permission** → **Microsoft Graph** → **Delegated permissions**
   - Add these permissions:
     - `Files.Read.All`
     - `Sites.Read.All`
     - `Mail.Send`
     - `User.Read.All`
   - Click **Grant admin consent**

### 3. Configure Application Settings

1. Copy the example configuration:

   ```bash
   cp appsettings.example.json appsettings.json
   ```

 **NB** You will need your Azure AI inference endpoint (which is not your Azure AI Foundry Project endpoint). To get this navigate to `Models + Endpoints > name of Model` Switch the SDK to `Azure AI Inference SDK` and the code panel should have
 some code sample with the relevant endpoint. This endpoint will look something like `https://{projectName}.cognitiveservices.azure.com/openai/deployments/{modelName}`

2. Update `appsettings.json` with your values:

   ```json
   {
     "AzureAd": {
       "Instance": "https://login.microsoftonline.com/",
       "TenantId": "your-tenant-id",
       "ClientId": "your-client-id",
       "ClientSecret": "your-client-secret",
       "CallbackPath": "/signin-oidc",
       "SignedOutCallbackPath": "/signout-callback-oidc"
     },
     "AzureAIFoundry": {
       "ProjectEndpoint": "your-azure-ai-inference-endpoint",
       "ModelName": "your model name",
       "APIKey": "your-api-key"
     },
     "Microsoft365": {
       "TenantId": "your-tenant-id",
       "ClientId": "your-client-id",
       "FilterExpression": "path:\"https://your-sharepoint-site.sharepoint.com\""
     }
   }
   ```

### 4. Run Locally

```bash
# Install dependencies
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run
```

The application will be available at:

- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

## Usage

1. **First Run**: The application will open a browser for Microsoft 365 authentication
2. **Subsequent Runs**: Tokens are cached, no re-authentication needed
3. **Compliance Checking**: Use the web interface to run compliance checks
4. **View Results**: Responses include source document citations and violation reports


### 4. Running using azd
```bash
# Initialize environment
azd init

# Provision and deploy
azd up
```
1. **First Run**: After running the azd commands above you will be presented with
the url to the deployed container app. Add the following redirect URIs on the same app registration as the one mentioned above

- `https://{your-container-app-url}/signin-oidc`
- `https://{your-container-app-url}/signout-callback-oidc`


## Architecture

### Overview
The application is structured around the following components:

#### 1. Retrieval Layer
- **Purpose**: Fetches content from SharePoint using Microsoft Graph API.
- **Key Component**: `CopilotRetrievalService.cs`
  - Retrieves SharePoint content.
  - Implements chunking and embedding strategies for retrieval-augmented generation (RAG).
  - Handles authentication and API calls using Microsoft Graph SDK.

#### 2. Synthesis Layer
- **Purpose**: Generates responses using Azure AI Foundry SDK.
- **Key Component**: `FoundryService.cs`
  - Synthesizes responses based on retrieved content.
  - Implements chat completions and content generation patterns.
  - Uses Azure AI SDK for .NET.

#### 3. Orchestration Layer
- **Purpose**: Coordinates retrieval and synthesis processes.
- **Key Component**: `ChatService.cs`
  - Sequentially orchestrates retrieval and synthesis.
  - Implements async/await patterns for I/O operations.
  - Handles error management and logging.

#### 4. Presentation Layer
- **Purpose**: Displays synthesized responses and sources to the user.
- **Key Component**: `Program.cs`
  - Manages user input and output.
  - Displays top sources and synthesized responses.

#### 5. Configuration and Logging
- **Purpose**: Manages application settings and logs.
- **Key Components**:
  - `appsettings.json`: Stores configuration settings.
  - `ILogger`: Implements structured logging for debugging and monitoring.

#### 6. Authentication
- **Purpose**: Ensures secure access to APIs.
- **Key Component**: Token caching with `InteractiveBrowserCredential`.

### Architecture Diagram

```
+---------------------+
|   Presentation      |
|      Layer          |
|   (Program.cs)      |
+---------------------+
          |
          v
+---------------------+
|   Orchestration     |
|      Layer          |
|   (ChatService.cs)  |
+---------------------+
          |
          v
+---------------------+       +---------------------+
|   Retrieval Layer   |       |   Synthesis Layer   |
| (CopilotRetrieval   |       |   (FoundryService)  |
|    Service.cs)      |       |                     |
+---------------------+       +---------------------+
          |                           |
          v                           v
+---------------------+       +---------------------+
| Microsoft Graph API |       | Azure AI Foundry    |
|   (SharePoint)      |       |   SDK              |
+---------------------+       +---------------------+
```

## Security

- **No Secrets in Code**: All sensitive configuration in `appsettings.json` (git-ignored)
- **Delegated Permissions**: Respects user's SharePoint access rights
- **Token Security**: Uses Azure Identity SDK for secure token handling

## Guidance

Besides the tips below, you can find architectural diagrams in [here](./ARCHITECTURE.md).


## Troubleshooting

### Authentication Issues

#### Error: `AADSTS9002327` - "Tokens issued for the 'Single-Page Application' client-type..."
**Cause**: App registration is configured as SPA instead of Public Client  
**Solution**: 
1. Go to Azure Portal → App registrations → Your app → Authentication
2. Remove all **Single-page application** platforms
3. Keep only **Mobile and desktop applications** with `http://localhost` redirect URI
4. Ensure **Allow public client flows** is **Enabled**

#### Error: `AADSTS7000218` - "The request body must contain the following parameter: 'client_assertion' or 'client_secret'"
**Cause**: App registration is configured as Confidential Client instead of Public Client  
**Solution**:
1. Go to Azure Portal → App registrations → Your app → Authentication
2. Set **Allow public client flows** to **Yes**
3. Use **Mobile and desktop applications** platform (not Web or SPA)

#### General Authentication Troubleshooting
- Verify app registration has "Allow public client flows" enabled
- Ensure delegated permissions are granted with admin consent
- Check that redirect URI `http://localhost` is configured
- Remove any SPA or Web platform configurations that might conflict

### SharePoint Access
- Verify the user has access to the SharePoint content
- Check the `FilterExpression` path is correct
- Ensure `Sites.Read.All` and `Files.ReadWrite.All` permissions are granted

### Azure AI Foundry
**NB** You will need your Azure AI inference endpoint (which is not your Azure AI Foundry Project endpoint). To get this navigate to `Models + Endpoints > name of Model` Switch the SDK to `Azure AI Inference SDK` and the code panel should have
 some code sample with the relevant endpoint. This endpoint will look something like `https://{projectName}.cognitiveservices.azure.com/openai/deployments/{modelName}`
- Ensure you have the right endpoint url (see above)
- Ensure the model name matches your deployment
- Check Azure AI Foundry resource permissions

## Quick Fix Scripts

For convenience, this repository includes automation scripts to fix common Azure AD app registration issues:

### Bash Script (macOS/Linux)
```bash
./fix-azure-app-registration.sh
```

### PowerShell Script (Windows/Cross-platform)
```powershell
./fix-azure-app-registration.ps1
```

These scripts will automatically:
- Remove SPA platform configurations
- Add Mobile/Desktop platform with correct redirect URI
- Enable public client flows
- Display current configuration for verification

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure `appsettings.json` is not committed
5. Submit a pull request

## License

This project is licensed under the MIT License.

### Resources

- [Revolutionize your Enterprise Data with ChatGPT: Next-gen Apps w/ Azure OpenAI and Azure AI Search](https://aka.ms/entgptsearchblog)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/cognitive-services/openai/overview)
- [Azure AI Inference Client Library](https://learn.microsoft.com/en-us/python/api/overview/azure/ai-inference-readme?view=azure-python-preview)