@description('Frontend (Next.js) container app on Azure Container Apps.')
param location string
param tags object
param containerAppName string
param environmentId string
param registryLoginServer string
param identityId string
param identityClientId string
param applicationInsightsConnectionString string
param apiBaseUrl string

@description('Container image. azd overrides this during deployment.')
param containerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: containerAppName
  location: location
  tags: union(tags, { 'azd-service-name': 'frontend' })
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
        targetPort: 3000
        transport: 'auto'
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
          name: 'frontend'
          image: containerImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'AZURE_CLIENT_ID', value: identityClientId }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: applicationInsightsConnectionString }
            { name: 'NEXT_PUBLIC_API_BASE_URL', value: apiBaseUrl }
            { name: 'PORT', value: '3000' }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

output frontendUri string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output frontendName string = containerApp.name
