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

@description('Plain-value secrets, e.g. [{ name: \'OS_DATABASE_CONNECTION_STRING\', value: \'...\' }]. Only meant as a temporary bypass when the Key Vault Secrets User role assignment cannot be created yet -- prefer keyVaultSecretRefs once role assignments work again.')
param plainSecrets array = []

@description('ACR admin username -- temporary bypass for when the AcrPull role assignment cannot be created yet. Leave empty to keep pulling via the system-assigned identity (the default, correct path).')
param registryUsername string = ''

@secure()
param registryPassword string = ''

param minReplicas int = 0
param maxReplicas int = 3

param cpu string = '0.5'
param memory string = '1Gi'

@description('Resource ID of the Container Registry so the system-assigned identity can be granted AcrPull by the caller.')
param registryLoginServer string

// Container Apps secret names must be lowercase alphanumeric/hyphens -- the env var names
// they back (e.g. AUTH_MICROSOFT_ENTRA_ID_SECRET) don't meet that, so derive a valid secret
// name separately and keep the real env var name on the `env` entry that references it.
var keyVaultSecrets = [for s in keyVaultSecretRefs: {
  name: toLower(replace(s.name, '_', '-'))
  keyVaultUrl: s.keyVaultUrl
  identity: 'system'
}]

var plainSecretEntries = [for s in plainSecrets: {
  name: toLower(replace(s.name, '_', '-'))
  value: s.value
}]

var registryPasswordSecret = !empty(registryUsername) ? [{
  name: '${name}-registry-password'
  value: registryPassword
}] : []

var secrets = concat(keyVaultSecrets, plainSecretEntries, registryPasswordSecret)

var keyVaultSecretEnvVars = [for s in keyVaultSecretRefs: { name: s.name, secretRef: toLower(replace(s.name, '_', '-')) }]
var plainSecretEnvVars = [for s in plainSecrets: { name: s.name, secretRef: toLower(replace(s.name, '_', '-')) }]
var secretEnvVars = concat(keyVaultSecretEnvVars, plainSecretEnvVars)

var registryConfig = !empty(registryUsername) ? {
  server: registryLoginServer
  username: registryUsername
  passwordSecretRef: '${name}-registry-password'
} : {
  server: registryLoginServer
  identity: 'system'
}

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
        registryConfig
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
