param baseName string
param location string
param tags object = {}

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${baseName}-logs'
  location: location
  tags: tags
  properties: { retentionInDays: 30, sku: { name: 'PerGB2018' } }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${baseName}-insights'
  location: location
  kind: 'web'
  tags: tags
  properties: union({
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
    IngestionMode: 'LogAnalytics'
    RetentionInDays: 30
  }, {
    DailyDataCapInGB: json('0.1')
  })
}

output connectionString string = insights.properties.ConnectionString
