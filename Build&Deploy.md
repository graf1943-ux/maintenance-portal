# ビルド・デプロイ

## Azure Static Web Apps（フロントエンド）

プロジェクトルートで実行します。

```powershell
$env:SWA_CLI_DEPLOYMENT_TOKEN = az staticwebapp secrets list --name "maintenance-portal-vue" --resource-group "maintenance-portal-vue" --query "properties.apiKey" --output tsv

npm install -g @azure/static-web-apps-cli
swa --version

cd frontend
$env:VITE_API_BASE_URL = "https://maintenance-portal-vue-backend-b6caekfvgeaegwhx.japanwest-01.azurewebsites.net/api"
$env:VITE_USE_MOCK_DATA = "false"
npm ci
npm run build
swa deploy ./dist --env production --deployment-token $env:SWA_CLI_DEPLOYMENT_TOKEN
Remove-Item Env:VITE_API_BASE_URL
Remove-Item Env:VITE_USE_MOCK_DATA
```

## Azure App Service（バックエンド）

デプロイ前にAzure App Serviceのアプリ設定 `ConnectionStrings__RoadbikeCare` をKey Vault参照として設定し、Azure SQL Databaseへ到達できる環境からSQL Server用EF Coreマイグレーションを適用してください。デプロイスクリプトは接続文字列設定がない場合に中止します。

プロジェクトルートに戻って実行します。

```powershell
cd ..
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

.\\backend\\deploy-azure.ps1 `
  -ResourceGroup "maintenance-portal-vue" `
  -AppName "maintenance-portal-vue-backend" `
  -FrontendOrigin "https://blue-smoke-0a0af5d00.3.azurestaticapps.net"
```

## DB マイグレーション

```powershell
$rg = "maintenance-portal-vue"
$app = "maintenance-portal-vue-backend"

$conn = az webapp config appsettings list `
  --resource-group $rg `
  --name $app `
  --query "[?name=='ConnectionStrings__RoadbikeCare'].value | [0]" `
  --output tsv

if ([string]::IsNullOrWhiteSpace($conn) -or $conn -eq "None") {
  throw "ConnectionStrings__RoadbikeCare が見つかりません。"
}

$env:ConnectionStrings__RoadbikeCare = $conn
Remove-Variable conn
```powershell


dotnet ef database update --project backend/RoadbikeCare.Api/RoadbikeCare.Api.csproj --startup-project backend/RoadbikeCare.Api/RoadbikeCare.Api.csproj

マイグレーション後に削除

```powershell
Remove-Item Env:ConnectionStrings__RoadbikeCare
```powershell