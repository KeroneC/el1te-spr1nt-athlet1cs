param baseName string
param keyVaultName string
param tags object = {}
param logAnalyticsWorkspaceId string
param monitoringActionGroupId string

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
    autoMitigate: true
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
output senderAddress string = 'DoNotReply@${managedDomain.properties.fromSenderDomain}'
output connectionSecretUri string = connectionSecret.properties.secretUri
