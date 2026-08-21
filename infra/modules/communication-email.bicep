param baseName string
param keyVaultName string
param tags object = {}
param logAnalyticsWorkspaceId string
param monitoringActionGroupId string
@description('Customer-managed email domain to create for DNS verification. Leave empty in demo.')
param customEmailDomainName string = ''
@description('Link the verified customer-managed domain instead of the Azure-managed demo domain.')
param useCustomEmailDomain bool = false
@description('Local part of the transactional sender when a custom domain is linked.')
param senderUsername string = 'orders'
@description('Display name shown on transactional messages from the custom sender.')
param senderDisplayName string = 'El1te Spr1nt Athlet1cs'

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

resource customDomain 'Microsoft.Communication/emailServices/domains@2023-03-31' = if (!empty(customEmailDomainName)) {
  parent: emailService
  name: customEmailDomainName
  location: 'global'
  properties: {
    domainManagement: 'CustomerManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource customSenderUsername 'Microsoft.Communication/emailServices/domains/senderUsernames@2023-04-01-preview' = if (useCustomEmailDomain) {
  parent: customDomain
  name: senderUsername
  properties: {
    displayName: senderDisplayName
    username: senderUsername
  }
}

resource communicationService 'Microsoft.Communication/communicationServices@2023-03-31' = {
  name: communicationServiceName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: 'United States'
    linkedDomains: useCustomEmailDomain ? [customDomain.id] : [managedDomain.id]
  }
}

resource emailDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: communicationService
  name: 'transactional-email-operations'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'EmailSendMailOperational'
        enabled: true
      }
      {
        category: 'EmailStatusUpdateOperational'
        enabled: true
      }
    ]
  }
}

resource emailDeliveryAlert 'Microsoft.Insights/scheduledQueryRules@2023-12-01' = {
  name: '${baseName}-email-delivery-failures'
  location: resourceGroup().location
  tags: tags
  kind: 'LogAlert'
  properties: {
    displayName: 'Transactional email delivery failures'
    description: 'At least three transactional emails were failed, bounced, suppressed, quarantined, or filtered as spam within fifteen minutes.'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    scopes: [logAnalyticsWorkspaceId]
    // Azure requires auto mitigation to be disabled when muteActionsDuration is configured.
    autoMitigate: false
    muteActionsDuration: 'PT30M'
    skipQueryValidation: true
    criteria: {
      allOf: [
        {
          query: 'ACSEmailStatusUpdateOperational | where DeliveryStatus in~ ("Failed", "Bounced", "Suppressed", "Quarantined", "FilteredSpam")'
          timeAggregation: 'Count'
          operator: 'GreaterThanOrEqual'
          threshold: 3
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [monitoringActionGroupId]
    }
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
output senderAddress string = useCustomEmailDomain
  ? '${senderUsername}@${customEmailDomainName}'
  : 'DoNotReply@${managedDomain.properties.fromSenderDomain}'
output connectionSecretUri string = connectionSecret.properties.secretUri
output customEmailDomainResourceId string = empty(customEmailDomainName) ? '' : customDomain.id
