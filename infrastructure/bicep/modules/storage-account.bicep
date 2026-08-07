param name string
param location string
param tags object = {}

@allowed(['Standard_LRS', 'Standard_ZRS', 'Standard_GRS'])
param skuName string = 'Standard_LRS'

@description('Blob containers to create, e.g. [\'documents\', \'reports\', \'exports\', \'quarantine\'] per blueprint section 6.3.')
param containerNames array = [
  'documents'
  'reports'
  'exports'
  'quarantine'
]

param logAnalyticsWorkspaceId string

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }

  resource blobService 'blobServices@2023-05-01' = {
    name: 'default'
    properties: {
      deleteRetentionPolicy: {
        enabled: true
        days: 14
      }
      containerDeleteRetentionPolicy: {
        enabled: true
        days: 14
      }
    }

    resource containers 'containers@2023-05-01' = [for containerName in containerNames: {
      name: containerName
      properties: {
        publicAccess: 'None'
      }
    }]
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'default'
  scope: storage::blobService
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'Transaction'
        enabled: true
      }
    ]
  }
}

output storageAccountId string = storage.id
output storageAccountName string = storage.name
output primaryBlobEndpoint string = storage.properties.primaryEndpoints.blob
