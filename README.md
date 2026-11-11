# ROAD BIKE CARE

ロードバイク年間メンテナンス契約・作業管理モックです。

## 構成

- `frontend/` : Vue 3 + TypeScript + Vite のフロントエンド
- `backend/RoadbikeCare.Api/` : ASP.NET Core 10 Web API

- APIデータ : 起動時にサンプルデータをメモリへロードするインメモリ実装

Vue画面は `frontend/.env` の `VITE_USE_MOCK_DATA` でデータ取得元を切り替えます。`true` の場合はVue内のモックデータを表示しAPI通信を行いません。`false` の場合はAPIのみを使用し、接続に失敗してもVueモックへ自動切替しません。画面上部に現在のデータ取得元を表示します。APIモードでは起動時にAPIから各データを取得し、登録・予定変更・作業開始・完了処理もAPI経由で実行します。

## 起動方法

### 1. API

.NET 10 SDKをインストールしたうえで、プロジェクトルートから実行します。

```powershell
cd backend/RoadbikeCare.Api
dotnet restore
dotnet run
```

APIは `http://localhost:5080` で起動します。

- ヘルスチェック: http://localhost:5080/api/health
- APIリソース一覧: http://localhost:5080/api

### 2. フロントエンド

別のターミナルで `frontend` フォルダに移動して実行します。

```powershell
cd frontend
npm ci
npm run dev
```

Viteの `/api` リクエストは `http://localhost:5080` にプロキシされます。

## 画面とAPIの接続範囲

- 初期表示：顧客、自転車（有効契約・プラン・残回数を含む）、店舗、担当者、プラン、オーバーホール、メンテナンス、イベント・点検、作業履歴をAPIから取得します。
- 年間契約登録：必要に応じて顧客を作成し、自転車を登録してから年間契約を作成します。
- プラン・店舗・担当者：新規登録をAPI経由で行います。
- オーバーホール・メンテナンス：予定登録、予定変更、作業開始、完了をAPI経由で行います。
- イベント・点検：予定登録、予定変更、作業開始、完了をAPI経由で行います。
- 完了時の契約回数消費と履歴追加はバックエンドで処理します。

## Azure App Service へのバックエンドデプロイ（.NET 10）

このプロジェクトの API は .NET 10 LTS を対象にしています。.NET 8 は 2026年11月10日にサポート終了したため、新規デプロイでは .NET 10 を使用します。

### 事前準備

- .NET 10 SDK
- Azure CLI
- Azure サブスクリプションへのログイン権限
- デプロイ先 App Service（Linux / Code / .NET 10）
- フロントエンドの Azure Static Web Apps の Origin（例：`https://<your-site>.azurestaticapps.net`）

### App Service の作成（まだ作成していない場合）

Azure CLI でログインし、リソースグループ・Linux App Service プラン・Web App を作成します。リージョンは既存 Static Web Apps の配置先に近い場所を選んでください。`<...>` は実際の値に置き換えます。

```powershell
az login
az account set --subscription "<サブスクリプションIDまたは名前>"

az group create --name "<リソースグループ名>" --location japaneast
az appservice plan create --name "<App Serviceプラン名>" --resource-group "<リソースグループ名>" --is-linux --sku B1
az webapp create --name "<世界で一意なApp Service名>" --resource-group "<リソースグループ名>" --plan "<App Serviceプラン名>" --runtime "DOTNETCORE:10.0"
```

料金を抑える場合は利用可能なプランを Azure Portal で確認してください。Free/Shared プランには機能・稼働制限があります。継続的な検証には Basic 以上が扱いやすい構成です。

### API のデプロイ

プロジェクトルートで PowerShell を開き、次を実行します。

Windows PowerShell 5.1 でも文字化け・構文エラーにならないよう、デプロイスクリプトは UTF-8 BOM 付きで保存しています。実行ポリシーでブロックされる場合は、現在の PowerShell セッションだけ一時的に許可します。

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
az login
.\backend\deploy-azure.ps1 `
  -ResourceGroup "<リソースグループ名>" `
  -AppName "<App Service名または既定ホスト名>" `
  -FrontendOrigin "https://<your-site>.azurestaticapps.net"
```

