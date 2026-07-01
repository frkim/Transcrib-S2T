@description('Logic App Standard hosting the low-code transcription and daily purge workflows.')
param location string
param tags object
param logicAppName string
param hostingPlanName string
param logicStorageName string
param identityId string
param identityClientId string
param applicationInsightsConnectionString string
param dataBlobEndpoint string
param apiBaseUrl string
param speechEndpoint string
param keyVaultUri string
param speechLanguage string = 'fr-FR'

resource logicStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: logicStorageName
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
    name: 'WS1'
    tier: 'WorkflowStandard'
  }
}

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${logicStorage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${logicStorage.listKeys().keys[0].value}'

resource logicApp 'Microsoft.Web/sites@2023-12-01' = {
  name: logicAppName
  location: location
  tags: union(tags, { 'azd-service-name': 'logic-apps' })
  kind: 'functionapp,workflowapp'
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
        { name: 'WEBSITE_CONTENTSHARE', value: toLower(logicAppName) }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'node' }
        { name: 'APP_KIND', value: 'workflowApp' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: applicationInsightsConnectionString }
        { name: 'AZURE_CLIENT_ID', value: identityClientId }
        // Resource ID of the user-assigned identity, consumed by workflow parameters
        // for ManagedServiceIdentity authentication on built-in HTTP actions.
        { name: 'MSI_RESOURCE_ID', value: identityId }
        // Built-in AzureBlob service provider connection (managed identity).
        { name: 'AzureBlob__blobServiceUri', value: dataBlobEndpoint }
        { name: 'AzureBlob__credential', value: 'managedidentity' }
        { name: 'AzureBlob__clientId', value: identityClientId }
        { name: 'Api__BaseUrl', value: apiBaseUrl }
        { name: 'Storage__BlobEndpoint', value: dataBlobEndpoint }
        { name: 'Speech__Endpoint', value: speechEndpoint }
        { name: 'Speech__Key', value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/speech-key)' }
        { name: 'Speech__Language', value: speechLanguage }
      ]
    }
  }
}

output logicAppName string = logicApp.name
output logicAppHostName string = logicApp.properties.defaultHostName
