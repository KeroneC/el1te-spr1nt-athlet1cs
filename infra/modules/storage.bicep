param name string
param location string
param containerName string = 'media'
param tags object = {}
@minValue(1)
@maxValue(365)
param softDeleteRetentionDays int = 30
@minValue(1)
@maxValue(3650)
param previousVersionRetentionDays int = 90

resource account 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: name
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: account
  name: 'default'
  properties: {
    isVersioningEnabled: true
    deleteRetentionPolicy: { enabled: true, days: softDeleteRetentionDays }
    containerDeleteRetentionPolicy: { enabled: true, days: softDeleteRetentionDays }
  }
}

resource lifecyclePolicy 'Microsoft.Storage/storageAccounts/managementPolicies@2023-05-01' = {
  parent: account
  name: 'default'
  properties: {
    policy: {
      rules: [
        {
          enabled: true
          name: 'expire-old-media-versions'
          type: 'Lifecycle'
          definition: {
            actions: {
              version: {
                delete: {
                  daysAfterCreationGreaterThan: previousVersionRetentionDays
                }
              }
            }
            filters: {
              blobTypes: ['blockBlob']
              prefixMatch: ['${containerName}/']
            }
          }
        }
      ]
    }
  }
}

resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: containerName
  properties: { publicAccess: 'None' }
}

output accountId string = account.id
output blobServiceUri string = account.properties.primaryEndpoints.blob
output containerName string = container.name
