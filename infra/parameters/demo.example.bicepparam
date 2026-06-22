using '../main.bicep'

param environmentName = 'demo'
param location = 'eastus'
param namePrefix = 'replacewithclub'
param appServiceSkuName = 'B1'
param sqlDatabaseSkuName = 'Basic'
param sqlAdminLogin = 'replacewithadmin'
param sqlAdminPassword = readEnvironmentVariable('AZURE_SQL_ADMIN_PASSWORD')
param jwtSigningKey = readEnvironmentVariable('JWT_SIGNING_KEY')
param frontendAllowedOrigin = ''
param tags = {
  application: 'el1te-spr1nt-athlet1cs'
  environment: 'demo'
  managedBy: 'bicep'
}
