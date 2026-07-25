param baseName string
param location string
param tags object = {}
param apiReadinessUrl string
param alertEmail string

var readinessTestName = '${baseName}-api-readiness'
var workbookName = guid(resourceGroup().id, baseName, 'support-workbook')
var alertDefinitions = [
  {
    name: '${baseName}-readiness-failures'
    displayName: 'API readiness failures'
    description: 'The API readiness endpoint failed at least twice in ten minutes.'
    severity: 2
    query: 'availabilityResults | where timestamp >= ago(10m) | where name == "${readinessTestName}" and success == false'
    threshold: 2
    windowSize: 'PT10M'
    timeAggregation: 'Count'
  }
  {
    name: '${baseName}-server-failures'
    displayName: 'Sustained server failures'
    description: 'The web or API application returned at least five server failures in ten minutes.'
    severity: 2
    query: 'requests | where timestamp >= ago(10m) | where toint(resultCode) between (500 .. 599)'
    threshold: 5
    windowSize: 'PT10M'
    timeAggregation: 'Count'
  }
  {
    name: '${baseName}-dependency-failures'
    displayName: 'Sustained dependency failures'
    description: 'At least five dependency calls failed in ten minutes.'
    severity: 3
    query: 'dependencies | where timestamp >= ago(10m) | where success == false'
    threshold: 5
    windowSize: 'PT10M'
    timeAggregation: 'Count'
  }
  {
    name: '${baseName}-request-latency'
    displayName: 'Sustained request latency'
    description: 'Request p95 exceeded five seconds with sufficient traffic.'
    severity: 3
    query: 'requests | where timestamp >= ago(15m) | summarize RequestCount=count(), AggregatedValue=percentile(duration, 95) | where RequestCount >= 10'
    threshold: 5000
    windowSize: 'PT15M'
    timeAggregation: 'Average'
  }
]

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${baseName}-logs'
  location: location
  tags: tags
  properties: {
    retentionInDays: 30
    sku: { name: 'PerGB2018' }
    workspaceCapping: {
      dailyQuotaGb: json('0.1')
    }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${baseName}-insights'
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
    IngestionMode: 'LogAnalytics'
    RetentionInDays: 30
  }
}

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: '${baseName}-operations'
  location: 'global'
  tags: tags
  properties: {
    groupShortName: 'el1te-alerts'
    enabled: true
    emailReceivers: [
      {
        name: 'operations-email'
        emailAddress: alertEmail
        useCommonAlertSchema: true
      }
    ]
    smsReceivers: []
    webhookReceivers: []
    itsmReceivers: []
    azureAppPushReceivers: []
    automationRunbookReceivers: []
    voiceReceivers: []
    logicAppReceivers: []
    azureFunctionReceivers: []
    armRoleReceivers: []
    eventHubReceivers: []
  }
}

resource readinessTest 'Microsoft.Insights/webtests@2022-06-15' = {
  name: readinessTestName
  location: location
  tags: union(tags, {
    'hidden-link:${insights.id}': 'Resource'
  })
  properties: {
    SyntheticMonitorId: guid(resourceGroup().id, readinessTestName)
    Name: readinessTestName
    Description: 'Checks database-backed API readiness without sending application data.'
    Enabled: true
    Frequency: 300
    Timeout: 30
    Kind: 'ping'
    RetryEnabled: true
    Locations: [
      { Id: 'us-va-ash-azr' }
      { Id: 'us-ca-sjc-azr' }
    ]
    Configuration: {
      WebTest: '<WebTest Name="${readinessTestName}" Id="${guid(resourceGroup().id, readinessTestName)}" Enabled="True" CssProjectStructure="" CssIteration="" Timeout="30" WorkItemIds="" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010" Description="" CredentialUserName="" CredentialPassword="" PreAuthenticate="True" Proxy="default" StopOnError="False" RecordedResultFile="" ResultsLocale=""><Items><Request Method="GET" Guid="${guid(resourceGroup().id, readinessTestName, 'request')}" Version="1.1" Url="${apiReadinessUrl}" ThinkTime="0" Timeout="30" ParseDependentRequests="False" FollowRedirects="True" RecordResult="True" Cache="False" ResponseTimeGoal="0" Encoding="utf-8" ExpectedHttpStatusCode="200" ExpectedResponseUrl="" ReportingName="" IgnoreHttpStatusCode="False" /></Items></WebTest>'
    }
  }
}

