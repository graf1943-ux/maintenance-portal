# ROAD BIKE CARE

ロードバイク年間メンテナンス契約・作業管理アプリです。

## 構成

- `frontend/` : Vue 3 + TypeScript + Vite のフロントエンド
- `backend/RoadbikeCare.Api/` : ASP.NET Core 10 Web API

- APIデータ : 開発時はサンプルデータを使うインメモリDB、本番はAzure SQL Database

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
Development環境で接続文字列を設定しない場合はEF Core InMemoryへサンプルデータを投入します。再起動すると初期状態に戻ります。DevelopmentでAzure SQL接続を設定した場合はマイグレーションのみを適用し、サンプルデータは投入しません。

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
- Azure SQL Database と接続可能なネットワーク
- フロントエンドの Azure Static Web Apps の Origin（例：`https://<your-site>.azurestaticapps.net`）

### Azure SQL Databaseの準備

低頻度・断続的な利用なら、リージョンとワークロード要件が合う場合にAzure SQL DatabaseのGeneral Purpose Serverlessを候補にできます。最小vCore数を抑え、自動一時停止を有効にすると未使用時のコンピューティング費を抑えられますが、再開時の待ち時間が発生し、ストレージ・バックアップ等の費用は別途かかります。常時アクセスされる場合はプロビジョニング済みコンピューティングの方が適することがあります。料金はリージョン、冗長性、ストレージ、バックアップ保持期間を含めてAzure料金計算ツールで比較してください。

Azure SQL logical serverとdatabaseを作成し、アプリとマイグレーション実行端末から接続できるようネットワークとファイアウォールを設定します。コストを抑える場合でも、SQL Serverへの公開アクセスを無制限に許可しないでください。

アプリの本番起動にはAzure SQL用の `ConnectionStrings__RoadbikeCare` が必須です。SQL接続文字列はソース管理せず、App ServiceのKey Vault参照として登録してください。App ServiceのマネージドIDにKey Vault Secrets Userロールを付与したうえで、アプリ設定を登録します。接続文字列はAzure SQL Databaseのサーバー名・DB名・認証方式に合わせて作成し、暗号化を有効にしてください。

```powershell
az webapp config appsettings set --resource-group "<resource-group>" --name "<app-name>" --settings `
  "ConnectionStrings__RoadbikeCare=@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/RoadbikeCareConnection/)"
```

Azure SQLへ到達できる管理端末から、初回デプロイ前にSQL Server用マイグレーションを適用します。次の例はKey Vaultから値をプロセス環境へ読み込み、コマンド出力には表示しません。

```powershell
$env:ConnectionStrings__RoadbikeCare = az keyvault secret show --vault-name "<vault>" --name "RoadbikeCareConnection" --query value --output tsv
dotnet tool restore
dotnet ef database update `
  --project backend/RoadbikeCare.Api/RoadbikeCare.Api.csproj `
  --startup-project backend/RoadbikeCare.Api/RoadbikeCare.Api.csproj
Remove-Item Env:ConnectionStrings__RoadbikeCare
```

API起動時に本番スキーマを自動変更することはありません。デプロイスクリプトは、`ConnectionStrings__RoadbikeCare` 設定がない場合にデプロイを中止します。PostgreSQL用マイグレーションおよび既存PostgreSQLデータベースからの自動変換は行いません。

### Entra ID認証の設定

APIは顧客用Microsoft Entra External IDとスタッフ用の社内Entra IDを別々に検証します。APIの保護対象ルートは認証必須で、`/`、`/api`、`/api/health` のみ匿名アクセス可能です。APIアプリ登録で委任スコープを公開し、顧客・スタッフそれぞれのSPAアプリ登録に許可してください。APIが受け取るAccess Tokenの `aud` をAudience、メタデータ取得に使うURLをAuthority、Access Tokenの `iss` と完全一致する値をIssuerに設定します。

各SPAアプリ登録の認証設定には、リダイレクトURIを追加してください。ローカルは `http://localhost:5173`、本番は `https://<フロントエンドのホスト名>` です。顧客用・スタッフ用の両方に、それぞれ利用する環境のURIを登録します。ローカルで5173番ポートが使用中の場合、Viteは別ポートへ切り替えず起動エラーになります。

スタッフ用APIアプリ登録には `Staff` と `StaffAdmin` のアプリロールを定義します。スタッフには `Staff`、顧客ID紐付けとスタッフ名簿の管理担当者には `StaffAdmin` を割り当ててください。顧客用External ID Authorityで検証されたトークンはAPIが `Customer` として扱うため、顧客へのアプリロール割り当ては不要です。スタッフのロールはAccess Tokenの `roles` クレームから判定します。

App Serviceには次の6設定を登録します。Authority、Issuer、Audienceは実際に発行されたAPI Access TokenとOIDCメタデータに合わせてください。Issuerは末尾の `/` を含め、トークンの `iss` と完全一致させます。

