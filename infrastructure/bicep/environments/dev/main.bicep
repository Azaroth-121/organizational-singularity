targetScope = 'subscription'

@description('Short region code used in resource names per blueprint section 3.2, e.g. eus2.')
param regionCode string = 'eus2'
param location string = 'eastus2'
param instance string = '01'

@description('Postgres Flexible Server only -- see resources.bicep for why this is separate from the shared location.')
param postgresLocation string = 'centralus'

@secure()
param postgresAdminPassword string

param budgetContactEmails array

@description('Phase 2 of the first deployment: the Container Apps reference images that must already be pushed to the registry, and a Key Vault secret that must already exist. Leave false until both are true.')
param deployApps bool = false

@description('AI Search + Foundry are scaffolded for future Prometheus/Atlas work and unused by the app today. AI Search Basic has a real idle cost; leave this false until that work actually starts.')
param deployAiFeatures bool = false

@description('Temporary bypass while this subscription refuses new roleAssignments/write calls -- see resources.bicep for detail. Flip back to false once role assignments work again.')
param useDirectCredentials bool = false

param containerRegistryAdminUsername string = ''
@secure()
param containerRegistryAdminPassword string = ''
@secure()
param postgresConnectionStringDirect string = ''

param authMicrosoftEntraIdId string = ''
param authMicrosoftEntraIdIssuer string = ''
param entraApiScope string = ''
@secure()
param authMicrosoftEntraIdSecret string = ''
@secure()
param authSecretValue string = ''

@description('A real OpenAI platform API key (platform.openai.com), scoped to this project specifically -- distinct from any other projects own key. When set, ModelGateway talks directly to https://api.openai.com/v1 instead of a deployed Foundry account (see ADR 0003, 2026-08-20 update). Leave empty to use the Foundry-derived key instead once deployAiFeatures=true.')
@secure()
param openAiApiKeyDirect string = ''
param openAiModelName string = 'gpt-5.4-mini'

var env = 'dev'
var tags = {
  application: 'organizational-singularity'
  environment: env
  owner: 'platform-engineering'
  costCenter: 'product-rnd'
  dataClassification: 'confidential'
  tenantModel: 'internal'
  managedBy: 'bicep-github-actions'
  criticality: 'low'
}

var names = {
  resourceGroup: 'rg-os-${env}-${regionCode}-${instance}'
  logAnalytics: 'law-os-platform-${env}-${regionCode}-${instance}'
  appInsights: 'appi-os-api-${env}-${regionCode}-${instance}'
  containerRegistry: 'acrosdeveus2${instance}'
  containerAppsEnv: 'cae-os-core-${env}-${regionCode}-${instance}'
  webApp: 'ca-os-web-${env}-${regionCode}-${instance}'
  apiApp: 'ca-os-api-${env}-${regionCode}-${instance}'
  postgres: 'psql-os-core-${env}-${regionCode}-${instance}'
  storage: 'stososdeveus2${instance}'
  keyVault: 'kv-os-core-${env}-${regionCode}-${instance}'
  aiSearch: 'srch-os-knowledge-${env}-${regionCode}-${instance}'
  foundryAccount: 'aif-os-${env}-${regionCode}-${instance}'
  foundryProject: 'prj-os-prometheus-${env}'
  budget: 'budget-os-${env}'
}

module resourceGroup '../../modules/resource-group-baseline.bicep' = {
  name: 'deploy-rg'
  params: {
    name: names.resourceGroup
    location: location
    tags: tags
  }
}

module budget '../../modules/budgets-alerts.bicep' = {
  name: 'deploy-budget'
  params: {
    budgetName: names.budget
    amount: 200
    contactEmails: budgetContactEmails
    resourceGroupFilter: names.resourceGroup
  }
  dependsOn: [
    resourceGroup
  ]
}

module resources 'resources.bicep' = {
  name: 'deploy-dev-resources'
  scope: az.resourceGroup(names.resourceGroup)
  params: {
    location: location
    tags: tags
    names: names
    postgresAdminPassword: postgresAdminPassword
    postgresLocation: postgresLocation
    deployApps: deployApps
    deployAiFeatures: deployAiFeatures
    useDirectCredentials: useDirectCredentials
    containerRegistryAdminUsername: containerRegistryAdminUsername
    containerRegistryAdminPassword: containerRegistryAdminPassword
    postgresConnectionStringDirect: postgresConnectionStringDirect
    authMicrosoftEntraIdId: authMicrosoftEntraIdId
    authMicrosoftEntraIdIssuer: authMicrosoftEntraIdIssuer
    entraApiScope: entraApiScope
    authMicrosoftEntraIdSecret: authMicrosoftEntraIdSecret
    authSecretValue: authSecretValue
    openAiApiKeyDirect: openAiApiKeyDirect
    openAiModelName: openAiModelName
  }
  dependsOn: [
    resourceGroup
  ]
}

output resourceGroupName string = names.resourceGroup
output containerRegistryLoginServer string = resources.outputs.containerRegistryLoginServer
output webAppFqdn string = resources.outputs.webAppFqdn
output apiAppFqdn string = resources.outputs.apiAppFqdn
