param name string
param location string
param tags object = {}

param administratorLogin string

@secure()
param administratorPassword string

@description('Burstable is the low-cost MVP tier per blueprint section 13.1.')
param skuName string = 'Standard_B1ms'
param skuTier string = 'Burstable'

param storageSizeGB int = 32
param postgresVersion string = '16'
param databaseName string = 'organizational_singularity'

param logAnalyticsWorkspaceId string

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    version: postgresVersion
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    storage: {
      storageSizeGB: storageSizeGB
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }

  resource database 'databases@2024-08-01' = {
    name: databaseName
    properties: {
      charset: 'UTF8'
      collation: 'en_US.utf8'
    }
  }

  // Dev-only convenience: allow Azure services (Container Apps) to reach the server.
  // Tighten to VNet integration / private endpoints per blueprint section 9.1 as the
  // deployment moves toward prod-enterprise / prod-sovereign tiers.
  resource allowAzureServices 'firewallRules@2024-08-01' = {
    name: 'AllowAzureServices'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress: '0.0.0.0'
    }
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'default'
  scope: server
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
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

output serverId string = server.id
output fullyQualifiedDomainName string = server.properties.fullyQualifiedDomainName
