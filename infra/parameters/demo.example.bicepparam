using '../main.bicep'

param environmentName = 'demo'
param location = 'eastus2'
param namePrefix = 'replacewithclub'
param appServiceSkuName = 'B1'
param sqlDatabaseSkuName = 'Basic'
param sqlAdminLogin = 'replacewithadmin'
param sqlAdminPassword = readEnvironmentVariable('AZURE_SQL_ADMIN_PASSWORD')
param deploymentPrincipalObjectId = '00000000-0000-0000-0000-000000000000'
param frontendAllowedOrigin = ''
param tags = {
  application: 'el1te-spr1nt-athlet1cs'
  environment: 'demo'
  managedBy: 'bicep'
  funding: 'nonprofit-grant'
}
