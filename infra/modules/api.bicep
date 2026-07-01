@description('Backend API container app (C#) on Azure Container Apps.')
param location string
param tags object
param containerAppName string
param environmentId string
param registryLoginServer string
param identityId string
param identityClientId string
param applicationInsightsConnectionString string
param cosmosEndpoint string
param cosmosDatabase string
param cosmosContainer string
param blobEndpoint string
param azureAdTenantId string = ''
param azureAdClientId string = ''

@description('Container image. azd overrides this during deployment.')
param containerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: containerAppName
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        corsPolicy: {
          allowedOrigins: [
            '*'
          ]
          allowedMethods: [
            '*'
          ]
          allowedHeaders: [
            '*'
          ]
        }
      }
      registries: [
        {
          server: registryLoginServer
          identity: identityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: containerImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'AZURE_CLIENT_ID', value: identityClientId }
            { name: 'ApplicationInsights__ConnectionString', value: applicationInsightsConnectionString }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: applicationInsightsConnectionString }
            { name: 'Cosmos__Endpoint', value: cosmosEndpoint }
            { name: 'Cosmos__Database', value: cosmosDatabase }
            { name: 'Cosmos__Container', value: cosmosContainer }
            { name: 'Storage__BlobEndpoint', value: blobEndpoint }
            { name: 'AzureAd__Instance', value: environment().authentication.loginEndpoint }
            { name: 'AzureAd__TenantId', value: azureAdTenantId }
            { name: 'AzureAd__ClientId', value: azureAdClientId }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
      }
    }
  }
}

output apiUri string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output apiName string = containerApp.name
