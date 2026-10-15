# Creates the Event Grid webhook subscription (BlobCreated on the audio container -> TranscribeFunction).
# The webhook validation handshake needs the Function code deployed and its host running, so this runs
# after `azd deploy` instead of inside the Bicep provisioning.
$ErrorActionPreference = 'Stop'
$rg = $env:AZURE_RESOURCE_GROUP
$sub = $env:AZURE_SUBSCRIPTION_ID
$subscriptionName = 'audio-mp3-created'
if (-not $rg) { throw 'AZURE_RESOURCE_GROUP is not set.' }

# CI runs azd without an Azure CLI session; the subscription is only needed on first provisioning.
az account show -o none 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Azure CLI is not logged in; skipping Event Grid subscription check.'
    return
}

$topic = az eventgrid system-topic list -g $rg --subscription $sub --query "[0].name" -o tsv
$func = az functionapp list -g $rg --subscription $sub --query "[0].name" -o tsv
if (-not $topic -or -not $func) { throw 'Event Grid system topic or function app not found.' }

$existing = az eventgrid system-topic event-subscription list -g $rg --system-topic-name $topic --subscription $sub --query "[?name=='$subscriptionName'].name" -o tsv
if ($existing) {
    Write-Host "Event subscription $subscriptionName already exists."
    return
}

$funcHost = az resource show --ids "/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.Web/sites/$func" --query properties.defaultHostName -o tsv
$keysUri = "/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.Web/sites/$func/host/default/listKeys?api-version=2023-12-01"

for ($attempt = 1; $attempt -le 6; $attempt++) {
    $key = az rest --method post --uri $keysUri --query systemKeys.blobs_extension -o tsv
    if ($key) {
        $url = "https://$funcHost/runtime/webhooks/blobs?functionName=Host.Functions.TranscribeFunction&code=$key"
        # A JSON body file avoids the '&' in the webhook URL being split by az.cmd on Windows.
        $body = @{
            properties = @{
                destination = @{
                    endpointType = 'WebHook'
                    properties   = @{ endpointUrl = $url; maxEventsPerBatch = 1; preferredBatchSizeInKilobytes = 64 }
                }
                filter = @{
                    includedEventTypes = @('Microsoft.Storage.BlobCreated')
                    subjectBeginsWith  = '/blobServices/default/containers/audio/blobs/'
                    subjectEndsWith    = '.mp3'
                }
                eventDeliverySchema = 'EventGridSchema'
                retryPolicy = @{ maxDeliveryAttempts = 30; eventTimeToLiveInMinutes = 1440 }
            }
        } | ConvertTo-Json -Depth 10
        $bodyFile = Join-Path ([IO.Path]::GetTempPath()) "evgsub-$([guid]::NewGuid()).json"
        Set-Content -Path $bodyFile -Value $body
        try {
            $subUri = "/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.EventGrid/systemTopics/$topic/eventSubscriptions/${subscriptionName}?api-version=2024-06-01-preview"
            az rest --method put --uri $subUri --body "@$bodyFile" -o none
        } finally {
            Remove-Item $bodyFile -Force -ErrorAction SilentlyContinue
        }        if ($LASTEXITCODE -eq 0) {
            Write-Host "Event subscription $subscriptionName created."
            return
        }
    }
    Write-Host "Function host not ready (attempt $attempt/6); retrying in 30s..."
    Start-Sleep -Seconds 30
}
throw "Could not create event subscription $subscriptionName."
