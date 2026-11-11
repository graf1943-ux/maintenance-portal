# ビルド・デプロイ

## Azure Static Web Apps（フロントエンド）

プロジェクトルートで実行します。

```powershell
$env:SWA_CLI_DEPLOYMENT_TOKEN = az staticwebapp secrets list --name "maintenance-portal-vue" --resource-group "maintenance-portal-vue" --query "properties.apiKey" --output tsv

npm install -g @azure/static-web-apps-cli
swa --version

cd frontend
npm ci
npm run build
swa deploy ./dist --env production --deployment-token $env:SWA_CLI_DEPLOYMENT_TOKEN
```

## Azure App Service（バックエンド）

プロジェクトルートに戻って実行します。

```powershell
cd ..
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

.\\backend\\deploy-azure.ps1 `
  -ResourceGroup "maintenance-portal-vue" `
  -AppName "maintenance-portal-vue-backend" `
  -FrontendOrigin "https://blue-smoke-0a0af5d00.3.azurestaticapps.net"
```
