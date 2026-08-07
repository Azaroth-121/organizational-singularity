@description('Generic role assignment module. The caller controls scope by setting the module\'s `scope:` property to the target resource (e.g. `scope: keyVault`) when invoking it — this module itself just creates the assignment at whatever scope it is deployed into.')
param principalId string

@allowed(['ServicePrincipal', 'User', 'Group'])
param principalType string = 'ServicePrincipal'

@description('Built-in role definition GUID, e.g. AcrPull = 7f951dda-4ed3-4680-a7ca-43fe172d538d, Key Vault Secrets User = 4633458b-17de-408a-b874-0445c86b69e6, Storage Blob Data Contributor = ba92f5b4-2d11-453d-a403-e96b0029c9fe.')
param roleDefinitionId string

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, principalId, roleDefinitionId)
  properties: {
    principalId: principalId
    principalType: principalType
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
  }
}
