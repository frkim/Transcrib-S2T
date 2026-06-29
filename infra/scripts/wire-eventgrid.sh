#!/bin/sh
# Wires (idempotently) the Event Grid subscription that routes BlobCreated events
# from the 'audio-auto' container to the Logic App auto-transcription workflow.
#
# Runs as an azd 'postdeploy' hook for the logic-apps service, because the workflow
# callback URL is a runtime secret that only exists once the workflow is deployed.
#
# Required environment variables (provided by azd):
#   AZURE_SUBSCRIPTION_ID, AZURE_RESOURCE_GROUP, AZURE_LOGIC_APP_NAME, AZURE_EVENTGRID_SYSTEM_TOPIC
set -eu

: "${AZURE_SUBSCRIPTION_ID:?Missing AZURE_SUBSCRIPTION_ID}"
: "${AZURE_RESOURCE_GROUP:?Missing AZURE_RESOURCE_GROUP}"
: "${AZURE_LOGIC_APP_NAME:?Missing AZURE_LOGIC_APP_NAME}"
: "${AZURE_EVENTGRID_SYSTEM_TOPIC:?Missing AZURE_EVENTGRID_SYSTEM_TOPIC}"

SUB="$AZURE_SUBSCRIPTION_ID"
RG="$AZURE_RESOURCE_GROUP"
SITE="$AZURE_LOGIC_APP_NAME"
TOPIC="$AZURE_EVENTGRID_SYSTEM_TOPIC"
WORKFLOW="transcription"
TRIGGER="When_Event_Grid_event_received"
SUB_NAME="audio-auto-created"

echo "Fetching workflow callback URL for '$WORKFLOW'..."
CALLBACK_URL=$(az rest --method post \
  --uri "https://management.azure.com/subscriptions/$SUB/resourceGroups/$RG/providers/Microsoft.Web/sites/$SITE/hostruntime/runtime/webhooks/workflow/api/management/workflows/$WORKFLOW/triggers/$TRIGGER/listCallbackUrl?api-version=2018-11-01" \
  --query value --output tsv)
[ -n "$CALLBACK_URL" ] || { echo "Could not retrieve workflow callback URL." >&2; exit 1; }

TMP=$(mktemp)
trap 'rm -f "$TMP"' EXIT
cat > "$TMP" <<JSON
{
  "properties": {
    "destination": {
      "endpointType": "WebHook",
      "properties": {
        "endpointUrl": "$CALLBACK_URL",
        "maxEventsPerBatch": 1,
        "preferredBatchSizeInKilobytes": 64
      }
    },
    "filter": {
      "includedEventTypes": ["Microsoft.Storage.BlobCreated"],
      "subjectBeginsWith": "/blobServices/default/containers/audio-auto/blobs/",
      "subjectEndsWith": ".mp3"
    },
    "eventDeliverySchema": "EventGridSchema",
    "retryPolicy": {
      "maxDeliveryAttempts": 30,
      "eventTimeToLiveInMinutes": 1440
    }
  }
}
JSON

echo "Creating/updating Event Grid subscription '$SUB_NAME' on system topic '$TOPIC'..."
az rest --method put \
  --uri "https://management.azure.com/subscriptions/$SUB/resourceGroups/$RG/providers/Microsoft.EventGrid/systemTopics/$TOPIC/eventSubscriptions/$SUB_NAME?api-version=2024-06-01-preview" \
  --body "@$TMP" --output none

echo "Event Grid subscription '$SUB_NAME' wired to workflow '$WORKFLOW'."
