# Subscriptions with a governance policy force publicNetworkAccess=Disabled on Storage and Cosmos DB
# every time an ARM template re-applies them. The apps reach these services over public endpoints
# (with Entra ID auth only), so re-enable access after each provision, then restart the Function host.
$ErrorActionPreference = 'Stop'
$rg = $env:AZURE_RESOURCE_GROUP
$sub = $env:AZURE_SUBSCRIPTION_ID
if (-not $rg) { throw 'AZURE_RESOURCE_GROUP is not set.' }

foreach ($name in (az storage account list -g $rg --subscription $sub --query "[?publicNetworkAccess!='Enabled'].name" -o tsv)) {
    Write-Host "Enabling public network access on storage account $name"
    az storage account update -n $name -g $rg --subscription $sub --public-network-access Enabled -o none
    if ($LASTEXITCODE -ne 0) { throw "Failed to update storage account $name" }
}

foreach ($name in (az cosmosdb list -g $rg --subscription $sub --query "[?publicNetworkAccess!='Enabled'].name" -o tsv)) {
    Write-Host "Enabling public network access on Cosmos DB account $name"
    az cosmosdb update -n $name -g $rg --subscription $sub --public-network-access ENABLED -o none
    if ($LASTEXITCODE -ne 0) { throw "Failed to update Cosmos DB account $name" }
}

foreach ($name in (az functionapp list -g $rg --subscription $sub --query "[].name" -o tsv)) {
    Write-Host "Restarting function app $name"
    az functionapp restart -n $name -g $rg --subscription $sub -o none
}
