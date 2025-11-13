# Local Development

## Prerequisites

- .NET 8.0 SDK.
- Azure AI Foundry resource with a deployed model. The model must support `chatCompletions` Inference Tasks. Visit [our learn articlesfor details on how to deploy Foundry Models](https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-models/how-to/create-model-deployments?pivots=ai-foundry-portal)
- Microsoft 365 tenant with a  SharePoint Site.
- Azure App Registration with delegated permissions.

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
      - Redirect URI: **Web** platform with the following redirect uris `https://localhost:5001/signin-oidc`, `https://localhost:5001/signout-callback-oidc`. Furthermore Enable ID tokens under Implicit grant and hybrid flows
      - Click **Register**

   2. **Create Client Secret**:
      - Go to **Certificates & secrets**
      - Click **New client secret**
      - Add description and set expiration
      - **Copy the secret value** immediately (you won't see it again)

   3. **Configure API Permissions**:
      - Go to **API permissions** → **Add a permission** → **Microsoft Graph** → **Delegated permissions**
      - Add these permissions:
        - `Files.Read.All`
        - `Sites.Read.All`
        - `Mail.Send`
        - `User.Read.All`
      - Click **Grant admin consent**

3. **Configure Application Settings**

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
          "DeployedModelEndpoint": "your-azure-ai-inference-endpoint",
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
      > You will need the endpoint to your deployed model (which is not your Azure AI Foundry Project endpoint). To get this navigate to `Models + Endpoints > name of Model` Switch the SDK to `Azure AI Inference SDK` and the code panel should have some code sample with the relevant endpoint. This endpoint will look something like `https://{projectName}.cognitiveservices.azure.com/openai/deployments/{modelName}`
      > For more details, visit [Azure AI inference endpoint learn article ](https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-models/concepts/endpoints?tabs=python#azure-ai-inference-endpoint).

5. **Prepare SharePoint Site**

The default configuration values (i.e. those without changing the prompts in `appsettings.json`) assume that you have a SharePoint site with documents. Some of these documents must include specific 
rules (for the purposes of this sample we call these policy rules) which will be run against prospective content (or policy). 

For example [Sample Rule book](./sample_docs/Sample%20Rule%20book.docx) is a Word document containing all the 
rules that a prospective policy must adhere to. In contrast [Sample Policy](./sample_docs/Sample%20Policy.docx) is a potential policy that a user might have drafted. This sample policy will be run against the established rules with 
a report of possible violations being the generated output.

Therefore after setting up your SharePoint site, upload similar documents to your site. Note that it takes some time for
the underlying index (which the Copilot Retrieval API uses) to be updated after uploading your documents. 

*Possible Extensions and Customizations*

With the guidance provided above there are several extension points one could
utilize to customize the default solution for other use cases. This could include:

- Editing `Microsoft365.FileContextQuery` in `appsettings.json` - to change the "file content" context that the rules will be run against.
- Editing `Microsoft365.RulesContext` in `appsettings.json` - to change the "rules" that the "file content" will be run against.
- Editing `AzureAIFoundry.SystemMessage` - to change how the AI model should process the 2-staged retrieval that is performed by the fileContextQuery and the ruleContextQuery. For example, if the 
"fileContextQuery" returns "candidate resume applications for a job posting" and "ruleContextQuery" returns "job requirements for said job posting", then we can edit the systemMessage accordingly.

4. **Run Locally**

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