```powershell
az webapp config appsettings set --resource-group "<resource-group>" --name "<app-name>" --settings `
  "Authentication__Staff__Authority=<staff-authority>" `
  "Authentication__Staff__Issuer=<exact-staff-token-iss>" `
  "Authentication__Staff__Audience=<staff-token-aud>" `
  "Authentication__Customer__Authority=<customer-authority>" `
  "Authentication__Customer__Issuer=<exact-customer-token-iss>" `
  "Authentication__Customer__Audience=<customer-token-aud>"
```

ローカルでAPIモードを使う場合も同じ6設定をAPIプロセス環境へ渡し、フロントエンドのVite環境変数も設定してください。Development環境では設定不足での起動は許可しますが、正しいBearer Token検証には実値が必要です。VueモックモードはEntra設定なしで利用できます。

フロントエンドのStatic Web Appsビルドには、顧客・スタッフ用それぞれのSPA Client ID、Authority、API scopeを `VITE_ENTRA_CUSTOMER_CLIENT_ID`、`VITE_ENTRA_CUSTOMER_AUTHORITY`、`VITE_ENTRA_CUSTOMER_API_SCOPE`、`VITE_ENTRA_STAFF_CLIENT_ID`、`VITE_ENTRA_STAFF_AUTHORITY`、`VITE_ENTRA_STAFF_API_SCOPE` として登録します。External IDでknown authorityが必要な構成では `VITE_ENTRA_CUSTOMER_KNOWN_AUTHORITY` も設定してください。これらはビルド時に公開JavaScriptへ含まれる値であり、クライアントシークレットは設定しないでください。

DBマイグレーション適用後、管理者権限のAccess Tokenを使い、顧客レコードとExternal IDの `tid`（Tenant ID）・`oid`（Object ID）を `/api/customers/{id}/identity` のPUTで紐付けます。メールアドレスは変更可能なため、本人識別キーとして使用しません。1つのExternal IDは1顧客にのみ紐付けられます。未紐付け顧客はサインインできても `/api/me` が403を返します。

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

### GitHub Actions からの API 自動デプロイ

`.github/workflows/backend-ci.yml` はPull Request時にバックエンドのビルドとテストを実行し、`main`へのpush時はそれらが成功した後にApp Serviceへ自動デプロイします。Azure認証にはOIDCを使い、長期パスワードやPublish ProfileはGitHubへ登録しません。

最初にAzureのアプリ登録へGitHub Actions用のFederated credentialを追加します。Issuerは `https://token.actions.githubusercontent.com`、Audienceは `api://AzureADTokenExchange`、Subjectは `repo:<GitHub所有者>/<リポジトリ名>:ref:refs/heads/main` です。このアプリ登録に対応するサービスプリンシパルへ、対象App Serviceのスコープで `Website Contributor` ロールを割り当てます。

GitHubリポジトリの **Settings > Secrets and variables > Actions > Variables** に次の変数を登録します。

| Variable | 値 |
|---|---|
| `AZURE_CLIENT_ID` | Azureアプリ登録のApplication (client) ID |
| `AZURE_TENANT_ID` | Microsoft Entra tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_RESOURCE_GROUP` | App Serviceがあるリソースグループ名 |
| `AZURE_WEBAPP_NAME` | App Service名 |
| `AZURE_FRONTEND_ORIGIN` | Static Web AppsのOrigin。例：`https://<site>.azurestaticapps.net` |

デプロイ前にApp Serviceへ `ConnectionStrings__RoadbikeCare`（Key Vault参照）と6つの `Authentication__Staff__*` / `Authentication__Customer__*` 設定を登録してください。App Serviceの.NET 10ランタイム設定とAzure SQLへのネットワーク接続も必要です。ワークフローは既存の `backend/deploy-azure.ps1` を実行し、設定を検査してからRelease発行・デプロイとCORS設定を行います。データベースマイグレーションは自動実行しないため、必要な場合は別途適用してください。

### 手動での API デプロイ

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
```

`/api/health` で `status: ok` が返ることを確認してください。保護されたAPIの確認はSPAからサインインして行います。Access Tokenを別の承認済みクライアントで取得した場合は、そのBearer Tokenを `/api/me` などへ送って確認してください。デプロイスクリプトで HTTPS Only も有効化します。

Swagger UIはDevelopment環境で `https://<APIホスト>/swagger`（ローカルは `http://localhost:5080/swagger`）から利用できます。Swagger UIのAuthorizeにEntra IDのAPI access tokenを入力すると、保護されたAPIを試せます。本番では既定で無効です。一時的に公開する場合はApp Service設定に `Swagger__Enabled=true` を追加し、利用後に削除してください。

### フロントエンドから API を呼び出す

フロントエンドのビルド時に、次の環境変数を設定します。

