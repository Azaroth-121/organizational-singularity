param name string
param location string
param tags object = {}

@description('Basic is sufficient for dev/internal; consider Standard/Premium for prod (geo-replication, private endpoints).')
@allowed(['Basic', 'Standard', 'Premium'])
param sku string = 'Basic'

resource registry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: false
  }
}

output registryId string = registry.id
output loginServer string = registry.properties.loginServer
