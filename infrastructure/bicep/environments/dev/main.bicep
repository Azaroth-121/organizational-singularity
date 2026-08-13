targetScope = 'subscription'

@description('Short region code used in resource names per blueprint section 3.2, e.g. eus2.')
param regionCode string = 'eus2'
param location string = 'eastus2'
param instance string = '01'

@secure()
param postgresAdminPassword string

param budgetContactEmails array

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
  }
  dependsOn: [
    resourceGroup
  ]
}

output resourceGroupName string = names.resourceGroup
output containerRegistryLoginServer string = resources.outputs.containerRegistryLoginServer
output webAppFqdn string = resources.outputs.webAppFqdn
output apiAppFqdn string = resources.outputs.apiAppFqdn
