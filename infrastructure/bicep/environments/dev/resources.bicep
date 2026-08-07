@description('Deployed with scope set to the dev resource group by main.bicep.')
param location string
param tags object
param names object

@secure()
param postgresAdminPassword string

var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

module logAnalytics '../../modules/log-analytics.bicep' = {
  name: 'deploy-log-analytics'
  params: {
    name: names.logAnalytics
    location: location
    tags: tags
  }
}

module appInsights '../../modules/application-insights.bicep' = {
  name: 'deploy-app-insights'
  params: {
    name: names.appInsights
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

module containerRegistry '../../modules/container-registry.bicep' = {
  name: 'deploy-acr'
  params: {
    name: names.containerRegistry
    location: location
    tags: tags
    sku: 'Basic'
  }
}

resource lawExisting 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: names.logAnalytics
  dependsOn: [
    logAnalytics
  ]
}

module containerAppsEnv '../../modules/container-apps-environment.bicep' = {
  name: 'deploy-cae'
  params: {
    name: names.containerAppsEnv
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    logAnalyticsCustomerId: logAnalytics.outputs.customerId
    logAnalyticsSharedKey: lawExisting.listKeys().primarySharedKey
  }
}

module postgres '../../modules/postgresql-flexible.bicep' = {
  name: 'deploy-postgres'
  params: {
    name: names.postgres
    location: location
    tags: tags
    administratorLogin: 'os_admin'
    administratorPassword: postgresAdminPassword
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

module storage '../../modules/storage-account.bicep' = {
  name: 'deploy-storage'
  params: {
    name: names.storage
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

module keyVault '../../modules/key-vault.bicep' = {
  name: 'deploy-key-vault'
  params: {
    name: names.keyVault
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

module aiSearch '../../modules/ai-search.bicep' = {
  name: 'deploy-ai-search'
  params: {
    name: names.aiSearch
    location: location
    tags: tags
    skuName: 'basic'
  }
}

module foundry '../../modules/foundry-resource-project.bicep' = {
  name: 'deploy-foundry'
  params: {
    accountName: names.foundryAccount
    projectName: names.foundryProject
    location: location
    tags: tags
  }
}

// --- Web and API container apps ---
// NOTE: image references point at a placeholder tag until the first CI build pushes a
// real digest. Update via pipeline, not by hand, once GitHub Actions/OIDC is wired up.

module webApp '../../modules/container-app.bicep' = {
  name: 'deploy-web'
  params: {
    name: names.webApp
    location: location
    tags: tags
    containerAppsEnvironmentId: containerAppsEnv.outputs.environmentId
    image: '${containerRegistry.outputs.loginServer}/os-web:latest'
    targetPort: 3000
    registryLoginServer: containerRegistry.outputs.loginServer
    environmentVariables: [
      {
        name: 'NEXT_PUBLIC_API_BASE_URL'
        value: 'https://${names.apiApp}.${containerAppsEnv.outputs.defaultDomain}'
      }
    ]
  }
}

module apiApp '../../modules/container-app.bicep' = {
  name: 'deploy-api'
  params: {
    name: names.apiApp
    location: location
    tags: tags
    containerAppsEnvironmentId: containerAppsEnv.outputs.environmentId
    image: '${containerRegistry.outputs.loginServer}/os-api:latest'
    targetPort: 8080
    registryLoginServer: containerRegistry.outputs.loginServer
    environmentVariables: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Development'
      }
    ]
    keyVaultSecretRefs: [
      {
        name: 'OS_DATABASE_CONNECTION_STRING'
        keyVaultUrl: '${keyVault.outputs.vaultUri}secrets/database-connection-string'
      }
    ]
  }
}

// --- Access grants: managed identities to the resources they need ---
// Scoped to the resource group rather than the individual ACR/Key Vault/Storage resource:
// Bicep modules can't be referenced as scope targets, only resource symbols can, and ACR/
// Key Vault/Storage are themselves child modules here. Acceptable for a single-tenant dev
// resource group; tighten to per-resource scope before this pattern is reused for
// prod-enterprise or prod-sovereign stamps (see docs/adr).

module webAcrPull '../../modules/role-assignments.bicep' = {
  name: 'deploy-web-acrpull'
  scope: resourceGroup()
  params: {
    principalId: webApp.outputs.principalId
    roleDefinitionId: acrPullRoleId
  }
}

module apiAcrPull '../../modules/role-assignments.bicep' = {
  name: 'deploy-api-acrpull'
  scope: resourceGroup()
  params: {
    principalId: apiApp.outputs.principalId
    roleDefinitionId: acrPullRoleId
  }
}

module apiKeyVaultAccess '../../modules/role-assignments.bicep' = {
  name: 'deploy-api-kv-access'
  scope: resourceGroup()
  params: {
    principalId: apiApp.outputs.principalId
    roleDefinitionId: keyVaultSecretsUserRoleId
  }
}

module apiStorageAccess '../../modules/role-assignments.bicep' = {
  name: 'deploy-api-storage-access'
  scope: resourceGroup()
  params: {
    principalId: apiApp.outputs.principalId
    roleDefinitionId: storageBlobDataContributorRoleId
  }
}

output containerRegistryLoginServer string = containerRegistry.outputs.loginServer
output webAppFqdn string = webApp.outputs.fqdn
output apiAppFqdn string = apiApp.outputs.fqdn
output postgresFqdn string = postgres.outputs.fullyQualifiedDomainName
output keyVaultUri string = keyVault.outputs.vaultUri
