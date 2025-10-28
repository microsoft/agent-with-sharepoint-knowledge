<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# Azure AI Chat Agent with SharePoint RAG

This project implements a chat agent using Azure AI Foundry SDK that retrieves and grounds responses on SharePoint content through Microsoft 365 Copilot APIs.

## Code Generation Guidelines

1. **Azure AI Integration**: Use Azure AI SDK for .NET patterns and best practices for chat completions and content generation
2. **Microsoft Graph**: Implement proper authentication and API calls for SharePoint content retrieval using Microsoft Graph SDK
3. **RAG Implementation**: Follow retrieval-augmented generation patterns with proper chunking and embedding strategies
4. **Error Handling**: Include comprehensive error handling for Azure and Microsoft Graph API calls with try-catch blocks
5. **Configuration**: Use IConfiguration for configuration management with appsettings.json and environment variables
6. **Logging**: Implement structured logging using ILogger for debugging and monitoring
7. **Async/Await**: Use async/await patterns throughout the codebase for all I/O operations
8. **Dependency Injection**: Use .NET dependency injection container for service registration and lifetime management
9. **Type Safety**: Use strongly-typed configuration classes and models for better type safety
