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

@secure()
@minLength(32)
@description('JWT signing key supplied only at deployment time.')
param jwtSigningKey string

@description('Optional custom frontend HTTPS origin to allow in addition to the Azure hostname.')
param frontendAllowedOrigin string = ''

@description('Resource tags.')
param tags object = {}

var normalizedPrefix = toLower(replace(namePrefix, '-', ''))
var suffix = substring(uniqueString(resourceGroup().id, environmentName), 0, 6)
var baseName = '${normalizedPrefix}-${environmentName}-${suffix}'
var planName = '${baseName}-plan'
var webAppName = take('${baseName}-web', 60)
var apiAppName = take('${baseName}-api', 60)
var sqlServerName = take('${baseName}-sql', 63)
var sqlDatabaseName = '${normalizedPrefix}-${environmentName}-db'
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
    serverName: sqlServer.outputs.name
    skuName: sqlDatabaseSkuName
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
    jwtSigningKey: jwtSigningKey
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
    appServicePlanId: plan.outputs.id
    location: location
    name: webAppName
    tags: tags
  }
}

output apiAppName string = api.outputs.name
output apiPrincipalId string = api.outputs.principalId
output apiUrl string = api.outputs.url
output sqlDatabaseName string = sqlDatabase.outputs.name
output sqlServerName string = sqlServer.outputs.name
output webAppName string = web.outputs.name
output webUrl string = web.outputs.url
