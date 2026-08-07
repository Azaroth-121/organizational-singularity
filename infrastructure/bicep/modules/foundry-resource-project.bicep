@description('Microsoft Foundry account + project for governed model access, evaluations, and tracing (blueprint section 7). Defer connecting real model deployments/spend until Phase 2 (Governed AI assistance) — this module only stands up the resource and project shell.')
param accountName string
param projectName string
param location string
param tags object = {}

resource account 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: accountName
  location: location
  tags: tags
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled'
    allowProjectManagement: true
  }

  resource project 'projects@2024-10-01' = {
    name: projectName
    location: location
    tags: tags
    identity: {
      type: 'SystemAssigned'
    }
    properties: {}
  }
}

output accountId string = account.id
output projectId string = account::project.id
output accountPrincipalId string = account.identity.principalId
