using '../main.bicep'

param environmentName = 'production'
param location = 'eastus2'
param namePrefix = 'replacewithclub'
param appServiceSkuName = 'B1'
param sqlDatabaseSkuName = 'Basic'
param sqlAdminLogin = 'replacewithadmin'
param sqlAdminPassword = readEnvironmentVariable('AZURE_SQL_ADMIN_PASSWORD')
param deploymentPrincipalObjectId = '00000000-0000-0000-0000-000000000000'
param monitoringAlertEmail = 'operations@example.org'
param releaseSha = '0000000000000000000000000000000000000000'

// Apply the custom URLs only after their App Service DNS validation records exist.
param webPublicUrl = 'https://www.el1tespr1ntathlet1cs.org'
param apiPublicUrl = 'https://api.el1tespr1ntathlet1cs.org'
param frontendAllowedOrigin = 'https://el1tespr1ntathlet1cs.org'
param cspMode = 'enforce'
param browserAnalyticsEnabled = true
param homeAllAmericanShowcaseEnabled = false
param publicIndexingEnabled = false

param mediaSoftDeleteRetentionDays = 30
param mediaPreviousVersionRetentionDays = 90
param sqlBackupRetentionDays = 14

// First deploy creates the domain resource for DNS verification. Set transactionalEmailUseCustomDomain only after Azure reports verification complete.
param transactionalEmailDomain = 'updates.el1tespr1ntathlet1cs.org'
param transactionalEmailUseCustomDomain = false
param transactionalEmailSenderUsername = 'orders'
param transactionalEmailReplyToAddress = 'el1tespr1nt.athlet1cs@gmail.com'

param storeNavigationMode = 'external'
param storePublicPreviewEnabled = false
param storeEnabled = false
param storeCheckoutEnabled = false
param squareEnvironment = 'Production'

param tags = {
  application: 'el1te-spr1nt-athlet1cs'
  environment: 'production'
  managedBy: 'bicep'
  funding: 'nonprofit-grant'
}
