@description('Generic Container App module used for both the web (Next.js) and api (.NET) workloads.')
param name string
param location string
param tags object = {}

param containerAppsEnvironmentId string

@description('Full image reference, e.g. acrosdeveus201.azurecr.io/os-api:<digest>')
param image string

param targetPort int

@description('Non-secret environment variables.')
param environmentVariables array = []

@description('Secret environment variables sourced from Key Vault references, e.g. [{ name: \'OS_DATABASE_CONNECTION_STRING\', keyVaultUrl: \'https://kv-os-core-dev-eus2-01.vault.azure.net/secrets/db-connection-string\', identity: \'system\' }]')
param keyVaultSecretRefs array = []

param minReplicas int = 0
param maxReplicas int = 3

param cpu string = '0.5'
param memory string = '1Gi'

@description('Resource ID of the Container Registry so the system-assigned identity can be granted AcrPull by the caller.')
param registryLoginServer string

var secrets = [for s in keyVaultSecretRefs: {
  name: s.name
  keyVaultUrl: s.keyVaultUrl
  identity: 'system'
}]

var secretEnvVars = [for s in keyVaultSecretRefs: {
  name: s.name
  secretRef: s.name
}]

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      ingress: {
        external: true
        targetPort: targetPort
        transport: 'auto'
      }
      registries: [
        {
          server: registryLoginServer
          identity: 'system'
        }
      ]
      secrets: secrets
    }
    template: {
      containers: [
        {
          name: name
          image: image
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: concat(environmentVariables, secretEnvVars)
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

output principalId string = containerApp.identity.principalId
output fqdn string = containerApp.properties.configuration.ingress.fqdn
output containerAppId string = containerApp.id
