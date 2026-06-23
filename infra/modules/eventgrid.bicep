@description('Event Grid system topic + subscription routing BlobCreated events on the audio container to the transcription function via the blobs extension webhook (low-latency trigger).')
param location string
param tags object
param systemTopicName string
param storageAccountId string

@description('Name of the Function App hosting the Event Grid blob trigger.')
param functionAppName string

@description('Default host name (e.g. func-xxx.azurewebsites.net) of the Function App.')
param functionAppHostName string

@description('Name of the function that handles the Event Grid blob trigger.')
param functionName string = 'TranscribeFunction'

// The blobs extension system key authorizes Event Grid -> function delivery.
// It only exists once the function code (with an Event Grid blob trigger) has been deployed.
var blobsExtensionKey = listKeys(resourceId('Microsoft.Web/sites/host', functionAppName, 'default'), '2023-12-01').systemKeys.blobs_extension
var webhookUrl = 'https://${functionAppHostName}/runtime/webhooks/blobs?functionName=Host.Functions.${functionName}&code=${blobsExtensionKey}'

resource systemTopic 'Microsoft.EventGrid/systemTopics@2024-06-01-preview' = {
  name: systemTopicName
  location: location
  tags: tags
  properties: {
    source: storageAccountId
    topicType: 'Microsoft.Storage.StorageAccounts'
  }
}

resource blobCreatedSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2024-06-01-preview' = {
  parent: systemTopic
  name: 'audio-mp3-created'
  properties: {
    destination: {
      endpointType: 'WebHook'
      properties: {
        endpointUrl: webhookUrl
        maxEventsPerBatch: 1
        preferredBatchSizeInKilobytes: 64
      }
    }
    filter: {
      includedEventTypes: [
        'Microsoft.Storage.BlobCreated'
      ]
      subjectBeginsWith: '/blobServices/default/containers/audio/blobs/'
      subjectEndsWith: '.mp3'
    }
    eventDeliverySchema: 'EventGridSchema'
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
}

output systemTopicName string = systemTopic.name