```env
VITE_USE_MOCK_DATA=false
VITE_API_BASE_URL=https://<App Service名>.azurewebsites.net/api
VITE_ENTRA_CUSTOMER_CLIENT_ID=<customer-spa-client-id>
VITE_ENTRA_CUSTOMER_AUTHORITY=<customer-authority>
VITE_ENTRA_CUSTOMER_API_SCOPE=<api-scope-uri>
VITE_ENTRA_CUSTOMER_KNOWN_AUTHORITY=<external-id-domain>
VITE_ENTRA_STAFF_CLIENT_ID=<staff-spa-client-id>
VITE_ENTRA_STAFF_AUTHORITY=<staff-authority>
VITE_ENTRA_STAFF_API_SCOPE=<api-scope-uri>
```

`VITE_API_BASE_URL` は Vite のビルド時に埋め込まれるため、設定を変更したら Static Web Apps のフロントエンドを再ビルド・再デプロイしてください。Azure Static Web Apps の GitHub Actions を使っている場合は、ワークフローの build job に環境変数として設定します。値は API の URL なので秘密情報ではありません。

CORS は App Service の `Cors__AllowedOrigins__0` に Static Web Apps の Origin（`https://...azurestaticapps.net`）を設定します。独自ドメインも使う場合は `Cors__AllowedOrigins__1` などを追加します。Origin にはパスや末尾の `/` を含めません。ローカル開発用の許可 Origin は `appsettings.Development.json` に分離しており、本番設定には含まれません。

### この段階の制約

- Azure SQL接続設定と初回マイグレーション適用が必要です。`/api/health` はDB接続不可時に503を返します。
- ローカルDevelopmentはInMemory DBのため、再起動するとサンプルデータへ戻ります。
- 認証テナント、API Audience/Scope、スタッフアプリロール、顧客IDの初期紐付けはデプロイ前に設定してください。認証値が不完全な本番APIは起動しません。
- 顧客は紐付けられた自分の顧客・自転車・契約・予定・履歴だけを参照でき、登録・更新・削除はできません。スタッフは業務データを操作でき、スタッフアカウントの管理と顧客ID紐付けは `StaffAdmin` のみ可能です。
- バックアップと監査ログは未実装です。別途運用要件を定めてください。

## API一覧

| Method | Endpoint | 内容 |
|---|---|---|
| GET / POST | `/api/customers` | 顧客一覧・登録 |
| GET / PUT / DELETE | `/api/customers/{id}` | 顧客詳細・更新・削除 |
| PUT | `/api/customers/{id}/identity` | StaffAdminによるEntra顧客ID紐付け |
| GET | `/api/me` | 認証済みユーザー情報 |
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

## バックエンド統合テスト

プロジェクトルートから次を実行します。

```powershell
dotnet test backend/RoadbikeCare.Api.Tests/RoadbikeCare.Api.Tests.csproj
```

テストは `WebApplicationFactory` とDevelopment用InMemory DBを使うため、Azure SQL接続やマイグレーション適用は不要です。未認証アクセス拒否、スタッフ権限、顧客ID紐付けと顧客間のデータ分離、顧客による書き込み拒否に加え、契約・作業完了のHTTPフローを検証します。実際のEntraテナント設定と本番DB接続・トランザクション動作は別途Azure環境で確認してください。

## 動作確認例（PowerShell）

```powershell
Invoke-RestMethod http://localhost:5080/api/health
```

顧客・自転車・契約などの保護されたAPIは、SPAでサインインした状態で確認してください。APIモードにEntra設定がない場合は、認証付き統合テストを `dotnet test backend/RoadbikeCare.Api.Tests/RoadbikeCare.Api.Tests.csproj` で実行できます。

## 開発データの制約

- 開発用サンプルデータはInMemory DBにのみ投入します。本番Azure SQLにはサンプルデータを投入しません。
- 作業・点検の完了と契約回数・履歴の更新はAzure SQLのSerializableトランザクションで実行します。
- バックアップと監査ログは別途導入が必要です。

## フロントエンドの環境設定

Vueモックモードは設定不要で、API通信も行いません。APIモードは前述の顧客・スタッフ向けEntra設定が必要です。`frontend/vite.config.ts` のViteは `/api` を `http://localhost:5080` に転送します。フロントエンドとAPIを別ホストへ配置する場合は、`frontend/.env` に `VITE_API_BASE_URL` を設定し、APIのCORS許可オリジンも配置先に合わせて変更してください。

## Azure SQL永続化

APIはEF Core SQL Server Providerを介してデータを読み書きします。テーブル変更は `backend/RoadbikeCare.Api/Data/Migrations/` のSQL Serverマイグレーションで管理し、デプロイ前に `dotnet ef database update` を実行します。契約枠の検証・予定登録・作業完了の関連更新はトランザクション内で処理します。APIのURLとJSON形式は維持します。


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