AppName には App Service のリソース名（例：`my-api-name`）を指定します。既定ホスト名（例：`my-api-name.japanwest-01.azurewebsites.net`）を指定した場合も、スクリプトが先頭のリソース名を取り出します。スクリプトはデプロイ前にリソースグループ内の App Service の存在を確認し、.NET 10 で Release 発行し、発行物だけを ZIP 化して App Service に配置します。あわせて Linux の .NET 10 ランタイム、`ASPNETCORE_ENVIRONMENT`、`WEBSITE_RUN_FROM_PACKAGE`、CORS 許可 Origin を設定します。ZIP は `backend/artifacts/roadbike-care-api.zip` に残ります。

### デプロイ後の確認

```powershell
Invoke-RestMethod "https://<App Service名>.azurewebsites.net/api/health"
Invoke-RestMethod "https://<App Service名>.azurewebsites.net/api/customers"
```

`/api/health` で `status: ok` が返ることを確認してください。デプロイスクリプトで HTTPS Only も有効化します。

### フロントエンドから API を呼び出す

フロントエンドのビルド時に、次の環境変数を設定します。

```env
VITE_USE_MOCK_DATA=false
VITE_API_BASE_URL=https://<App Service名>.azurewebsites.net/api
```

`VITE_API_BASE_URL` は Vite のビルド時に埋め込まれるため、設定を変更したら Static Web Apps のフロントエンドを再ビルド・再デプロイしてください。Azure Static Web Apps の GitHub Actions を使っている場合は、ワークフローの build job に環境変数として設定します。値は API の URL なので秘密情報ではありません。

CORS は App Service の `Cors__AllowedOrigins__0` に Static Web Apps の Origin（`https://...azurestaticapps.net`）を設定します。独自ドメインも使う場合は `Cors__AllowedOrigins__1` などを追加します。Origin にはパスや末尾の `/` を含めません。ローカル開発用の許可 Origin は `appsettings.Development.json` に分離しており、本番設定には含まれません。

### この段階の制約

- データは API プロセスのメモリ内だけに保持され、再起動・再デプロイで初期データに戻ります。
- 複数インスタンス間でデータは共有されません。スケールアウトは行わないでください。
- 認証・認可が未実装です。URLを知る第三者から API を操作できるため、実顧客情報や本番データを登録しないでください。
- このデプロイは開発・結合テスト用です。本番運用前に PostgreSQL、認証・権限管理、バックアップ、監査ログを実装してください。

## API一覧

| Method | Endpoint | 内容 |
|---|---|---|
| GET / POST | `/api/customers` | 顧客一覧・登録 |
| GET / PUT / DELETE | `/api/customers/{id}` | 顧客詳細・更新・削除 |
| GET / POST | `/api/bikes` | 自転車一覧・登録 |
| GET / PUT / DELETE | `/api/bikes/{id}` | 自転車詳細・更新・削除 |
| GET / POST | `/api/shops` | 店舗一覧・登録 |
| PUT / DELETE | `/api/shops/{id}` | 店舗更新・削除 |
| GET / POST | `/api/staff` | 担当者一覧・登録 |
| PUT / DELETE | `/api/staff/{id}` | 担当者更新・削除 |
| GET / POST | `/api/plans` | プラン一覧・登録 |
| PUT / DELETE | `/api/plans/{id}` | プラン更新・削除 |
| GET / POST | `/api/contracts` | 年間契約一覧・登録 |
| GET | `/api/contracts/{id}` | 年間契約詳細 |
| GET / POST | `/api/overhauls` | オーバーホール一覧・予定登録 |
| GET / POST | `/api/maintenance` | メンテナンス一覧・予定登録 |
| PUT / DELETE | `/api/work/{id}` | 作業予定の更新・削除 |
| POST | `/api/work/{id}/complete` | オーバーホール／メンテナンス完了 |
| GET / POST | `/api/events` | イベント・点検一覧・登録 |
| PUT | `/api/events/{id}` | イベント・点検更新 |
| POST | `/api/events/{id}/complete` | 点検完了 |
| GET | `/api/history` | 作業履歴一覧 |

