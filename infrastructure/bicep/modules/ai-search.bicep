@description('Azure AI Search for knowledge retrieval (blueprint section 6). Provisioned in dev per Appendix A, but defer heavy/vector-indexed ingestion until the deterministic methodology is proven (section 13.1) — this module just stands up the empty service.')
param name string
param location string
param tags object = {}

@description('Free tier only supports one service per subscription; use Basic for anything beyond a single shared dev instance.')
@allowed(['free', 'basic', 'standard'])
param skuName string = 'basic'

resource searchService 'Microsoft.Search/searchServices@2024-06-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

output searchServiceId string = searchService.id
output searchServiceEndpoint string = 'https://${name}.search.windows.net'
