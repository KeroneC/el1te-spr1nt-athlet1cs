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
param applicationInsightsConnectionString string
param publicBaseUrl string
param allowedOrigins array
param tags object = {}

var connectionString = 'Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${databaseName};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
var corsAppSettings = map(allowedOrigins, (origin, index) => {
  name: 'Cors__AllowedOrigins__${index}'
  value: origin
})

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
      healthCheckPath: '/health/ready'
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
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
      ], corsAppSettings)
    }
  }
}

output name string = api.name
output principalId string = api.identity.principalId
output url string = 'https://${api.properties.defaultHostName}'
