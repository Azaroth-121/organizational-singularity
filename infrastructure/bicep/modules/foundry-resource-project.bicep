@description('Microsoft Foundry account + project for governed model access (blueprint section 7), plus one chat model deployment for ModelGateway (see ADR 0003). API version 2024-10-01 previously used here predates full Bicep type support for allowProjectManagement/accounts-projects (BCP037/BCP081) -- 2025-06-01 is current and fully typed, confirmed against Microsoft Learn.')
param accountName string
param projectName string
param location string
param tags object = {}

@description('Authentication for this deployment is by API key (see ADR 0003) rather than the blueprint intended managed identity, because this subscription refuses new roleAssignments/write calls -- disableLocalAuth must stay false for the key to work. Revert alongside useDirectCredentials once role assignments work again.')
param disableLocalAuth bool = false

@description('The chat model to deploy for ModelGateway. Confirm actual availability in this region/subscription at deploy time via `az cognitiveservices account list-models` rather than trusting this default blindly.')
param chatModelName string = 'gpt-5-mini'
param chatModelFormat string = 'OpenAI'
param chatModelVersion string = '2026-01-01'
param chatModelCapacity int = 10

resource account 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
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
    disableLocalAuth: disableLocalAuth
  }

  resource project 'projects@2025-06-01' = {
    name: projectName
    location: location
    tags: tags
    identity: {
      type: 'SystemAssigned'
    }
    properties: {}
  }

  resource chatDeployment 'deployments@2025-06-01' = {
    name: chatModelName
    sku: {
      name: 'Standard'
      capacity: chatModelCapacity
    }
    properties: {
      model: {
        format: chatModelFormat
        name: chatModelName
        version: chatModelVersion
      }
    }
  }
}

output accountId string = account.id
output projectId string = account::project.id
output accountPrincipalId string = account.identity.principalId
output accountEndpoint string = 'https://${accountName}.openai.azure.com/openai/v1/'
output chatDeploymentName string = account::chatDeployment.name
