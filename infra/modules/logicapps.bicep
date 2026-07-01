@description('Consumption Logic Apps hosting the low-code transcription and daily purge workflows. Consumption billing is pay-per-action, eliminating the fixed WorkflowStandard (WS1) plan cost.')
param location string
param tags object
param transcriptionWorkflowName string
param purgeWorkflowName string
param identityId string
param dataBlobEndpoint string
param apiBaseUrl string
param speechEndpoint string
param speechLanguage string = 'fr-FR'

var workflowIdentity = {
  type: 'UserAssigned'
  userAssignedIdentities: {
    '${identityId}': {}
  }
}

// Low-code transcription pipeline (functionally equivalent to the Pro Code Azure
// Function). All actions use HTTP + Managed Identity, so no managed API
// connections are required.
resource transcriptionWorkflow 'Microsoft.Logic/workflows@2019-05-01' = {
  name: transcriptionWorkflowName
  location: location
  tags: tags
  identity: workflowIdentity
  properties: {
    state: 'Enabled'
    definition: loadJsonContent('../../src/logic-apps/transcription/workflow.json').definition
    parameters: {
      speechEndpoint: { value: speechEndpoint }
      speechLanguage: { value: speechLanguage }
      blobServiceUri: { value: dataBlobEndpoint }
      managedIdentityResourceId: { value: identityId }
    }
  }
}

// Daily purge of audio + transcript blobs older than one day. Blob operations
// use the Blob REST API over HTTP + Managed Identity (the Standard-only
// AzureBlob service-provider connector does not exist in Consumption).
resource purgeWorkflow 'Microsoft.Logic/workflows@2019-05-01' = {
  name: purgeWorkflowName
  location: location
  tags: tags
  identity: workflowIdentity
  properties: {
    state: 'Enabled'
    definition: loadJsonContent('../../src/logic-apps/purge/workflow.json').definition
    parameters: {
      blobServiceUri: { value: dataBlobEndpoint }
      apiBaseUrl: { value: apiBaseUrl }
      managedIdentityResourceId: { value: identityId }
    }
  }
}

output transcriptionWorkflowName string = transcriptionWorkflow.name
output purgeWorkflowName string = purgeWorkflow.name
