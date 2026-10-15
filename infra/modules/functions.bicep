@description('Azure Functions (.NET isolated) pro-code transcription pipeline.')
param location string
param tags object
param functionAppName string
param hostingPlanName string
param functionStorageName string
param identityId string
param identityClientId string
param identityPrincipalId string
param applicationInsightsConnectionString string
param dataBlobEndpoint string
param cosmosEndpoint string
param cosmosDatabase string
param cosmosContainer string
param speechEndpoint string
param speechLanguage string = 'fr-FR'

@description('Speech transcription engine: Fast (synchronous) or Batch (asynchronous, cheaper for large volumes).')
@allowed(['Fast', 'Batch'])
param speechMode string = 'Fast'

// Always-ready instances kept warm to eliminate cold starts on the blob
// (Event Grid) trigger. 0 = pure pay-per-use (cold starts); >=1 = warm baseline.
@description('Number of always-ready instances for the blob trigger scale group.')
param alwaysReadyInstanceCount int = 1

// Dedicated storage account for the Functions runtime and deployment package.
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
    allowSharedKeyAccess: false
    publicNetworkAccess: 'Enabled'
  }
}

resource functionStorageBlob 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: functionStorage
  name: 'default'
}

// Flex Consumption runs the app from a deployment package stored in this container.
resource deploymentContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: functionStorageBlob
  name: 'deployment'
  properties: {
    publicAccess: 'None'
  }
}

// Flex Consumption plan (Linux). Enables always-ready instances to mitigate cold starts.
resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlanName
  location: location
  tags: tags
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

// Shared-key access is disabled on the storage account: the Functions host and the
// deployment package use the user-assigned managed identity.
var functionStorageRoleIds = [
  'b7e6dc6d-f1e8-4753-8033-0f276bb0955b' // Storage Blob Data Owner
  '974c5e8b-45b9-4653-ba55-5f855dd0fb88' // Storage Queue Data Contributor
  '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3' // Storage Table Data Contributor
]

resource functionStorageRoles 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for roleId in functionStorageRoleIds: {
    name: guid(functionStorage.id, identityPrincipalId, roleId)
    scope: functionStorage
    properties: {
      principalId: identityPrincipalId
      principalType: 'ServicePrincipal'
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleId)
    }
  }
]

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  dependsOn: [functionStorageRoles]
  location: location
  tags: union(tags, { 'azd-service-name': 'functions' })
  kind: 'functionapp,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${functionStorage.properties.primaryEndpoints.blob}${deploymentContainer.name}'
          authentication: {
            type: 'UserAssignedIdentity'
            userAssignedIdentityResourceId: identityId
          }
        }
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
      scaleAndConcurrency: {
        // 'blob' is the per-function scale group for Event Grid blob triggers.
        alwaysReady: [
          {
            name: 'blob'
            instanceCount: alwaysReadyInstanceCount
          }
        ]
        instanceMemoryMB: 2048
        maximumInstanceCount: 40
      }
    }
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'AzureWebJobsStorage__accountName', value: functionStorage.name }
        { name: 'AzureWebJobsStorage__credential', value: 'managedidentity' }
        { name: 'AzureWebJobsStorage__clientId', value: identityClientId }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: applicationInsightsConnectionString }
        { name: 'AZURE_CLIENT_ID', value: identityClientId }
        // Identity-based blob trigger connection for the shared data storage.
        { name: 'AudioStorage__blobServiceUri', value: dataBlobEndpoint }
        { name: 'AudioStorage__queueServiceUri', value: replace(dataBlobEndpoint, '.blob.', '.queue.') }
        { name: 'AudioStorage__credential', value: 'managedidentity' }
        { name: 'AudioStorage__clientId', value: identityClientId }
        { name: 'Cosmos__Endpoint', value: cosmosEndpoint }
        { name: 'Cosmos__Database', value: cosmosDatabase }
        { name: 'Cosmos__Container', value: cosmosContainer }
        { name: 'Storage__BlobEndpoint', value: dataBlobEndpoint }
        { name: 'Speech__Endpoint', value: speechEndpoint }
        { name: 'Speech__Language', value: speechLanguage }
        { name: 'Speech__Mode', value: speechMode }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output functionAppHostName string = functionApp.properties.defaultHostName
output functionAppId string = functionApp.id
