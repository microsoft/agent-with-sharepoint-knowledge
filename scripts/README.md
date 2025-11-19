# Scripts

This directory contains utility scripts for managing the project.

## update-appsettings.ps1

Updates `appsettings.json` with values from the deployed Azure resources.

### Usage

After running `azd up`, run this script to automatically configure your local `appsettings.json`:

```powershell
.\scripts\update-appsettings.ps1
```

### What it does

The script:
1. Reads environment values from `azd env get-values`
2. Gets the tenant ID from Azure CLI (`az account show`)
3. Updates the following fields in `appsettings.json`:
   - `AzureAd.TenantId`
   - `AzureAd.ClientId`
   - `AzureAIFoundry.ProjectEndpoint`
   - `Microsoft365.TenantId`
   - `Microsoft365.ClientId`

### Prerequisites

- You must have run `azd up` successfully at least once
- You must be logged in with Azure CLI (`az login`)

### Example Output

```
Reading azd environment values...
Getting tenant ID from Azure CLI...
Found values:
  Tenant ID: 3e3d6c37-23ef-4207-b420-897dfffdada4
  Client ID: c9385f91-baff-451f-a912-3fd7f41cb608
  Project Endpoint: https://cog-fjwrl54y7ksee.cognitiveservices.azure.com/openai/deployments/gpt-4o

Updating appsettings.json...

Successfully updated appsettings.json
```
