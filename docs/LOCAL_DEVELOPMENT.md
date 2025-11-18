# Local Development

## Prerequisites

- .NET 8.0 SDK
- Azure AI Foundry resource  
- Microsoft 365 tenant with SharePoint
- Azure App Registration with delegated permissions

## Setup Instructions

1. **Clone the Repository**

   ```bash
   git clone https://github.com/microsoft/agent-with-sharepoint-knowledge
   cd agent-with-sharepoint-knowledge
   ```

2. **Configure Azure App Registration**

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

3. **Upload sample files to your SharePoint site**
   1. Create a SharePoint site if you don't already have one. Keep the site URL handy to paste in the application settings in the next step.

   2. Upload the files from the /Sample data folder to your SharePoint site.

4. **Configure Application Settings**

   1. Copy the example configuration:

      ```bash
      cp appsettings.example.json appsettings.json
      ```

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
          "ModelName": "your-model-name",
          "APIKey": "your-api-key"
        },
        "Microsoft365": {
          "TenantId": "your-tenant-id",
          "ClientId": "your-client-id",
          "FilterExpression": "path:\"https://your-sharepoint-site.sharepoint.com\""
        }
      }
      ```

      > [!NOTE]
      > You will need your Azure AI inference endpoint (which is not your Azure AI Foundry Project endpoint). To get this navigate to `Models + Endpoints > name of Model` Switch the SDK to `Azure AI Inference SDK` and the code panel should have some code sample with the relevant endpoint. This endpoint will look something like `https://{projectName}.cognitiveservices.azure.com/openai/deployments/{modelName}`


5. **Run Locally**

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

## Azure Developer CLI Deployment

```bash
# Initialize environment
azd init

# Provision and deploy
azd up
```

After running the azd commands above you will be presented with the URL to the deployed container app. Add the following redirect URIs to the same app registration mentioned above:

- `https://{your-container-app-url}/signin-oidc`
- `https://{your-container-app-url}/signout-callback-oidc`