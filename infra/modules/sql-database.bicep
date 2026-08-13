param serverName string
param databaseName string
param location string
param skuName string
param tags object = {}
@minValue(7)
@maxValue(35)
param backupRetentionDays int = 14

resource server 'Microsoft.Sql/servers@2023-08-01-preview' existing = {
  name: serverName
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: server
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

resource shortTermRetention 'Microsoft.Sql/servers/databases/backupShortTermRetentionPolicies@2023-08-01' = {
  parent: database
  name: 'default'
  properties: {
    retentionDays: backupRetentionDays
    diffBackupIntervalInHours: 24
  }
}

output id string = database.id
output name string = database.name
