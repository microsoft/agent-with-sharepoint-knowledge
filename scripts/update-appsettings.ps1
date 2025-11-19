#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Updates appsettings.json with values from azd environment.

.DESCRIPTION
    Reads environment values from 'azd env get-values' and updates the appsettings.json
    file with the Azure AD, Azure AI Foundry, and Microsoft 365 configuration values.

.EXAMPLE
    .\scripts\update-appsettings.ps1
#>

param(
    [string]$AppSettingsPath = "appsettings.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "Reading azd environment values..." -ForegroundColor Cyan

# Get azd environment values
$azdEnvOutput = azd env get-values 2>&1 | Where-Object { $_ -notmatch "^WARNING:" }
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to read azd environment values. Make sure you have run 'azd up' first."
    exit 1
}

# Parse environment values into a hashtable
$envVars = @{}
foreach ($line in $azdEnvOutput) {
    if ($line -match '^([^=]+)="?([^"]*)"?$') {
        $envVars[$matches[1]] = $matches[2]
    }
}

# Get tenant ID from Azure CLI
Write-Host "Getting tenant ID from Azure CLI..." -ForegroundColor Cyan
$tenantId = az account show --query tenantId -o tsv 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to get tenant ID from Azure CLI. Make sure you are logged in with 'az login'."
    exit 1
}

# Required values
$clientId = $envVars["AZURE_CLIENT_ID"]
$projectEndpoint = $envVars["AZURE_OPENAI_PLAYGROUND_URL"]

if (-not $clientId) {
    Write-Error "AZURE_CLIENT_ID not found in azd environment. Make sure 'azd up' has completed successfully."
    exit 1
}

if (-not $projectEndpoint) {
    Write-Error "AZURE_OPENAI_PLAYGROUND_URL not found in azd environment. Make sure 'azd up' has completed successfully."
    exit 1
}

Write-Host "Found values:" -ForegroundColor Green
Write-Host "  Tenant ID: $tenantId" -ForegroundColor Gray
Write-Host "  Client ID: $clientId" -ForegroundColor Gray
Write-Host "  Project Endpoint: $projectEndpoint" -ForegroundColor Gray

# Read appsettings.json
if (-not (Test-Path $AppSettingsPath)) {
    Write-Error "appsettings.json not found at path: $AppSettingsPath"
    exit 1
}

Write-Host "`nUpdating $AppSettingsPath..." -ForegroundColor Cyan

$appSettings = Get-Content $AppSettingsPath -Raw | ConvertFrom-Json

# Update AzureAd section
if (-not $appSettings.AzureAd) {
    $appSettings | Add-Member -MemberType NoteProperty -Name "AzureAd" -Value ([PSCustomObject]@{})
}
$appSettings.AzureAd.TenantId = $tenantId
$appSettings.AzureAd.ClientId = $clientId

# Update AzureAIFoundry section
if (-not $appSettings.AzureAIFoundry) {
    $appSettings | Add-Member -MemberType NoteProperty -Name "AzureAIFoundry" -Value ([PSCustomObject]@{})
}
$appSettings.AzureAIFoundry.ProjectEndpoint = $projectEndpoint

# Update Microsoft365 section
if (-not $appSettings.Microsoft365) {
    $appSettings | Add-Member -MemberType NoteProperty -Name "Microsoft365" -Value ([PSCustomObject]@{})
}
$appSettings.Microsoft365.TenantId = $tenantId
$appSettings.Microsoft365.ClientId = $clientId

# Write updated appsettings.json with proper formatting
$appSettings | ConvertTo-Json -Depth 10 | Set-Content $AppSettingsPath -Encoding UTF8

Write-Host "`nSuccessfully updated $AppSettingsPath" -ForegroundColor Green
Write-Host "`nUpdated values:" -ForegroundColor Green
Write-Host "  AzureAd.TenantId: $tenantId" -ForegroundColor Gray
Write-Host "  AzureAd.ClientId: $clientId" -ForegroundColor Gray
Write-Host "  AzureAIFoundry.ProjectEndpoint: $projectEndpoint" -ForegroundColor Gray
Write-Host "  Microsoft365.TenantId: $tenantId" -ForegroundColor Gray
Write-Host "  Microsoft365.ClientId: $clientId" -ForegroundColor Gray
