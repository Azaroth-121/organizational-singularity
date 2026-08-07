targetScope = 'subscription'

@description('Resource group name, e.g. rg-os-dev-eus2-01')
param name string

param location string

@description('Mandatory tags per blueprint section 3.3')
param tags object

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: name
  location: location
  tags: tags
}

output resourceGroupName string = rg.name
output resourceGroupId string = rg.id
