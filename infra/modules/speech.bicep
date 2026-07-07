@description('Azure AI Speech (Cognitive Services) resource with diarization support. Accessed via Microsoft Entra ID (Managed Identity) — no keys are provisioned or stored.')
param location string
param tags object
param speechAccountName string

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
    // Disable local (key) authentication — only Entra ID tokens are accepted.
    disableLocalAuth: true
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
