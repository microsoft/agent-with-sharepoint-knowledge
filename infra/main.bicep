targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

param AgentWithSPKnowledgeViaRetrievalExists bool
@secure()
param AgentWithSPKnowledgeViaRetrievalDefinition object

@description('Id of the user or app to assign application roles')
param principalId string

@description('Timestamp for deployment to ensure unique resource names')
param deploymentTimestamp string = utcNow('yyyyMMddHHmmss')

// Tags that should be applied to all resources.
// 
// Note that 'azd-service-name' tags should be applied separately to service host resources.
// Example usage:
//   tags: union(tags, { 'azd-service-name': <service name in azure.yaml> })
var tags = {
  'azd-env-name': environmentName
}

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module identity './shared/identity.bicep' = {
  name: 'identity'
  params: {
    name: '${abbrs.managedIdentityUserAssignedIdentities}agentwith-${resourceToken}'
    location: location
    tags: tags
  }
  scope: rg
}

module registry './shared/registry.bicep' = {
  name: 'registry'
  params: {
    location: location
    tags: tags
    name: '${abbrs.containerRegistryRegistries}${resourceToken}'
  }
  scope: rg
}


module appsEnv './shared/apps-env.bicep' = {
  name: 'apps-env'
  params: {
    name: '${abbrs.appManagedEnvironments}${resourceToken}'
    location: location
    tags: tags
  }
  scope: rg
}

module foundry './shared/foundry.bicep' = {
  name: 'foundry'
  params: {
    name: '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    location: location
    tags: tags
    identityId: identity.outputs.id
    identityPrincipalId: identity.outputs.principalId
  }
  scope: rg
}

module AgentWithSPKnowledgeViaRetrieval './app/AgentWithSPKnowledgeViaRetrieval.bicep' = {
  name: 'AgentWithSPKnowledgeViaRetrieval'
  params: {
    name: '${abbrs.appContainerApps}agentwith-${resourceToken}'
    location: location
    tags: tags
    identityName: identity.outputs.name
    containerAppsEnvironmentName: appsEnv.outputs.name
    containerRegistryName: registry.outputs.name
    exists: AgentWithSPKnowledgeViaRetrievalExists
    appDefinition: AgentWithSPKnowledgeViaRetrievalDefinition
    resourceToken: resourceToken
    playgroundUrl: foundry.outputs.playgroundUrl
    modelName: foundry.outputs.modelName
    deploymentTimestamp: deploymentTimestamp
  }
  scope: rg
}

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.outputs.loginServer
output MANAGED_IDENTITY_CLIENT_ID string = identity.outputs.clientId
output MANAGED_IDENTITY_PRINCIPAL_ID string = identity.outputs.principalId
output AZURE_CLIENT_ID string = AgentWithSPKnowledgeViaRetrieval.outputs.appId
output AZURE_APP_UNIQUE_NAME string = AgentWithSPKnowledgeViaRetrieval.outputs.appUniqueName
output AZURE_OPENAI_ENDPOINT string = foundry.outputs.endpoint
output AZURE_OPENAI_PLAYGROUND_URL string = foundry.outputs.playgroundUrl

// Output the principal ID parameter for reference (used by azd)
output DEPLOYMENT_PRINCIPAL_ID string = principalId
