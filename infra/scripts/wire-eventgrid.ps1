<#
.SYNOPSIS
  Wires (idempotently) the Event Grid subscription that routes BlobCreated events
  from the 'audio-auto' container to the Logic App auto-transcription workflow.

  Runs as an azd 'postdeploy' hook for the logic-apps service, because the workflow
  callback URL is a runtime secret that only exists once the workflow is deployed.

  Required environment variables (provided by azd from Bicep outputs / azd env):
    AZURE_SUBSCRIPTION_ID
    AZURE_RESOURCE_GROUP
    AZURE_LOGIC_APP_NAME
    AZURE_EVENTGRID_SYSTEM_TOPIC
#>
$ErrorActionPreference = 'Stop'

$sub      = $env:AZURE_SUBSCRIPTION_ID
$rg       = $env:AZURE_RESOURCE_GROUP
$site     = $env:AZURE_LOGIC_APP_NAME
$topic    = $env:AZURE_EVENTGRID_SYSTEM_TOPIC
$workflow = 'transcription'
$trigger  = 'When_Event_Grid_event_received'
$subName  = 'audio-auto-created'

foreach ($pair in @{ AZURE_SUBSCRIPTION_ID = $sub; AZURE_RESOURCE_GROUP = $rg; AZURE_LOGIC_APP_NAME = $site; AZURE_EVENTGRID_SYSTEM_TOPIC = $topic }.GetEnumerator()) {
  if ([string]::IsNullOrWhiteSpace($pair.Value)) { throw "Missing required environment variable: $($pair.Name)" }
}

Write-Host "Fetching workflow callback URL for '$workflow'..."
$callbackUri = "https://management.azure.com/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.Web/sites/$site/hostruntime/runtime/webhooks/workflow/api/management/workflows/$workflow/triggers/$trigger/listCallbackUrl?api-version=2018-11-01"
$callbackUrl = az rest --method post --uri $callbackUri --query value --output tsv
if ([string]::IsNullOrWhiteSpace($callbackUrl)) { throw "Could not retrieve workflow callback URL." }

$body = @{
  properties = @{
    destination = @{
      endpointType = 'WebHook'
      properties   = @{
        endpointUrl                   = $callbackUrl
        maxEventsPerBatch             = 1
        preferredBatchSizeInKilobytes = 64
      }
    }
    filter = @{
      includedEventTypes = @('Microsoft.Storage.BlobCreated')
      subjectBeginsWith  = '/blobServices/default/containers/audio-auto/blobs/'
      subjectEndsWith    = '.mp3'
    }
    eventDeliverySchema = 'EventGridSchema'
    retryPolicy = @{
      maxDeliveryAttempts        = 30
      eventTimeToLiveInMinutes   = 1440
    }
  }
}

$tmp = New-TemporaryFile
try {
  ($body | ConvertTo-Json -Depth 10) | Set-Content -Path $tmp.FullName -Encoding utf8
  $putUri = "https://management.azure.com/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.EventGrid/systemTopics/$topic/eventSubscriptions/$subName`?api-version=2024-06-01-preview"
  Write-Host "Creating/updating Event Grid subscription '$subName' on system topic '$topic'..."
  az rest --method put --uri $putUri --body "@$($tmp.FullName)" --output none
  Write-Host "Event Grid subscription '$subName' wired to workflow '$workflow'."
}
finally {
  Remove-Item $tmp.FullName -Force -ErrorAction SilentlyContinue
}
