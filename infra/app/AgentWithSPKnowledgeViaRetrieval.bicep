extension 'br:mcr.microsoft.com/bicep/extensions/microsoftgraph/v1.0:0.1.8-preview'

param name string
param location string = resourceGroup().location
param tags object = {}

param identityName string
param containerRegistryName string
param containerAppsEnvironmentName string
param exists bool
param resourceToken string
param playgroundUrl string
param modelName string
@secure()
param appDefinition object

var appSettingsArray = filter(array(appDefinition.settings), i => i.name != '')
var secrets = map(filter(appSettingsArray, i => i.?secret != null), i => {
  name: i.name
  value: i.value
  secretRef: i.?secretRef ?? take(replace(replace(toLower(i.name), '_', '-'), '.', '-'), 32)
})
var env = map(filter(appSettingsArray, i => i.?secret == null), i => {
  name: i.name
  value: i.value
})

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identityName
}

// Configure the Azure AD application with federated credential to trust the managed identity
resource azureAdApp 'Microsoft.Graph/applications@v1.0' = {
  displayName: 'AI App with SharePoint Knowledge'
  uniqueName: 'spe-compliance-app-${resourceToken}'
  web: {
    redirectUris: [
      'https://${appFqdn}/signin-oidc'
    ]
    logoutUrl: 'https://${appFqdn}/signout-oidc'
  }
  requiredResourceAccess: [
    {
      // Microsoft Graph
      resourceAppId: '00000003-0000-0000-c000-000000000000'
      resourceAccess: [
        {
          // Files.Read.All (delegated)
          id: 'df85f4d6-205c-4ac5-a5ea-6bf408dba283'
          type: 'Scope'
        }
        {
          // Sites.Read.All (delegated)
          id: '205e70e5-aba6-4c52-a976-6d2d46c48043'
          type: 'Scope'
        }
        {
          // Mail.Send (delegated)
          id: 'e383f46e-2787-4529-855e-0e479a3ffac0'
          type: 'Scope'
        }
        {
          // User.Read (delegated)
          id: 'e1fe6dd8-ba31-4d61-89e7-88639da4683d'
          type: 'Scope'
        }
      ]
    }
    {
      // Azure Cognitive Services
      resourceAppId: '7d312290-28c8-473c-a0ed-8e53749b6d6d'
      resourceAccess: [
        {
          // user_impersonation (delegated)
          id: '5f1e8914-a52b-429f-9324-91b92b81adaf'
          type: 'Scope'
        }
      ]
    }
  ]

  resource managedIdentityFederatedCredential 'federatedIdentityCredentials@v1.0' = {
    name: '${azureAdApp.uniqueName}/managed-identity-federation'
    description: 'Trust the container app managed identity to impersonate the Azure AD application'
    audiences: [
      'api://AzureADTokenExchange'
    ]
    issuer: '${environment().authentication.loginEndpoint}${tenant().tenantId}/v2.0'
    subject: identity.properties.principalId
  }
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: containerRegistryName
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: containerAppsEnvironmentName
}

var appFqdn = '${name}.${containerAppsEnvironment.properties.defaultDomain}'

resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: containerRegistry
  name: guid(subscription().id, resourceGroup().id, identity.id, 'acrPullRole')
  properties: {
    roleDefinitionId:  subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
    principalId: identity.properties.principalId
  }
}

module fetchLatestImage '../modules/fetch-container-image.bicep' = {
  name: '${name}-fetch-image'
  params: {
    exists: exists
    name: name
  }
}

resource app 'Microsoft.App/containerApps@2023-05-02-preview' = {
  name: name
  location: location
  tags: union(tags, {'azd-service-name':  'AgentWithSPKnowledgeViaRetrieval' })
  dependsOn: [ acrPullRole ]
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress:  {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: '${containerRegistryName}.azurecr.io'
          identity: identity.id
        }
      ]
      secrets: union([
      ],
      map(secrets, secret => {
        name: secret.secretRef
        value: secret.value
      }))
    }
    template: {
      containers: [
        {
          image: fetchLatestImage.outputs.?containers[?0].?image ?? 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          name: 'main'
          env: union([
            {
              name: 'PORT'
              value: '8080'
            }
            {
              name: 'AzureAd__Instance'
              value: environment().authentication.loginEndpoint
            }
            {
              name: 'AzureAd__TenantId'
              value: tenant().tenantId
            }
            {
              name: 'AzureAd__ClientId'
              value: azureAdApp.appId
            }
            {
              name: 'AzureAd__CallbackPath'
              value: '/signin-oidc'
            }
            {
              name: 'AzureAd__SignedOutCallbackPath'
              value: '/signout-callback-oidc'
            }
            {
              name: 'AzureAd__ClientCredentials__0__SourceType'
              value: 'SignedAssertionFromManagedIdentity'
            }
            {
              name: 'AzureAd__ClientCredentials__0__ManagedIdentityClientId'
              value: identity.properties.clientId
            }
            {
              name: 'AzureAd__ClientCredentials__0__TokenExchangeUrl'
              value: 'api://AzureADTokenExchange/.default'
            }
            {
              name: 'AzureAIFoundry__ProjectEndpoint'
              value: playgroundUrl
            }
            {
              name: 'AzureAIFoundry__ModelName'
              value: modelName
            }
            {
              name: 'Microsoft365__TenantId'
              value: tenant().tenantId
            }
            {
              name: 'Microsoft365__ClientId'
              value: azureAdApp.appId
            }
          ],
          env,
          map(secrets, secret => {
            name: secret.name
            secretRef: secret.secretRef
          }))
          resources: {
            cpu: json('1.0')
            memory: '2.0Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
      }
    }
  }
}

output defaultDomain string = containerAppsEnvironment.properties.defaultDomain
output name string = app.name
output uri string = 'https://${app.properties.configuration.ingress.fqdn}'
output id string = app.id
output appId string = azureAdApp.appId
output appUniqueName string = azureAdApp.uniqueName
