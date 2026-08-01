param baseName string
param keyVaultName string
param tags object = {}

var emailServiceName = take('${baseName}-email', 63)
var communicationServiceName = take('${baseName}-communication', 63)

resource emailService 'Microsoft.Communication/emailServices@2023-03-31' = {
  name: emailServiceName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: 'United States'
  }
}

resource managedDomain 'Microsoft.Communication/emailServices/domains@2023-03-31' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: 'global'
  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource communicationService 'Microsoft.Communication/communicationServices@2023-03-31' = {
  name: communicationServiceName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: 'United States'
    linkedDomains: [managedDomain.id]
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' existing = { name: keyVaultName }
resource connectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'acs-email-connection-string'
  properties: {
    value: communicationService.listKeys().primaryConnectionString
  }
}

output communicationServiceName string = communicationService.name
output senderAddress string = 'DoNotReply@${managedDomain.properties.fromSenderDomain}'
output connectionSecretUri string = connectionSecret.properties.secretUri
