@description('Generic diagnostic settings module. Invoke with the module\'s `scope:` property set to the target resource — this module attaches a diagnosticSettings extension resource to whatever it is scoped into. Use this for resource types not already wired with inline diagnostics in their own module.')
param name string = 'default'
param logAnalyticsWorkspaceId string
param logCategoryGroups array = ['allLogs']
param metricCategories array = ['AllMetrics']

resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: name
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [for categoryGroup in logCategoryGroups: {
      categoryGroup: categoryGroup
      enabled: true
    }]
    metrics: [for category in metricCategories: {
      category: category
      enabled: true
    }]
  }
}
