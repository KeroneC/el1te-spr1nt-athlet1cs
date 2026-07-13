param name string
param location string
param tenantId string
param tags object = {}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    tenantId: tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: true
    publicNetworkAccess: 'Enabled'
    sku: { family: 'A', name: 'standard' }
  }
}

output id string = vault.id
output vaultUri string = vault.properties.vaultUri
