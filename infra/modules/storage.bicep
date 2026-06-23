@description('Storage account with the shared audio and transcripts containers.')
param location string
param tags object
param storageAccountName string

@description('Principal ids granted Storage Blob Data Contributor.')
param contributorPrincipalIds array = []

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource audioContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'audio'
  properties: {
    publicAccess: 'None'
  }
}

resource transcriptsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'transcripts'
  properties: {
    publicAccess: 'None'
  }
}

// Storage Blob Data Contributor role.
var blobContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource blobRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in contributorPrincipalIds: {
    name: guid(storageAccount.id, principalId, blobContributorRoleId)
    scope: storageAccount
    properties: {
      principalId: principalId
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', blobContributorRoleId)
      principalType: 'ServicePrincipal'
    }
  }
]

// Storage Queue Data Contributor role (required by the blob trigger for scan/poison queues).
var queueContributorRoleId = '974c5e8b-45b9-4585-a649-bb74284b6c0d'

resource queueRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in contributorPrincipalIds: {
    name: guid(storageAccount.id, principalId, queueContributorRoleId)
    scope: storageAccount
    properties: {
      principalId: principalId
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', queueContributorRoleId)
      principalType: 'ServicePrincipal'
    }
  }
]

output storageAccountName string = storageAccount.name
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
output storageAccountId string = storageAccount.id
