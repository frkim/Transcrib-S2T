#!/bin/sh
# Renders src/logic-apps/parameters.json from parameters.json.template using the
# concrete values provisioned by Bicep (exposed by azd as environment variables).
# The Logic Apps Standard runtime does NOT evaluate @appsetting() inside parameters.json,
# so the workflow parameters must contain literal values at packaging time.
set -e

SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)
SERVICE_DIR=$(cd "$SCRIPT_DIR/../../src/logic-apps" && pwd)
TEMPLATE="$SERVICE_DIR/parameters.json.template"
OUTPUT="$SERVICE_DIR/parameters.json"

for pair in \
  "__SPEECH_ENDPOINT__:$SPEECH_ENDPOINT" \
  "__SPEECH_LANGUAGE__:$SPEECH_LANGUAGE" \
  "__STORAGE_BLOB_ENDPOINT__:$STORAGE_BLOB_ENDPOINT" \
  "__AZURE_MANAGED_IDENTITY_RESOURCE_ID__:$AZURE_MANAGED_IDENTITY_RESOURCE_ID"; do
  value=${pair#*:}
  token=${pair%%:*}
  if [ -z "$value" ]; then
    echo "Missing required environment variable for token '$token'. Run 'azd provision' first." >&2
    exit 1
  fi
done

content=$(cat "$TEMPLATE")
content=${content//__SPEECH_ENDPOINT__/$SPEECH_ENDPOINT}
content=${content//__SPEECH_LANGUAGE__/$SPEECH_LANGUAGE}
content=${content//__STORAGE_BLOB_ENDPOINT__/$STORAGE_BLOB_ENDPOINT}
content=${content//__AZURE_MANAGED_IDENTITY_RESOURCE_ID__/$AZURE_MANAGED_IDENTITY_RESOURCE_ID}
printf '%s' "$content" > "$OUTPUT"
echo "Rendered $OUTPUT"
