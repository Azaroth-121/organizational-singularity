@description('Deployed with scope set to the dev resource group by main.bicep.')
param location string
param tags object
param names object

@secure()
param postgresAdminPassword string

@description('Separate from the shared location: this subscription is restricted from provisioning Postgres Flexible Server in eastus2/eastus/westus2/southcentralus/westeurope (confirmed live via az postgres flexible-server list-skus, not a code issue) -- centralus and westus3 are open. Everything else stays in the shared region.')
param postgresLocation string = 'centralus'

param deployApps bool = false
param deployAiFeatures bool = false

@description('Temporary bypass while this subscription refuses new roleAssignments/write calls (MissingSubscription error, confirmed live against Reader/Key-Vault-Secrets-Officer grants). When true, container apps pull via ACR admin credentials and read the DB connection string as a plain secret instead of Key Vault -- both role-assignment-free paths. Flip back to false once role assignments work again and redeploy to restore managed identity + Key Vault.')
param useDirectCredentials bool = false

param containerRegistryAdminUsername string = ''
@secure()
param containerRegistryAdminPassword string = ''
@secure()
param postgresConnectionStringDirect string = ''

@description('Same Entra app registration already used for local dev -- client ID and issuer are not secret.')
param authMicrosoftEntraIdId string = ''
param authMicrosoftEntraIdIssuer string = ''
param entraApiScope string = ''
@secure()
param authMicrosoftEntraIdSecret string = ''
@secure()
param authSecretValue string = ''

@description('See main.bicep and ADR 0003 (2026-08-20 update). Takes priority over the Foundry-derived key when both are set.')
@secure()
param openAiApiKeyDirect string = ''
param openAiModelName string = 'gpt-5.4-mini'

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
    adminUserEnabled: useDirectCredentials
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
    location: postgresLocation
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

module aiSearch '../../modules/ai-search.bicep' = if (deployAiFeatures) {
  name: 'deploy-ai-search'
  params: {
    name: names.aiSearch
    location: location
    tags: tags
    skuName: 'basic'
  }
}

module foundry '../../modules/foundry-resource-project.bicep' = if (deployAiFeatures) {
  name: 'deploy-foundry'
  params: {
    accountName: names.foundryAccount
    projectName: names.foundryProject
    location: location
    tags: tags
  }
}

// API-key auth (see ADR 0003 -- same roleAssignments-restriction bypass as
// useDirectCredentials elsewhere in this file). Only resolved when actually deploying AI
// features; conditional 'existing' avoids referencing a resource that was never deployed.
resource foundryAccountExisting 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = if (deployAiFeatures) {
  name: names.foundryAccount
  dependsOn: [
    foundry
  ]
}

// Direct OpenAI (ADR 0003, 2026-08-20 update) takes priority when a key is supplied --
// today's actual deployment path, skipping the Foundry account entirely. Falls back to the
// Foundry-derived endpoint/deployment when deployAiFeatures is true and no direct key is set.
var useDirectOpenAi = !empty(openAiApiKeyDirect)

