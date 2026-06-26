# Renders src/logic-apps/parameters.json from parameters.json.template using the
# concrete values provisioned by Bicep (exposed by azd as environment variables).
# The Logic Apps Standard runtime does NOT evaluate @appsetting() inside parameters.json,
# so the workflow parameters must contain literal values at packaging time.
$ErrorActionPreference = 'Stop'

$serviceDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$serviceDir = Resolve-Path (Join-Path $serviceDir '..' '..' 'src' 'logic-apps')
$template = Join-Path $serviceDir 'parameters.json.template'
$output = Join-Path $serviceDir 'parameters.json'

$map = @{
  '__SPEECH_ENDPOINT__'                  = $env:SPEECH_ENDPOINT
  '__SPEECH_LANGUAGE__'                  = $env:SPEECH_LANGUAGE
  '__STORAGE_BLOB_ENDPOINT__'            = $env:STORAGE_BLOB_ENDPOINT
  '__AZURE_MANAGED_IDENTITY_RESOURCE_ID__' = $env:AZURE_MANAGED_IDENTITY_RESOURCE_ID
}

foreach ($key in $map.Keys) {
  if ([string]::IsNullOrWhiteSpace($map[$key])) {
    throw "Missing required environment variable for token '$key'. Run 'azd provision' first."
  }
}

$content = Get-Content -Raw -Path $template
foreach ($key in $map.Keys) {
  $content = $content.Replace($key, $map[$key])
}
Set-Content -Path $output -Value $content -NoNewline -Encoding utf8
Write-Output "Rendered $output"
