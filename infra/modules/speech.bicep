@description('Azure AI Speech (Cognitive Services) resource with diarization support, fronted by Azure AI Foundry conventions.')
param location string
param tags object
param speechAccountName string
param keyVaultName string

@description('Principal ids granted Cognitive Services User.')
param userPrincipalIds array = []

resource speech 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: speechAccountName
  location: location
  tags: tags
  kind: 'SpeechServices'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: speechAccountName
    publicNetworkAccess: 'Enabled'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Persist the Speech key as a Key Vault secret (never exposed in code/IaC outputs).
resource speechKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'speech-key'
  properties: {
    value: speech.listKeys().key1
  }
}

// Cognitive Services User role.
var cognitiveUserRoleId = 'a97b65f3-24c7-4388-baec-2e87135dc908'

resource cognitiveUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for principalId in userPrincipalIds: {
    name: guid(speech.id, principalId, cognitiveUserRoleId)
    scope: speech
    properties: {
      principalId: principalId
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveUserRoleId)
      principalType: 'ServicePrincipal'
    }
  }
]

output speechAccountName string = speech.name
output speechEndpoint string = speech.properties.endpoint
output speechKeySecretUri string = speechKeySecret.properties.secretUri