var aiEnvironmentVariables = useDirectOpenAi ? [
  {
    name: 'Foundry__Endpoint'
    value: 'https://api.openai.com/v1'
  }
  {
    name: 'Foundry__ApiVersion'
    value: 'v1'
  }
  {
    name: 'Foundry__ClassifyDocumentDeployment'
    value: openAiModelName
  }
  {
    name: 'Foundry__SummarizeEvidenceDeployment'
    value: openAiModelName
  }
  {
    name: 'Foundry__DraftFindingDeployment'
    value: openAiModelName
  }
  {
    name: 'Foundry__RecommendPrioritiesDeployment'
    value: openAiModelName
  }
  {
    name: 'Foundry__AnswerExecutiveQuestionDeployment'
    value: openAiModelName
  }
  {
    name: 'Foundry__GenerateReportNarrativeDeployment'
    value: openAiModelName
  }
] : (deployAiFeatures ? [
  {
    name: 'Foundry__Endpoint'
    value: foundry.outputs.accountEndpoint
  }
  {
    name: 'Foundry__ApiVersion'
    value: '2025-06-01'
  }
  {
    name: 'Foundry__ClassifyDocumentDeployment'
    value: foundry.outputs.chatDeploymentName
  }
  {
    name: 'Foundry__SummarizeEvidenceDeployment'
    value: foundry.outputs.chatDeploymentName
  }
  {
    name: 'Foundry__DraftFindingDeployment'
    value: foundry.outputs.chatDeploymentName
  }
  {
    name: 'Foundry__RecommendPrioritiesDeployment'
    value: foundry.outputs.chatDeploymentName
  }
  {
    name: 'Foundry__AnswerExecutiveQuestionDeployment'
    value: foundry.outputs.chatDeploymentName
  }
  {
    name: 'Foundry__GenerateReportNarrativeDeployment'
    value: foundry.outputs.chatDeploymentName
  }
] : [])

var aiPlainSecrets = useDirectOpenAi ? [
  {
    name: 'Foundry__ApiKey'
    value: openAiApiKeyDirect
  }
] : (deployAiFeatures ? [
  {
    name: 'Foundry__ApiKey'
    value: foundryAccountExisting.listKeys().key1
  }
] : [])

// --- Web and API container apps ---
// NOTE: image references point at a placeholder tag until the first CI build pushes a
// real digest. Update via pipeline, not by hand, once GitHub Actions/OIDC is wired up.

module webApp '../../modules/container-app.bicep' = if (deployApps) {
  name: 'deploy-web'
  params: {
    name: names.webApp
    location: location
    tags: tags
    containerAppsEnvironmentId: containerAppsEnv.outputs.environmentId
    image: '${containerRegistry.outputs.loginServer}/os-web:latest'
    targetPort: 3000
    registryLoginServer: containerRegistry.outputs.loginServer
    registryUsername: useDirectCredentials ? containerRegistryAdminUsername : ''
    registryPassword: useDirectCredentials ? containerRegistryAdminPassword : ''
    environmentVariables: [
      {
        name: 'NEXT_PUBLIC_API_BASE_URL'
        value: 'https://${names.apiApp}.${containerAppsEnv.outputs.defaultDomain}'
      }
      {
        name: 'AUTH_URL'
        value: 'https://${names.webApp}.${containerAppsEnv.outputs.defaultDomain}'
      }
      {
        name: 'AUTH_TRUST_HOST'
        value: 'true'
      }
      {
        name: 'AUTH_MICROSOFT_ENTRA_ID_ID'
        value: authMicrosoftEntraIdId
      }
      {
        name: 'AUTH_MICROSOFT_ENTRA_ID_ISSUER'
        value: authMicrosoftEntraIdIssuer
      }
      {
        name: 'ENTRA_API_SCOPE'
        value: entraApiScope
      }
    ]
    plainSecrets: [
      {
        name: 'AUTH_MICROSOFT_ENTRA_ID_SECRET'
        value: authMicrosoftEntraIdSecret
      }
      {
        name: 'AUTH_SECRET'
        value: authSecretValue
      }
    ]
  }
}

