@description('Container Apps Job for document ingestion, report generation, and scheduled work (blueprint 4.1).')
param name string
param location string
param tags object = {}

param containerAppsEnvironmentId string
param image string
param environmentVariables array = []

@allowed(['Manual', 'Schedule', 'Event'])
param triggerType string = 'Schedule'

@description('Required when triggerType is Schedule, e.g. \'0 * * * *\' for hourly.')
param cronExpression string = ''

param cpu string = '0.5'
param memory string = '1Gi'
param registryLoginServer string

resource job 'Microsoft.App/jobs@2024-03-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: containerAppsEnvironmentId
    configuration: {
      triggerType: triggerType
      replicaTimeout: 1800
      replicaRetryLimit: 1
      scheduleTriggerConfig: triggerType == 'Schedule' ? {
        cronExpression: cronExpression
        parallelism: 1
        replicaCompletionCount: 1
      } : null
      registries: [
        {
          server: registryLoginServer
          identity: 'system'
        }
      ]
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
          env: environmentVariables
        }
      ]
    }
  }
}

output principalId string = job.identity.principalId
output jobId string = job.id
