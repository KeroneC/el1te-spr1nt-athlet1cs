targetScope = 'resourceGroup'

@description('Short environment label such as demo.')
@minLength(2)
@maxLength(12)
param environmentName string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Lowercase project prefix used in resource names.')
@minLength(3)
@maxLength(16)
param namePrefix string

@description('Linux App Service plan SKU. B1 is a low-cost demo default, not a free tier.')
param appServiceSkuName string = 'B1'

@description('Azure SQL database SKU.')
param sqlDatabaseSkuName string = 'Basic'

@description('Temporary SQL administrator login used only to bootstrap Phase 6B.')
param sqlAdminLogin string

@secure()
@description('Temporary SQL administrator password supplied only at deployment time.')
param sqlAdminPassword string

@description('Object ID of the GitHub OIDC service principal used to initialize Key Vault secrets.')
param deploymentPrincipalObjectId string

@description('Optional custom frontend HTTPS origin to allow in addition to the Azure hostname.')
param frontendAllowedOrigin string = ''

@description('Resource tags.')
param tags object = {}
param mediaContainerName string = 'media'

var normalizedPrefix = toLower(replace(namePrefix, '-', ''))
var normalizedEnvironment = toLower(replace(environmentName, '-', ''))
var suffix = substring(uniqueString(resourceGroup().id, environmentName), 0, 6)
var baseName = '${normalizedPrefix}-${environmentName}-${suffix}'
var planName = '${baseName}-plan'
var webAppName = take('${baseName}-web', 60)
var apiAppName = take('${baseName}-api', 60)
var sqlServerName = take('${baseName}-sql', 63)
var sqlDatabaseName = '${normalizedPrefix}-${environmentName}-db'
var storageName = take('${normalizedPrefix}${environmentName}${suffix}media', 24)
// Key Vault names must be alphanumeric, end in a letter or digit, and fit within 24 characters.
var vaultName = take('kv${normalizedPrefix}${normalizedEnvironment}${suffix}', 24)
var defaultWebOrigin = 'https://${webAppName}.azurewebsites.net'
var allowedOrigins = empty(frontendAllowedOrigin)
  ? [defaultWebOrigin]
  : union([defaultWebOrigin], [frontendAllowedOrigin])

module plan 'modules/app-service-plan.bicep' = {
  name: 'app-service-plan'
  params: {
    location: location
    name: planName
    skuName: appServiceSkuName
    tags: tags
  }
}

module sqlServer 'modules/sql-server.bicep' = {
  name: 'sql-server'
  params: {
    administratorLogin: sqlAdminLogin
    administratorPassword: sqlAdminPassword
    location: location
    name: sqlServerName
    tags: tags
  }
}

module sqlDatabase 'modules/sql-database.bicep' = {
  name: 'sql-database'
  params: {
    databaseName: sqlDatabaseName
    location: location
    serverName: sqlServer.outputs.name
    skuName: sqlDatabaseSkuName
    tags: tags
  }
}

module storage 'modules/storage.bicep' = {
  name: 'media-storage'
  params: {
    name: storageName
    location: location
    containerName: mediaContainerName
    tags: tags
  }
}

module vault 'modules/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    name: vaultName
    location: location
    tenantId: tenant().tenantId
    tags: tags
  }
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    baseName: baseName
    location: location
    tags: tags
  }
}

module api 'modules/api-app.bicep' = {
  name: 'api-app'
  params: {
    allowedOrigins: allowedOrigins
    appServicePlanId: plan.outputs.id
    databaseName: sqlDatabase.outputs.name
    jwtAudience: defaultWebOrigin
    jwtIssuer: 'https://${apiAppName}.azurewebsites.net'
    jwtSecretUri: '${vault.outputs.vaultUri}secrets/jwt-signing-key'
    blobServiceUri: storage.outputs.blobServiceUri
    mediaContainerName: storage.outputs.containerName
    applicationInsightsConnectionString: monitoring.outputs.connectionString
    publicBaseUrl: 'https://${apiAppName}.azurewebsites.net'
    location: location
    name: apiAppName
    sqlServerFqdn: sqlServer.outputs.fullyQualifiedDomainName
    tags: tags
  }
}

module web 'modules/web-app.bicep' = {
  name: 'web-app'
  params: {
    apiBaseUrl: api.outputs.url
    applicationInsightsConnectionString: monitoring.outputs.connectionString
    appServicePlanId: plan.outputs.id
    location: location
    name: webAppName
    tags: tags
  }
}

resource deployedStorage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = { name: storageName }
resource deployedVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = { name: vaultName }

resource blobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageName, apiAppName, 'blob-contributor')
  scope: deployedStorage
  properties: {
    principalId: api.outputs.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  }
}

resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vaultName, apiAppName, 'key-vault-secrets-user')
  scope: deployedVault
  properties: {
    principalId: api.outputs.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

resource deploymentSecretsOfficer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vaultName, deploymentPrincipalObjectId, 'key-vault-secrets-officer')
  scope: deployedVault
  properties: {
    principalId: deploymentPrincipalObjectId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')
  }
  dependsOn: [vault]
}

output apiAppName string = api.outputs.name
output apiPrincipalId string = api.outputs.principalId
output apiUrl string = api.outputs.url
output sqlDatabaseName string = sqlDatabase.outputs.name
output sqlServerName string = sqlServer.outputs.name
output webAppName string = web.outputs.name
output webUrl string = web.outputs.url
output storageAccountName string = storageName
output keyVaultName string = vaultName
