param name string
param location string
param appServicePlanId string
param apiBaseUrl string
param applicationInsightsConnectionString string
param tags object = {}
param releaseSha string
param browserAnalyticsEnabled bool = false
@allowed(['off', 'report-only', 'enforce'])
param cspMode string = 'report-only'

resource web 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  kind: 'app,linux'
  tags: tags
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      appCommandLine: 'node apps/web/server.js'
      ftpsState: 'Disabled'
      healthCheckPath: '/'
      http20Enabled: true
      linuxFxVersion: 'NODE|22-lts'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'API_BASE_URL'
          value: apiBaseUrl
        }
        {
          name: 'NODE_ENV'
          value: 'production'
        }
        {
          name: 'SITE_URL'
          value: 'https://${name}.azurewebsites.net'
        }
        {
          name: 'PORT'
          value: '8080'
        }
        {
          name: 'WEBSITES_PORT'
          value: '8080'
        }
        {
          name: 'WEBSITES_CONTAINER_START_TIME_LIMIT'
          value: '600'
        }
        {
          name: 'DEPLOYMENT_ENVIRONMENT'
          value: 'demo'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONFIGURATION_CONTENT'
          value: '{"roleName":"web","enableAutoCollectConsole":true}'
        }
        {
          name: 'APPINSIGHTS_JAVASCRIPT_ENABLED'
          value: 'false'
        }
        {
          name: 'BROWSER_ANALYTICS_ENABLED'
          value: string(browserAnalyticsEnabled)
        }
        {
          name: 'CSP_MODE'
          value: cspMode
        }
        {
          name: 'RELEASE_SHA'
          value: releaseSha
        }
        {
          name: 'OTEL_SERVICE_NAME'
          value: 'web'
        }
        {
          name: 'OTEL_SERVICE_VERSION'
          value: releaseSha
        }
      ]
    }
  }
}

output name string = web.name
output url string = 'https://${web.properties.defaultHostName}'
