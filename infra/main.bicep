targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the azd environment, used to derive the resource group and resource names.')
param environmentName string

@minLength(1)
@description('Primary location for all resources.')
param location string

@description('Optional Entra ID tenant id used to protect the API.')
param azureAdTenantId string = ''

@description('Optional Entra ID application (client) id used to protect the API.')
param azureAdClientId string = ''

@description('Speech recognition language.')
param speechLanguage string = 'en-US'

var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
}

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: '${environmentName}-rg'
  location: location
  tags: tags
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    logAnalyticsName: 'log-${resourceToken}'
    applicationInsightsName: 'appi-${resourceToken}'
  }
}

module identity 'modules/identity.bicep' = {
  name: 'identity'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    identityName: 'id-${resourceToken}'
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    keyVaultName: 'kv-${resourceToken}'
    readerPrincipalIds: [
      identity.outputs.identityPrincipalId
    ]
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    storageAccountName: 'st${resourceToken}'
    contributorPrincipalIds: [
      identity.outputs.identityPrincipalId
    ]
  }
}

module cosmos 'modules/cosmos.bicep' = {
  name: 'cosmos'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    accountName: 'cosmos-${resourceToken}'
    dataContributorPrincipalIds: [
      identity.outputs.identityPrincipalId
    ]
  }
}

module speech 'modules/speech.bicep' = {
  name: 'speech'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    speechAccountName: 'speech-${resourceToken}'
    keyVaultName: keyVault.outputs.keyVaultName
    userPrincipalIds: [
      identity.outputs.identityPrincipalId
    ]
  }
}

module registry 'modules/registry.bicep' = {
  name: 'registry'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    registryName: 'acr${resourceToken}'
    pullPrincipalIds: [
      identity.outputs.identityPrincipalId
    ]
  }
}

module containerAppsEnv 'modules/containerAppsEnv.bicep' = {
  name: 'container-apps-env'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    environmentName: 'cae-${resourceToken}'
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
  }
}

module api 'modules/api.bicep' = {
  name: 'api'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    containerAppName: 'ca-api-${resourceToken}'
    environmentId: containerAppsEnv.outputs.environmentId
    registryLoginServer: registry.outputs.loginServer
    identityId: identity.outputs.identityId
    identityClientId: identity.outputs.identityClientId
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    cosmosEndpoint: cosmos.outputs.endpoint
    cosmosDatabase: cosmos.outputs.databaseName
    cosmosContainer: cosmos.outputs.containerName
    blobEndpoint: storage.outputs.blobEndpoint
    azureAdTenantId: azureAdTenantId
    azureAdClientId: azureAdClientId
  }
}

module frontend 'modules/frontend.bicep' = {
  name: 'frontend'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    containerAppName: 'ca-web-${resourceToken}'
    environmentId: containerAppsEnv.outputs.environmentId
    registryLoginServer: registry.outputs.loginServer
    identityId: identity.outputs.identityId
    identityClientId: identity.outputs.identityClientId
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    apiBaseUrl: api.outputs.apiUri
  }
}

module functions 'modules/functions.bicep' = {
  name: 'functions'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    functionAppName: 'func-${resourceToken}'
    hostingPlanName: 'plan-func-${resourceToken}'
    functionStorageName: 'stfunc${resourceToken}'
    identityId: identity.outputs.identityId
    identityClientId: identity.outputs.identityClientId
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    dataBlobEndpoint: storage.outputs.blobEndpoint
    cosmosEndpoint: cosmos.outputs.endpoint
    cosmosDatabase: cosmos.outputs.databaseName
    cosmosContainer: cosmos.outputs.containerName
    speechEndpoint: speech.outputs.speechEndpoint
    keyVaultUri: keyVault.outputs.keyVaultUri
    speechLanguage: speechLanguage
  }
}

module eventGrid 'modules/eventgrid.bicep' = {
  name: 'event-grid'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    systemTopicName: 'evgt-${resourceToken}'
    storageAccountId: storage.outputs.storageAccountId
    functionAppName: functions.outputs.functionAppName
    functionAppHostName: functions.outputs.functionAppHostName
  }
}

module logicApps 'modules/logicapps.bicep' = {
  name: 'logic-apps'
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    logicAppName: 'logic-${resourceToken}'
    hostingPlanName: 'plan-logic-${resourceToken}'
    logicStorageName: 'stlogic${resourceToken}'
    identityId: identity.outputs.identityId
    identityClientId: identity.outputs.identityClientId
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    dataBlobEndpoint: storage.outputs.blobEndpoint
    apiBaseUrl: api.outputs.apiUri
    speechEndpoint: speech.outputs.speechEndpoint
    keyVaultUri: keyVault.outputs.keyVaultUri
    speechLanguage: speechLanguage
  }
}

output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = subscription().tenantId
output AZURE_RESOURCE_GROUP string = resourceGroup.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.outputs.loginServer
output SERVICE_API_ENDPOINT_URL string = api.outputs.apiUri
output SERVICE_API_NAME string = api.outputs.apiName
output SERVICE_FRONTEND_ENDPOINT_URL string = frontend.outputs.frontendUri
output SERVICE_FRONTEND_NAME string = frontend.outputs.frontendName

// Consumed by the logic-apps postdeploy hook to wire the Event Grid subscription
// (audio-auto -> Logic App auto-transcription workflow) once the workflow callback URL exists.
output AZURE_LOGIC_APP_NAME string = logicApps.outputs.logicAppName
output AZURE_EVENTGRID_SYSTEM_TOPIC string = eventGrid.outputs.systemTopicName
output SERVICE_FUNCTIONS_NAME string = functions.outputs.functionAppName
output SERVICE_LOGIC_APPS_NAME string = logicApps.outputs.logicAppName
output COSMOS_ENDPOINT string = cosmos.outputs.endpoint
output STORAGE_BLOB_ENDPOINT string = storage.outputs.blobEndpoint
output SPEECH_ENDPOINT string = speech.outputs.speechEndpoint
output KEY_VAULT_URI string = keyVault.outputs.keyVaultUri
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString

// Consumed by the logic-apps prepackage hook to render parameters.json with concrete
// values (the workflow runtime does not evaluate @appsetting() inside parameters.json).
output AZURE_MANAGED_IDENTITY_RESOURCE_ID string = identity.outputs.identityId
output SPEECH_LANGUAGE string = speechLanguage
