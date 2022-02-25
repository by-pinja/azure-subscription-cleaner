@description('The name of the function app that you wish to create.')
param appName string = resourceGroup().name

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Environment type (Development, Production)')
@allowed([
  'Development'
  'Production'
])
param environment string

@description('Bearer token that is used for slack messages')
@secure()
param slackBearerToken string

@description('Channel for slack messages.')
param slackChannel string

@description('If true, only simulation is done.')
param simulate bool

@description('Schedule for cleanup (CRON expression). See https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer?tabs=csharp#ncrontab-expressions')
param cleanupSchedule string

var functionAppName_var = appName
var hostingPlanName_var = appName
var applicationInsightsName_var = appName
var storageAccountName_var = uniqueString(resourceGroup().id)
var storageAccountid = '${resourceGroup().id}/providers/Microsoft.Storage/storageAccounts/${storageAccountName_var}'

resource storageAccountName 'Microsoft.Storage/storageAccounts@2018-07-01' = {
  name: storageAccountName_var
  location: location
  kind: 'Storage'
  sku: {
    name: 'Standard_LRS'
  }
  tags: {
    displayName: 'Storage for function app'
    environment: environment
  }
}

resource hostingPlanName 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: hostingPlanName_var
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    capacity: 1
  }
  tags: {
    displayName: 'Server for function app'
    environment: environment
  }
}

resource functionAppName 'Microsoft.Web/sites@2018-02-01' = {
  kind: 'functionapp'
  name: functionAppName_var
  location: location
  tags: {
    displayName: 'Function app'
    environment: environment
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlanName.id
    siteConfig: {
      defaultDocuments: []
      phpVersion: ''
      use32BitWorkerProcess: true
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName_var};AccountKey=${listKeys(storageAccountid, '2015-05-01-preview').key1}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName_var};AccountKey=${listKeys(storageAccountid, '2015-05-01-preview').key1}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName_var)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: reference(applicationInsightsName.id, '2015-05-01').InstrumentationKey
        }
        {
          name: 'SlackClientSettings:BearerToken'
          value: slackBearerToken
        }
        {
          name: 'CleanupConfiguration:SlackChannel'
          value: slackChannel
        }
        {
          name: 'CleanupConfiguration:Simulate'
          value: simulate ? 'True' : 'False'
        }
        {
          name: 'CleanupSchedule'
          value: cleanupSchedule
        }
      ]
    }
  }
  dependsOn: [
    storageAccountName
  ]
}

resource workspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: resourceGroup().name
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource applicationInsightsName 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName_var
  location: location
  kind: 'web'
  tags: {
    'hidden-link:${resourceGroup().id}/providers/Microsoft.Web/sites/${applicationInsightsName_var}': 'Resource'
    displayName: 'Application insights'
    environment: environment
  }
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}