module apiApp '../../modules/container-app.bicep' = if (deployApps) {
  name: 'deploy-api'
  params: {
    name: names.apiApp
    location: location
    tags: tags
    containerAppsEnvironmentId: containerAppsEnv.outputs.environmentId
    image: '${containerRegistry.outputs.loginServer}/os-api:latest'
    targetPort: 8080
    registryLoginServer: containerRegistry.outputs.loginServer
    registryUsername: useDirectCredentials ? containerRegistryAdminUsername : ''
    registryPassword: useDirectCredentials ? containerRegistryAdminPassword : ''
    environmentVariables: concat([
      {
        // Was hardcoded to 'Development' -- that flips on ASP.NET Core's
        // IsDevelopment() gate, which publicly exposed Swagger UI on the live
        // HTTPS URL with no auth in front of it. 'Staging' is a real built-in
        // environment name with no dev-only behavior; no appsettings.Staging.json
        // exists, so it falls back to the base appsettings.json, same as
        // production would. Only appsettings.Development.json carries AzureAd
        // config, so that config is now supplied directly below instead of
        // relying on this environment name to pull in that file.
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Staging'
      }
      {
        name: 'AzureAd__Instance'
        value: 'https://login.microsoftonline.com/'
      }
      {
        // Derived from the same issuer URL already passed to the web app
        // (https://login.microsoftonline.com/<TENANT_ID>/v2.0) rather than a
        // second parameter carrying the same tenant ID.
        name: 'AzureAd__TenantId'
        value: split(authMicrosoftEntraIdIssuer, '/')[3]
      }
      {
        // The API is its own, separate Entra app registration from the web
        // app's (authMicrosoftEntraIdId is the web app's registration, used
        // for user sign-in delegation) -- its id only otherwise appears
        // embedded in entraApiScope ("api://<API_CLIENT_ID>/access_as_user"),
        // which the web app already receives to request this exact scope.
        name: 'AzureAd__ClientId'
        value: split(entraApiScope, '/')[2]
      }
      {
        name: 'AzureAd__Audience'
        value: 'api://${split(entraApiScope, '/')[2]}'
      }
    ], aiEnvironmentVariables)
    keyVaultSecretRefs: useDirectCredentials ? [] : [
      {
        name: 'OS_DATABASE_CONNECTION_STRING'
        keyVaultUrl: '${keyVault.outputs.vaultUri}secrets/database-connection-string'
      }
    ]
    plainSecrets: concat(aiPlainSecrets, useDirectCredentials ? [
      {
        name: 'OS_DATABASE_CONNECTION_STRING'
        value: postgresConnectionStringDirect
      }
    ] : [])
  }
}

// --- Access grants: managed identities to the resources they need ---
// Scoped to the resource group rather than the individual ACR/Key Vault/Storage resource:
// Bicep modules can't be referenced as scope targets, only resource symbols can, and ACR/
// Key Vault/Storage are themselves child modules here. Acceptable for a single-tenant dev
// resource group; tighten to per-resource scope before this pattern is reused for
// prod-enterprise or prod-sovereign stamps (see docs/adr).

module webAcrPull '../../modules/role-assignments.bicep' = if (deployApps && !useDirectCredentials) {
  name: 'deploy-web-acrpull'
  scope: resourceGroup()
  params: {
    principalId: webApp.outputs.principalId
    roleDefinitionId: acrPullRoleId
  }
}

module apiAcrPull '../../modules/role-assignments.bicep' = if (deployApps && !useDirectCredentials) {
  name: 'deploy-api-acrpull'
  scope: resourceGroup()
  params: {
    principalId: apiApp.outputs.principalId
    roleDefinitionId: acrPullRoleId
  }
}

module apiKeyVaultAccess '../../modules/role-assignments.bicep' = if (deployApps && !useDirectCredentials) {
  name: 'deploy-api-kv-access'
  scope: resourceGroup()
  params: {
    principalId: apiApp.outputs.principalId
    roleDefinitionId: keyVaultSecretsUserRoleId
  }
}

module apiStorageAccess '../../modules/role-assignments.bicep' = if (deployApps && !useDirectCredentials) {
  name: 'deploy-api-storage-access'
  scope: resourceGroup()
  params: {
    principalId: apiApp.outputs.principalId
    roleDefinitionId: storageBlobDataContributorRoleId
  }
}

output containerRegistryLoginServer string = containerRegistry.outputs.loginServer
output webAppFqdn string = deployApps ? webApp.outputs.fqdn : ''
output apiAppFqdn string = deployApps ? apiApp.outputs.fqdn : ''
output postgresFqdn string = postgres.outputs.fullyQualifiedDomainName
output keyVaultUri string = keyVault.outputs.vaultUri