resource queryAlerts 'Microsoft.Insights/scheduledQueryRules@2023-12-01' = [for alert in alertDefinitions: {
  name: alert.name
  location: location
  tags: tags
  kind: 'LogAlert'
  properties: {
    displayName: alert.displayName
    description: alert.description
    severity: alert.severity
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: alert.windowSize
    scopes: [insights.id]
    autoMitigate: true
    skipQueryValidation: false
    criteria: {
      allOf: [
        union({
          query: alert.query
          timeAggregation: alert.timeAggregation
          operator: 'GreaterThanOrEqual'
          threshold: alert.threshold
          failingPeriods: alert.name == '${baseName}-request-latency'
            ? {
                numberOfEvaluationPeriods: 3
                minFailingPeriodsToAlert: 2
              }
            : {
                numberOfEvaluationPeriods: 1
                minFailingPeriodsToAlert: 1
              }
        }, alert.name == '${baseName}-request-latency' ? {
          metricMeasureColumn: 'AggregatedValue'
        } : {})
      ]
    }
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}]

resource supportWorkbook 'Microsoft.Insights/workbooks@2023-06-01' = {
  name: workbookName
  location: location
  tags: tags
  kind: 'shared'
  properties: {
    displayName: 'El1te Platform Support'
    category: 'workbook'
    description: 'Privacy-safe operational health and support-reference lookup.'
    sourceId: insights.id
    serializedData: string({
      version: 'Notebook/1.0'
      fallbackResourceIds: [insights.id]
      items: [
        {
          type: 1
          content: {
            json: '# El1te Platform Support\nUse this workbook for operational readiness and reported support references. Raw telemetry remains protected by Azure RBAC.'
          }
        }
        {
          type: 3
          name: 'readiness-and-success'
          content: {
            version: 'KqlItem/1.0'
            title: 'Readiness and request success — last 24 hours'
            query: 'let readiness = availabilityResults | where timestamp >= ago(24h) | where name == "${readinessTestName}" | summarize Value=round(100.0 * countif(success) / count(), 2) | extend Metric="Readiness success %"; let requestsSummary = requests | where timestamp >= ago(24h) | summarize Value=round(100.0 * countif(success) / count(), 2) | extend Metric="Request success %"; union readiness, requestsSummary | project Metric, Value'
            size: 0
            queryType: 0
            resourceType: 'microsoft.insights/components'
          }
        }
        {
          type: 3
          name: 'failure-trend'
          content: {
            version: 'KqlItem/1.0'
            title: 'Server failures by role'
            query: 'requests | where timestamp >= ago(24h) | where toint(resultCode) between (500 .. 599) | summarize Failures=count() by bin(timestamp, 30m), cloud_RoleName | render timechart'
            size: 0
            queryType: 0
            resourceType: 'microsoft.insights/components'
          }
        }
        {
          type: 3
          name: 'latency'
          content: {
            version: 'KqlItem/1.0'
            title: 'Request latency by role'
            query: 'requests | where timestamp >= ago(24h) | summarize p50=percentile(duration, 50), p95=percentile(duration, 95), Requests=count() by cloud_RoleName'
            size: 0
            queryType: 0
            resourceType: 'microsoft.insights/components'
          }
        }
        {
          type: 3
          name: 'dependencies'
          content: {
            version: 'KqlItem/1.0'
            title: 'Failed dependencies'
            query: 'dependencies | where timestamp >= ago(24h) | where success == false | summarize Failures=count() by cloud_RoleName, type, target, resultCode | order by Failures desc'
            size: 0
            queryType: 0
            resourceType: 'microsoft.insights/components'
          }
        }
        {
          type: 3
          name: 'routes'
          content: {
            version: 'KqlItem/1.0'
            title: 'Safe route-template usage'
            query: 'requests | where timestamp >= ago(24h) | summarize Requests=count(), Failures=countif(success == false), p95=percentile(duration, 95) by cloud_RoleName, name | order by Requests desc'
            size: 0
            queryType: 0
            resourceType: 'microsoft.insights/components'
          }
        }
        {
          type: 3
          name: 'releases'
          content: {
            version: 'KqlItem/1.0'
            title: 'Observed releases'
            query: 'union requests, traces | where timestamp >= ago(30d) | extend ReleaseSha=coalesce(tostring(customDimensions.ReleaseSha), tostring(application_Version)) | where isnotempty(ReleaseSha) | summarize FirstSeen=min(timestamp), LastSeen=max(timestamp), Events=count() by ReleaseSha, cloud_RoleName | order by LastSeen desc'
            size: 0
            queryType: 0
            resourceType: 'microsoft.insights/components'
          }
        }
        {
          type: 9
          name: 'reference-parameter'
          content: {
            version: 'KqlParameterItem/1.0'
            parameters: [
              {
                id: 'reference-id'
                version: 'KqlParameterItem/1.0'
                name: 'ReferenceId'
                label: 'Support reference'
                type: 1
                isRequired: false
                value: ''
              }
            ]
            style: 'pills'
            queryType: 0
            resourceType: 'microsoft.insights/components'
          }
        }
        {
          type: 3
          name: 'reference-results'
          content: {
            version: 'KqlItem/1.0'
            title: 'Correlated operation timeline'
            query: 'let reference = trim(" ", "{ReferenceId}"); let matchingOperations = traces | where timestamp >= ago(30d) | where isnotempty(reference) | where message has reference or tostring(customDimensions.ReferenceId) == reference | distinct operation_Id; union requests, dependencies, exceptions, traces | where operation_Id in (matchingOperations) | project timestamp, itemType, cloud_RoleName, operation_Id, name, resultCode, success, message | order by timestamp asc'
            size: 0
            queryType: 0
            resourceType: 'microsoft.insights/components'
          }
        }
      ]
    })
  }
}

output connectionString string = insights.properties.ConnectionString
output workbookId string = supportWorkbook.id
