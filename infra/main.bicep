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

@description('Canonical public web URL. Leave empty to use the App Service hostname.')
param webPublicUrl string = ''

@description('Canonical public API URL. Leave empty to use the App Service hostname.')
param apiPublicUrl string = ''

@description('JWT issuer. Leave empty to use the canonical API URL.')
param apiJwtIssuer string = ''

@description('JWT audience. Leave empty to use the canonical web URL.')
param apiJwtAudience string = ''

@allowed(['off', 'report-only', 'enforce'])
@description('Content Security Policy mode for the web application.')
param cspMode string = 'report-only'

@description('Enable cookie-free public browser performance telemetry.')
param browserAnalyticsEnabled bool = true

@description('Allow public search-engine indexing. Keep false until the final production gate passes.')
param publicIndexingEnabled bool = false

@description('Resource tags.')
param tags object = {}
param mediaContainerName string = 'media'

@description('Blob and container soft-delete retention.')
param mediaSoftDeleteRetentionDays int = 30

@description('Age at which noncurrent Blob versions are removed by lifecycle policy.')
param mediaPreviousVersionRetentionDays int = 90

@description('Azure SQL point-in-time backup retention.')
param sqlBackupRetentionDays int = 14

@description('Email recipient for Azure Monitor operational alerts.')
param monitoringAlertEmail string

@description('Customer-managed ACS Email domain to prepare for verification.')
param transactionalEmailDomain string = ''

@description('Use the custom email domain only after ownership, SPF, and DKIM are verified.')
param transactionalEmailUseCustomDomain bool = false

@description('Sender local part for the verified custom email domain.')
param transactionalEmailSenderUsername string = 'orders'

@description('Reply-To address for transactional email.')
param transactionalEmailReplyToAddress string = ''

@description('Keep false until the final store cutover.')
param storeEnabled bool = false

@description('Enable transactional checkout only after store content and Square Sandbox configuration are ready.')
param storeCheckoutEnabled bool = false

@description('Expose the read-only catalog/configurator preview without enabling Square, orders, or workers.')
param storePublicPreviewEnabled bool = false

@allowed(['internal', 'external'])
@description('Controls whether global navigation points to the internal shop or the legacy external Square storefront.')
param storeNavigationMode string = 'external'

@allowed([
  'Sandbox'
  'Production'
])
@description('Square API environment used when the store is enabled.')
param squareEnvironment string = 'Sandbox'

@description('Square location identifier. Required only when the store is enabled.')
param squareLocationId string = ''

@description('Exact public Square webhook notification URL. Required only when the store is enabled.')
param squareWebhookNotificationUrl string = ''

@description('Public checkout return URL. Required only when the store is enabled.')
param squareCheckoutReturnUrl string = ''

@description('Optional Key Vault secret URI containing the Square access token.')
param squareAccessTokenSecretUri string = ''

@description('Optional Key Vault secret URI containing the Square webhook signature key.')
param squareWebhookSignatureKeySecretUri string = ''

@description('Immutable Git commit SHA promoted by the deployment workflow.')
@minLength(7)
@maxLength(40)
param releaseSha string

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
var defaultApiOrigin = 'https://${apiAppName}.azurewebsites.net'
var canonicalWebOrigin = empty(webPublicUrl) ? defaultWebOrigin : webPublicUrl
var canonicalApiOrigin = empty(apiPublicUrl) ? defaultApiOrigin : apiPublicUrl
var jwtIssuer = empty(apiJwtIssuer) ? canonicalApiOrigin : apiJwtIssuer
var jwtAudience = empty(apiJwtAudience) ? canonicalWebOrigin : apiJwtAudience
var allowedOrigins = union([defaultWebOrigin], [canonicalWebOrigin], empty(frontendAllowedOrigin) ? [] : [frontendAllowedOrigin])

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
    backupRetentionDays: sqlBackupRetentionDays
    tags: tags
  }
}

module storage 'modules/storage.bicep' = {
  name: 'media-storage'
  params: {
    name: storageName
    location: location
    containerName: mediaContainerName
    softDeleteRetentionDays: mediaSoftDeleteRetentionDays
    previousVersionRetentionDays: mediaPreviousVersionRetentionDays
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
    apiReadinessUrl: 'https://${apiAppName}.azurewebsites.net/health/ready'
    alertEmail: monitoringAlertEmail
    tags: tags
  }
}

module communicationEmail 'modules/communication-email.bicep' = {
  name: 'communication-email'
  params: {
    baseName: baseName
    keyVaultName: vaultName
    logAnalyticsWorkspaceId: monitoring.outputs.workspaceId
    monitoringActionGroupId: monitoring.outputs.actionGroupId
    customEmailDomainName: transactionalEmailDomain
    useCustomEmailDomain: transactionalEmailUseCustomDomain
    senderUsername: transactionalEmailSenderUsername
    tags: tags
  }
  dependsOn: [deploymentSecretsOfficer]
}