## 動作確認例（PowerShell）

```powershell
Invoke-RestMethod http://localhost:5080/api/health
Invoke-RestMethod http://localhost:5080/api/customers
Invoke-RestMethod http://localhost:5080/api/bikes
Invoke-RestMethod http://localhost:5080/api/contracts
Invoke-RestMethod http://localhost:5080/api/maintenance
```

## モック段階の制約

- データはAPIプロセスのメモリ内に保持されます。APIを再起動するとサンプルデータに戻ります。
- 認証・権限管理、実メール送信、PostgreSQL永続化は未実装です。
- 複数ユーザーが同時に更新する場合の排他制御は未実装です。
- 作業完了APIは完了状態・履歴・契約回数をまとめて更新します。モック実装のため、PostgreSQL移行時にはDBトランザクションで実装します。
- サンプルデータは画面モックと同じ顧客・自転車・契約・予定を中心に用意しています。

## フロントエンドの環境設定

ローカル開発では設定不要です。`frontend/vite.config.ts` のViteが `/api` を `http://localhost:5080` に転送します。フロントエンドとAPIを別ホストへ配置する場合は、`frontend/.env` に `VITE_API_BASE_URL` を設定し、APIのCORS許可オリジンも配置先に合わせて変更してください。

## PostgreSQL移行時の方針

APIのルート定義と業務処理を分離し、`MockDataStore` を Repository / Service + PostgreSQL実装へ置き換えます。APIのURLとJSON形式は可能な限り維持し、フロントエンドへの影響を抑えます。


## データ取得元の切り替え

`frontend` フォルダ内に `.env` を作成し、次のいずれかを設定してください。設定変更後は `npm run dev` を再起動してください。

### Vueモックデータを使用

```env
VITE_USE_MOCK_DATA=true
```

Vue内のサンプルデータを表示します。API通信は行わず、登録・更新操作は無効です（参照・画面レイアウト確認用）。

### バックエンドAPIを使用

```env
VITE_USE_MOCK_DATA=false
```

バックエンドAPIから取得したデータだけを表示します。API接続に失敗した場合はエラーを表示し、Vueモックデータへフォールバックしません。

PowerShellでは、プロジェクトルートから `cd frontend` で移動した後、`Copy-Item .env.mock.example .env` または `Copy-Item .env.api.example .env` で切り替えられます。設定を変更したらViteを再起動してください。

## 顧客詳細からの作業登録

顧客・自転車・契約管理画面で顧客名をクリックすると、顧客詳細モーダルの各自転車カードからオーバーホール・メンテナンス・点検を直接登録できます。選択した自転車は登録モーダルにあらかじめ設定されるため、顧客・自転車を再選択する必要はありません。登録モーダルは顧客詳細モーダルを閉じてから単独表示し、閉じた場合は該当する管理画面に留まります。

Vueモックデータモードでは、オーバーホール・メンテナンス・点検の登録内容を画面内データに反映します。APIモードでは既存のREST APIを利用します。

## 年間プランの予定登録上限

オーバーホール・メンテナンス・点検サポートは、自転車ごとの年間契約枠を共有せず、それぞれ独立して上限を管理します。新しい予定を登録するときは、次の合計がプランの上限回数を超えないようにします。

- 完了済み回数（契約で消費済みの回数）
- 完了・キャンセル以外の登録済み予定回数（予定枠として確保中の回数）

予定の登録だけでは契約の消費回数は増えません。作業完了時に予定枠から実績へ移り、契約回数を1回消費します。キャンセルされた予定は新規登録枠を解放します。フロントエンドだけでなくAPI側でも上限を検証します。


## コード整形

フロントエンドの整形は `frontend` フォルダで実行します。

```powershell
cd frontend
npm run format
npm run format:check
```

バックエンドの整形は `frontend` フォルダから次のスクリプトで実行できます（ルートから実行する場合は `dotnet format backend/RoadbikeCare.Api/RoadbikeCare.Api.csproj`）。

```powershell
npm run format:backend
npm run format:backend:check
```
