@description('Azure Functions (.NET isolated) pro-code transcription pipeline.')
param location string
param tags object
param functionAppName string
param hostingPlanName string
param functionStorageName string
param identityId string
param identityClientId string
param applicationInsightsConnectionString string
param dataBlobEndpoint string
param cosmosEndpoint string
param cosmosDatabase string
param cosmosContainer string
param speechEndpoint string
param keyVaultUri string
param speechLanguage string = 'en-US'

// Dedicated storage account for the Functions runtime.
resource functionStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: functionStorageName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlanName
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
}

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${functionStorage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${functionStorage.listKeys().keys[0].value}'

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  tags: union(tags, { 'azd-service-name': 'functions' })
  kind: 'functionapp'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    keyVaultReferenceIdentity: identityId
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'AzureWebJobsStorage', value: storageConnectionString }
        { name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING', value: storageConnectionString }
        { name: 'WEBSITE_CONTENTSHARE', value: toLower(functionAppName) }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: applicationInsightsConnectionString }
        { name: 'AZURE_CLIENT_ID', value: identityClientId }
        // Identity-based blob trigger connection for the shared data storage.
        { name: 'AudioStorage__blobServiceUri', value: dataBlobEndpoint }
        { name: 'AudioStorage__credential', value: 'managedidentity' }
        { name: 'AudioStorage__clientId', value: identityClientId }
        { name: 'Cosmos__Endpoint', value: cosmosEndpoint }
        { name: 'Cosmos__Database', value: cosmosDatabase }
        { name: 'Cosmos__Container', value: cosmosContainer }
        { name: 'Storage__BlobEndpoint', value: dataBlobEndpoint }
        { name: 'Speech__Endpoint', value: speechEndpoint }
        { name: 'Speech__Key', value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/speech-key)' }
        { name: 'Speech__Language', value: speechLanguage }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output functionAppHostName string = functionApp.properties.defaultHostName
