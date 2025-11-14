# AI Application with SharePoint Knowledge and Actions

This project features a web application and an agent designed to help users process information from their SharePoint content and generate summary reports. The application leverages the Azure AI Foundry SDK to host and communicate with the agent, which utilizes the Copilot Retrieval API ([learn more](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/api/ai-services/retrieval/overview)) for semantic queries of relevant SharePoint content. The Retrieval API relies on SharePoint’s built-in semantic index and access control.

<br/>

<div align="center">
  
[**SOLUTION OVERVIEW**](#solution-overview) \| [**GETTING STARTED**](#getting-started) \| [**BUSINESS SCENARIO**](#business-scenario) \| [**SUPPORTING DOCUMENTATION**](#supporting-documentation)

</div>
<br/>

**Note**: With any AI solutions you create using these templates, you are responsible for assessing all associated risks, and for complying with all applicable laws and safety standards. [Learn more](https://learn.microsoft.com/en-us/azure/ai-foundry/responsible-ai/agents/transparency-note) and [See here](https://github.com/microsoft/agent-framework/blob/main/TRANSPARENCY_FAQ.md).

<h2><img src="./docs/images/readme/solution-overview.png" width="48" />
Solution overview
</h2>

This solution deploys a web-based chat application with AI capabilities running in Azure Container App.

The application leverages Azure AI Foundry projects and Azure AI services to provide intelligent policy compliance analysis. It supports retrieving content from SharePoint sites using Microsoft 365 Copilot Retrieval API and analyzes documents against compliance rules using Azure AI models. The solution includes built-in monitoring capabilities with tracing to ensure easier troubleshooting and optimized performance.

This solution creates an Azure AI Foundry project and Azure AI services. Instructions are provided for deployment through Azure Developer CLI and local development environment.

### Solution architecture

The app code runs in Azure Container Apps to process user requests for policy compliance checking. It leverages Azure AI projects and Azure AI services, including the model and Microsoft 365 Copilot Retrieval API for SharePoint content retrieval.

|![image](./docs/images/readme/agent-with-sp-knowledge-m1-solution-diagram.png)|
|---|

<br/>

### Key Features

• **Policy Compliance Analysis**: The AI chat application analyzes SharePoint documents against organizational compliance rules, providing detailed violation reports with citations.

• **SharePoint Integration**: Uses Microsoft 365 Copilot Retrieval API with proper authentication to retrieve policy documents from SharePoint sites with respect for user permissions.

• **Built-in Monitoring and Tracing**: Integrated monitoring capabilities, including structured logging with ILogger, enable tracing and logging for easier troubleshooting and performance optimization.

• **Web-based Interface**: Modern Bootstrap-based responsive web interface with authentication flows and real-time progress indicators.

Here is a screenshot showing the web application interface:

![Screenshot of the policy compliance web application showing the main interface with compliance check functionality](./docs/agent_with_sp_knowledge_output.png)

> [!WARNING]
> This template, the application code and configuration it contains, has been built to showcase Microsoft Azure specific services and tools. We strongly advise our customers not to make this code part of their production environments without implementing or enabling additional security features.

For a more comprehensive list of best practices and security recommendations for Intelligent Applications, [visit our official documentation](https://learn.microsoft.com/en-us/azure/ai-foundry/).

## Getting Started

### Quick Deploy

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/microsoft/agent-with-sharepoint-knowledge)
[![Open in Dev Containers](https://img.shields.io/static/v1?style=for-the-badge&label=Dev%20Containers&message=Open&color=blue&logo=visualstudiocode)](https://vscode.dev/redirect?url=vscode://ms-vscode-remote.remote-containers/cloneInVolume?url=https://github.com/microsoft/agent-with-sharepoint-knowledge)

You can run this repository virtually by using GitHub Codespaces or VS Code Dev Containers. Click on one of the buttons above to open this repository in one of those options.

After deployment, try asking the application to check policy compliance to test your web application.

## Local Development

For developers who want to run the application locally or customize the application:

- [Local Development Guide](./docs/LOCAL_DEVELOPMENT.md) - Set up a local development environment, customize the frontend, modify agent instructions and tools, and use evaluation to improve your code.

## Resource Clean-up

To prevent incurring unnecessary charges, it's important to clean up your Azure resources after completing your work with the application.

**When to Clean Up:**

- After you have finished testing or demonstrating the application
- If the application is no longer needed or you have transitioned to a different project or environment  
- When you have completed development and are ready to decommission the application

**Deleting Resources:** To delete all associated resources and shut down the application, execute the following command:

```bash
azd down
```

Please note that this process may take up to 20 minutes to complete.

> [!WARNING]
> Alternatively, you can delete the resource group directly from the Azure Portal to clean up resources.

## Guidance

### Costs

Pricing varies per region and usage, so it isn't possible to predict exact costs for your usage. The majority of the Azure resources used in this infrastructure are on usage-based pricing tiers. However, Azure Container Registry has a fixed cost per registry per day.

You can try the [Azure pricing calculator](https://azure.microsoft.com/en-us/pricing/calculator) for the resources:

- **Azure AI Foundry**: Free tier. [Pricing](https://azure.microsoft.com/pricing/details/ai-studio/)
- **Azure AI Services**: S0 tier, defaults to gpt-4o-mini models. Pricing is based on token count. [Pricing](https://azure.microsoft.com/pricing/details/cognitive-services/)
- **Azure Container App**: Consumption tier with 0.5 CPU, 1GiB memory/storage. Pricing is based on resource allocation, and each month allows for a certain amount of free usage. [Pricing](https://azure.microsoft.com/pricing/details/container-apps/)
- **Azure Container Registry**: Basic tier. [Pricing](https://azure.microsoft.com/pricing/details/container-registry/)
- **Log analytics**: Pay-as-you-go tier. Costs based on data ingested. [Pricing](https://azure.microsoft.com/pricing/details/monitor/)

> [!WARNING]
> To avoid unnecessary costs, remember to take down your app if it's no longer in use, either by deleting the resource group in the Portal or running `azd down`.

### Security Guidelines

This template uses [Managed Identity](https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/overview) for deployment and Microsoft Identity Web for local development.

To ensure continued best practices in your own repository, we recommend that anyone creating solutions based on our templates ensure that the [Github secret scanning](https://docs.github.com/code-security/secret-scanning/about-secret-scanning) setting is enabled.

You may want to consider additional security measures, such as:

- Enabling Microsoft Defender for Cloud to [secure your Azure resources](https://learn.microsoft.com/azure/defender-for-cloud/)
- Protecting the Azure Container Apps instance with a [firewall](https://learn.microsoft.com/azure/container-apps/waf-app-gateway) and/or [Virtual Network](https://learn.microsoft.com/azure/container-apps/networking?tabs=workload-profiles-env%2Cazure-cli)

> [!IMPORTANT]
> **Security Notice**  
> This template, the application code and configuration it contains, has been built to showcase Microsoft Azure specific services and tools. We strongly advise our customers not to make this code part of their production environments without implementing or enabling additional security features.
>
> For a more comprehensive list of best practices and security recommendations for Intelligent Applications, [visit our official documentation](https://learn.microsoft.com/en-us/azure/ai-foundry/).

### Resources

This template creates everything you need to get started with Azure AI Foundry:

| Resource | Purpose |
|----------|---------|
| **Azure AI Project** | Provides a collaborative workspace for AI development with access to models, data, and compute resources |
| **Azure AI Services** | Powers the AI agents for policy compliance analysis and intelligent document processing. Default models deployed are gpt-4o-mini, but any Azure AI models can be specified per the documentation |
| **Azure Container Apps** | Hosts and scales the web application with serverless containers |
| **Azure Container Registry** | Stores and manages container images for secure deployment |
| **Application Insights** | Optional - Provides application performance monitoring, logging, and telemetry for debugging and optimization |
| **Log Analytics Workspace** | Optional - Collects and analyzes telemetry data for monitoring and troubleshooting |

## Troubleshooting

For solutions to common deployment, container app, and app issues, see the [Troubleshooting Guide](./docs/TROUBLESHOOTING.md).
