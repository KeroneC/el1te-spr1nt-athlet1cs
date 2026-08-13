param name string
param location string
param appServicePlanId string
param sqlServerFqdn string
param databaseName string
param jwtIssuer string
param jwtAudience string
param jwtSecretUri string
param blobServiceUri string
param mediaContainerName string
param backfillMediaDerivativesOnStartup bool = false
param applicationInsightsConnectionString string
param publicBaseUrl string
param allowedOrigins array
param releaseSha string
param storeEnabled bool = false
param storeCheckoutEnabled bool = false
param storePublicPreviewEnabled bool = false
param squareEnvironment string = 'Sandbox'
param squareLocationId string = ''
param squareWebhookNotificationUrl string = ''
param squareCheckoutReturnUrl string = ''
param squareAccessTokenSecretUri string = ''
param squareWebhookSignatureKeySecretUri string = ''
param transactionalEmailConnectionSecretUri string
param transactionalEmailSenderAddress string
param transactionalEmailReplyToAddress string = ''
param tags object = {}

var connectionString = 'Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
var corsAppSettings = map(allowedOrigins, (origin, index) => {
  name: 'Cors__AllowedOrigins__${index}'
  value: origin
})
var squareSecretAppSettings = concat(
  empty(squareAccessTokenSecretUri) ? [] : [
    {
      name: 'Square__AccessToken'
      value: '@Microsoft.KeyVault(SecretUri=${squareAccessTokenSecretUri})'
    }
  ],
  empty(squareWebhookSignatureKeySecretUri) ? [] : [
    {
      name: 'Square__WebhookSignatureKey'
      value: '@Microsoft.KeyVault(SecretUri=${squareWebhookSignatureKeySecretUri})'
    }
  ])

resource api 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  kind: 'app,linux'
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      http20Enabled: true
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
      cors: {
        allowedOrigins: allowedOrigins
        supportCredentials: false
      }
      appSettings: concat([
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: connectionString
        }
        {
          name: 'Database__UseManagedIdentity'
          value: 'true'
        }
        {
          name: 'Jwt__Issuer'
          value: jwtIssuer
        }
        {
          name: 'Jwt__Audience'
          value: jwtAudience
        }
        {
          name: 'Jwt__ExpiresMinutes'
          value: '60'
        }
        {
          name: 'Jwt__Key'
          value: '@Microsoft.KeyVault(SecretUri=${jwtSecretUri})'
        }
        {
          name: 'MediaStorage__Provider'
          value: 'AzureBlob'
        }
        {
          name: 'MediaStorage__BlobServiceUri'
          value: blobServiceUri
        }
        {
          name: 'MediaStorage__ContainerName'
          value: mediaContainerName
        }
        {
          name: 'MediaStorage__PublicBaseUrl'
          value: publicBaseUrl
        }
        {
          name: 'MediaStorage__MaxFileSizeBytes'
          value: '10485760'
        }
        {
          name: 'MediaStorage__BackfillDerivativesOnStartup'
          value: string(backfillMediaDerivativesOnStartup)
        }
        {
          name: 'AdminInvitations__SiteUrl'
          value: jwtAudience
        }
        {
          name: 'AdminInvitations__ExpiresHours'
          value: '72'
        }
        {
          name: 'AuthFeatures__AllowPublicRegistration'
          value: 'false'
        }
        {
          name: 'TransactionalEmail__Provider'
          value: 'AzureCommunicationServices'
        }
        {
          name: 'TransactionalEmail__ConnectionString'
          value: '@Microsoft.KeyVault(SecretUri=${transactionalEmailConnectionSecretUri})'
        }
        {
          name: 'TransactionalEmail__SenderAddress'
          value: transactionalEmailSenderAddress
        }
        {
          name: 'TransactionalEmail__ReplyToAddress'
          value: transactionalEmailReplyToAddress
        }
        {
          name: 'TransactionalEmail__AdminSiteUrl'
          value: jwtAudience
        }
        {
          name: 'Store__Enabled'
          value: string(storeEnabled)
        }
        {
          name: 'Store__PublicPreviewEnabled'
          value: string(storePublicPreviewEnabled)
        }
        {
          name: 'Store__CheckoutEnabled'
          value: string(storeCheckoutEnabled)
        }
        {
          name: 'Store__Currency'
          value: 'USD'
        }
        {
          name: 'Store__ReservationMinutes'
          value: '30'
        }
        {
          name: 'Store__DefaultLowStockThreshold'
          value: '3'
        }
        {
          name: 'Store__OutboxPollSeconds'
          value: '5'
        }
        {
          name: 'Store__ReconciliationMinutes'
          value: '5'
        }
        {
          name: 'Store__PublicSiteUrl'
          value: jwtAudience
        }
        {
          name: 'Square__Environment'
          value: squareEnvironment
        }
        {
          name: 'Square__ApiVersion'
          value: '2026-07-15'
        }
        {
          name: 'Square__LocationId'
          value: squareLocationId
        }
        {
          name: 'Square__WebhookNotificationUrl'
          value: squareWebhookNotificationUrl
        }
        {
          name: 'Square__CheckoutReturnUrl'
          value: squareCheckoutReturnUrl
        }
        {
          name: 'Square__RequestTimeoutSeconds'
          value: '15'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'RELEASE_SHA'
          value: releaseSha
        }
        {
          name: 'OTEL_SERVICE_NAME'
          value: 'api'
        }
        {
          name: 'OTEL_SERVICE_VERSION'
          value: releaseSha
        }
        {
          name: 'WEBSITES_CONTAINER_START_TIME_LIMIT'
          value: '600'
        }
      ], corsAppSettings, squareSecretAppSettings)
    }
  }
}

output name string = api.name
output principalId string = api.identity.principalId
output url string = 'https://${api.properties.defaultHostName}'