module api 'modules/api-app.bicep' = {
  name: 'api-app'
  params: {
    allowedOrigins: allowedOrigins
    appServicePlanId: plan.outputs.id
    databaseName: sqlDatabase.outputs.name
    jwtAudience: jwtAudience
    jwtIssuer: jwtIssuer
    jwtSecretUri: '${vault.outputs.vaultUri}secrets/jwt-signing-key'
    blobServiceUri: storage.outputs.blobServiceUri
    mediaContainerName: storage.outputs.containerName
    backfillMediaDerivativesOnStartup: environmentName == 'demo'
    applicationInsightsConnectionString: monitoring.outputs.connectionString
    publicBaseUrl: canonicalApiOrigin
    releaseSha: releaseSha
    storeEnabled: storeEnabled
    storeCheckoutEnabled: storeCheckoutEnabled
    storePublicPreviewEnabled: storePublicPreviewEnabled
    squareEnvironment: squareEnvironment
    squareLocationId: squareLocationId
    squareWebhookNotificationUrl: squareWebhookNotificationUrl
    squareCheckoutReturnUrl: squareCheckoutReturnUrl
    squareAccessTokenSecretUri: squareAccessTokenSecretUri
    squareWebhookSignatureKeySecretUri: squareWebhookSignatureKeySecretUri
    transactionalEmailConnectionSecretUri: communicationEmail.outputs.connectionSecretUri
    transactionalEmailSenderAddress: communicationEmail.outputs.senderAddress
    transactionalEmailReplyToAddress: transactionalEmailReplyToAddress
    location: location
    name: apiAppName
    sqlServerFqdn: sqlServer.outputs.fullyQualifiedDomainName
    tags: tags
  }
}

module web 'modules/web-app.bicep' = {
  name: 'web-app'
  params: {
    apiBaseUrl: canonicalApiOrigin
    siteUrl: canonicalWebOrigin
    applicationInsightsConnectionString: monitoring.outputs.connectionString
    appServicePlanId: plan.outputs.id
    location: location
    name: webAppName
    releaseSha: releaseSha
    browserAnalyticsEnabled: browserAnalyticsEnabled
    cspMode: cspMode
    deploymentEnvironment: environmentName
    publicIndexingEnabled: publicIndexingEnabled
    storeNavigationMode: storeNavigationMode
    tags: tags
  }
}

resource deployedStorage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = { name: storageName }
resource deployedVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = { name: vaultName }
resource deployedSqlServer 'Microsoft.Sql/servers@2023-08-01-preview' existing = { name: sqlServerName }
resource deployedSqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' existing = {
  parent: deployedSqlServer
  name: sqlDatabaseName
}

resource productionStorageDeleteLock 'Microsoft.Authorization/locks@2020-05-01' = if (environmentName == 'production') {
  name: 'protect-production-media'
  scope: deployedStorage
  properties: {
    level: 'CanNotDelete'
    notes: 'Remove only during an approved production decommission or recovery operation.'
  }
  // The scoped resource is declared as existing, so Bicep cannot infer the
  // storage module dependency during a first-time environment deployment.
  dependsOn: [storage]
}

resource productionSqlDeleteLock 'Microsoft.Authorization/locks@2020-05-01' = if (environmentName == 'production') {
  name: 'protect-production-sql'
  scope: deployedSqlDatabase
  properties: {
    level: 'CanNotDelete'
    notes: 'Remove only during an approved production decommission or recovery operation.'
  }
  dependsOn: [sqlDatabase]
}

resource productionVaultDeleteLock 'Microsoft.Authorization/locks@2020-05-01' = if (environmentName == 'production') {
  name: 'protect-production-secrets'
  scope: deployedVault
  properties: {
    level: 'CanNotDelete'
    notes: 'Remove only during an approved production decommission or recovery operation.'
  }
  dependsOn: [vault]
}

resource blobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageName, apiAppName, 'blob-contributor')
  scope: deployedStorage
  properties: {
    principalId: api.outputs.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  }
}

resource deploymentBlobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageName, deploymentPrincipalObjectId, 'deployment-blob-contributor')
  scope: deployedStorage
  properties: {
    principalId: deploymentPrincipalObjectId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  }
  dependsOn: [storage]
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
output appServicePlanName string = plan.outputs.name
output sqlDatabaseName string = sqlDatabase.outputs.name
output sqlServerName string = sqlServer.outputs.name
output webAppName string = web.outputs.name
output webUrl string = web.outputs.url
output storageAccountName string = storageName
output keyVaultName string = vaultName
output canonicalWebUrl string = canonicalWebOrigin
output canonicalApiUrl string = canonicalApiOrigin
output communicationServiceName string = communicationEmail.outputs.communicationServiceName
output customEmailDomainResourceId string = communicationEmail.outputs.customEmailDomainResourceId
