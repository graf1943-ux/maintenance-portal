[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$AppName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$FrontendOrigin
)

$ErrorActionPreference = 'Stop'

# Accept either the App Service resource name or its default hostname.
# Example: my-api-name.japanwest-01.azurewebsites.net -> my-api-name
if ($AppName.Contains('.')) {
    $AppName = ($AppName -split '\.')[0]
}

try {
    $originUri = [Uri]$FrontendOrigin
    if ($originUri.Scheme -ne 'https' -or
        $originUri.AbsolutePath -ne '/' -or
        $originUri.Query -or
        $originUri.Fragment) {
        throw 'FrontendOrigin must be an HTTPS origin only.'
    }
    $FrontendOrigin = $originUri.GetLeftPart([System.UriPartial]::Authority)
}
catch {
    throw "FrontendOrigin is not a valid HTTPS origin: $FrontendOrigin"
}

$projectFile = Join-Path $PSScriptRoot 'RoadbikeCare.Api/RoadbikeCare.Api.csproj'
$artifactDirectory = Join-Path $PSScriptRoot 'artifacts'
$publishDirectory = Join-Path $artifactDirectory 'publish'
$packagePath = Join-Path $artifactDirectory 'roadbike-care-api.zip'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET 10 SDK is required. Install it from https://dotnet.microsoft.com/download/dotnet/10.0'
}
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Azure CLI is required. Install it from https://learn.microsoft.com/cli/azure/install-azure-cli'
}

$sdks = dotnet --list-sdks
if (-not ($sdks | Where-Object { $_ -match '^10\.' })) {
    throw '.NET 10 SDK was not found. Check the output of dotnet --list-sdks.'
}

az account show --output none
if ($LASTEXITCODE -ne 0) {
    throw 'Not logged in to Azure. Run az login first.'
}

# Verify the resource group and App Service name before publishing.
az webapp show --resource-group $ResourceGroup --name $AppName --output none
if ($LASTEXITCODE -ne 0) {
    throw "App Service '$AppName' was not found in resource group '$ResourceGroup'. Check both values."
}
$defaultHostName = az webapp show --resource-group $ResourceGroup --name $AppName --query defaultHostName --output tsv
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($defaultHostName)) {
    throw 'Could not retrieve the App Service default hostname.'
}
$databaseSettingCount = az webapp config appsettings list --resource-group $ResourceGroup --name $AppName `
    --query "[?name=='ConnectionStrings__RoadbikeCare'] | length(@)" --output tsv
if ($LASTEXITCODE -ne 0) {
    throw 'Could not verify the database connection setting.'
}
if ($databaseSettingCount.Trim() -ne '1') {
    throw 'Configure the ConnectionStrings__RoadbikeCare App Service setting before deploying.'
}
$requiredAuthenticationSettings = @(
    'Authentication__Staff__Authority',
    'Authentication__Staff__Issuer',
    'Authentication__Staff__Audience',
    'Authentication__Customer__Authority',
    'Authentication__Customer__Issuer',
    'Authentication__Customer__Audience'
)
foreach ($settingName in $requiredAuthenticationSettings) {
    $settingCount = az webapp config appsettings list --resource-group $ResourceGroup --name $AppName `
        --query "[?name=='$settingName' && value!=''] | length(@)" --output tsv
    if ($LASTEXITCODE -ne 0) {
        throw "Could not verify the required authentication setting '$settingName'."
    }
    if ($settingCount.Trim() -ne '1') {
        throw "Configure the $settingName App Service setting before deploying."
    }
}

if (Test-Path $publishDirectory) {
    Remove-Item $publishDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null

Write-Host '1/4 Publishing the .NET 10 API...' -ForegroundColor Cyan
dotnet publish $projectFile --configuration Release --output $publishDirectory
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

Write-Host '2/4 Creating the ZIP package...' -ForegroundColor Cyan
if (Test-Path $packagePath) { Remove-Item $packagePath -Force }
Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $packagePath -CompressionLevel Optimal

Write-Host '3/4 Configuring the App Service settings...' -ForegroundColor Cyan
Write-Host 'Skipping runtime configuration. .NET 10 was configured separately.' -ForegroundColor Yellow

az webapp update --resource-group $ResourceGroup --name $AppName --https-only true --output none
if ($LASTEXITCODE -ne 0) { throw 'Failed to enable HTTPS Only.' }

az webapp config appsettings set --resource-group $ResourceGroup --name $AppName --settings `
    "ASPNETCORE_ENVIRONMENT=Production" `
    "WEBSITE_RUN_FROM_PACKAGE=1" `
    "Cors__AllowedOrigins__0=$FrontendOrigin" --output none
if ($LASTEXITCODE -ne 0) { throw 'Failed to configure App Service application settings.' }

Write-Host '4/4 Deploying the ZIP package to App Service...' -ForegroundColor Cyan
az webapp deploy --resource-group $ResourceGroup --name $AppName --src-path $packagePath --type zip
if ($LASTEXITCODE -ne 0) { throw 'ZIP deployment failed.' }

Write-Host ''
Write-Host 'Deployment command completed. Check the API health endpoint:' -ForegroundColor Green
Write-Host "https://$defaultHostName/api/health"
Write-Host "Package: $packagePath"
