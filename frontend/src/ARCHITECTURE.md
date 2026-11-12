# フロントエンド構成

画面の動作を維持しながら、表示コンポーネントと業務処理を段階的に分離しています。

- `types/`: 画面・業務データで共有する型定義
- `constants/`: ページ名と表示名などの固定値
- `utils/`: 一覧検索・並び替えなど、画面に依存しない処理
- `services/`: APIレスポンス変換と契約日付ルール
- `fixtures/`: Vueモックモード用の初期サンプルデータ
- `composables/`: APIデータ、認証、一覧集計、登録・編集フローなどの状態と処理
- `components/`: ページ表示、レイアウト、登録・詳細モーダル
- `api.ts`: HTTP通信
- `App.vue`: 共有状態を所有し、ページ・モーダル・Composableを組み合わせる

## 現在の責務分担

- `App.vue` はページ選択、共有フォーム値、選択中の顧客・自転車、子コンポーネント間のイベント連携を担当します。
- `composables/usePortalSession.ts` は認証復元、サインイン・サインアウトとデータの初期化を担当します。
- `composables/usePortalApiData.ts` はAPIデータの取得・画面用変換と更新時の共通通知を担当します。
- `usePortalApiData.ts` は接続状態インジケーターの表示内容も状態に応じて提供します。
- `fixtures/mockData.ts` は型付きのVueモックデータを生成し、画面状態から独立して管理します。
- `composables/useContractRegistration.ts`、`useScheduleRegistration.ts`、`useWorkEditing.ts`、`useWorkLifecycle.ts` は契約・予定・作業の更新フローを担当します。
- `composables/useCustomerNavigation.ts` は顧客詳細からの自転車選択、画面遷移、対象を引き継いだ登録フォームの初期化を担当します。
- `composables/useBikeOptions.ts` は登録フォームで使う自転車候補ラベルと選択IDの同期を担当します。
- `composables/useBikeCatalog.ts` は契約プラン参照と選択中自転車の解決を担当します。
- `composables/useMailPreview.ts` は案内メールのプレビュー状態とモック送信後の通知を担当します。
- `composables/usePortalNavigation.ts` はページ切替時の通知クリアと、モック環境の表示ロール切り替えを担当します。
- `composables/useDetailSelection.ts` は顧客・自転車詳細を開く選択状態と、顧客ID連携フォームの初期化を担当します。
- `composables/useBikeDetails.ts` は自転車詳細モーダルの契約プラン、店舗名、残回数の表示データを組み立てます。
- `composables/useMasterDataRegistration.ts` は店舗・受付担当者・プラン登録のフォーム状態、モーダル状態、登録処理を担当します。
- `composables/useContractRegistration.ts` は契約登録フォーム、登録モーダル、顧客・自転車・契約の登録と次の作業受付への遷移を担当します。
- `composables/useWorkLifecycle.ts` と `useWorkEditing.ts` は完了・編集フォーム状態と、作業の状態遷移・編集保存をそれぞれ担当します。
- `useDashboardData.ts`、`useStaffLists.ts`、`useCustomerPortal.ts`、`useCustomerDetails.ts`、`useActivityLists.ts` は各画面用の集計行を作ります。
- `components/` の画面・モーダルはpropsで表示データを受け取り、イベントで操作を親へ通知します。

以下は、分割を進めた際の作業記録です。現在の責務はこの節を基準にしてください。

## 画面レイアウトの分割

- `components/layout/AppSidebar.vue`: サイドバー、ページ遷移イベント、スタッフ／顧客表示モード切替イベント
- `components/layout/AppHeader.vue`: パンくずと日付表示
- `App.vue`: ページ状態と表示モードの状態を保持し、レイアウトコンポーネントへprops/eventsで受け渡す

レイアウト側に業務データやAPI通信を持たせず、状態の所有者は引き続きApp.vueとしています。次の段階では、各ページの業務状態を整理してから `views/` にページ本体を移します。

## ダッシュボード画面の分離

- `components/dashboard/DashboardView.vue`：ダッシュボードの表示を担当します。
- 自転車一覧、各種件数、ソート済みの直近作業予定を `App.vue` から props で受け取ります。
- 一覧のソート操作と顧客詳細表示は emit で `App.vue` に通知し、既存の状態管理・処理を維持します。
- API通信、データ集計、ソート状態は引き続き `App.vue` が管理します。

## 画面コンポーネント分割（第2段階・続き）

- `components/bikes/BikeContractsView.vue`：顧客・自転車・年間契約一覧の表示を担当します。
- 契約一覧の検索・ソート・プラン情報・残回数の算出は `App.vue` 側で行い、表示用データをpropsで渡します。
- 年間契約登録、顧客詳細、自転車詳細の操作はイベントとして `App.vue` に通知し、既存のモーダル・業務処理を再利用します。

## 目標イベント・点検サポート画面の分離

- `components/events/EventInspectionView.vue`：目標イベント・点検サポート一覧の表示を担当します。
- 顧客・自転車フィルター、キーワード検索、ソート結果、点検サポートの新規登録可能枠は `App.vue` で集計し、表示用行データとしてpropsで渡します。
- 編集・作業開始・完了登録・顧客詳細表示はイベントで `App.vue` に通知します。
- 点検サポートの残回数計算は既存の `inspection` 区分を使用し、メンテナンス回数とは独立したままです。


## メンテナンス履歴画面の分離

- `components/history/HistoryView.vue`：自転車選択、履歴検索、履歴一覧の表示を担当します。
- 選択中の自転車、検索結果、日付ソート済み履歴は `App.vue` からpropsで渡します。
- 自転車選択・検索・ソート操作はイベントで親へ通知し、既存の状態管理を維持します。

## 連絡・メール管理画面の分離

- `components/messages/MessagesView.vue`：案内待ちのメンテナンス・点検予定、検索欄、ソート操作の表示を担当します。
- 案内待ちデータの統合、顧客・自転車情報の付加、検索・ソートは `App.vue` 側で行い、表示用行データとしてpropsで渡します。
- 顧客詳細表示とメール作成・内容確認はイベントで `App.vue` に通知し、既存の顧客詳細・メールモーダルを再利用します。
- メンテナンスと点検サポートの案内待ち件数は、従来どおり別々のデータを合算して表示します。


## 顧客マイページの分離

- `components/mypage/MypageView.vue`：顧客向け契約自転車・プラン残回数・メンテナンス／点検予定の表示を担当します。
- 顧客の契約自転車、プラン情報、残回数、予定一覧の統合・ソートは `App.vue` 側で行い、表示用データとしてpropsで渡します。
- 検索文字列と日付・状態のソート操作はイベントで親へ通知し、既存の検索・ソート状態を維持します。
- API通信、顧客データの管理、回数計算などの業務ロジックは引き続き `App.vue` が管理します。

## 店舗・受付担当者管理画面の分離

- `components/shops/ShopsView.vue`：店舗一覧、担当者一覧表示、店舗情報検索を担当します。
- 店舗・担当者データと検索文字列は `App.vue` が保持し、検索文字列の更新・店舗登録・担当者登録はイベントで親へ通知します。
- 登録モーダルとAPI登録処理は既存どおり `App.vue` が管理します。

## プラン設定画面の分離

- `components/plans/PlansView.vue`：年間契約プランと各サービスの利用回数枠を表示します。
- プランデータは `App.vue` からpropsで受け取り、追加操作のみイベントで親へ通知します。
- プラン追加フォーム、入力検証、API登録処理は既存どおり `App.vue` が管理します。

## 店舗・受付担当者管理画面の分離

- `components/shops/ShopsView.vue`：店舗一覧、店舗情報検索、店舗ごとの受付担当者表示を担当します。
- 店舗・担当者データと検索文字列は `App.vue` が保持し、検索文字列の更新・店舗登録・担当者登録はイベントで親へ通知します。
- 登録モーダルとAPI登録処理は既存どおり `App.vue` が管理します。

## プラン設定画面の分離

- `components/plans/PlansView.vue`：年間契約プランと各サービスの利用回数枠を表示します。
- プランデータは `App.vue` からpropsで受け取り、追加操作のみイベントで親へ通知します。
- プラン追加フォーム、入力検証、API登録処理は既存どおり `App.vue` が管理します。


## 契約日付ルールの分離

- `services/contractRules.ts` に年間契約開始日、契約受付期限、オーバーホール実施期間の判定を集約しました。
- `App.vue` は判定関数を利用し、画面通知・API登録フローは引き続き管理します。
- 日付形式の検証と閏年の2月末判定を共通化し、契約登録・オーバーホール登録・予定編集で同じルールを利用します。


## 予定登録処理のComposable化

- `composables/useScheduleRegistration.ts` にオーバーホール・メンテナンス・イベント点検予定の登録フローを移しました。
- 登録対象に応じた3種の契約残枠表示も同Composableが提供します。
- 入力値・一覧データ・モーダル状態は `App.vue` が保持し、Composableには `ref` と既存の `apiAction` を渡します。
- APIモード／Vueモックモードの分岐、契約回数チェック、受付担当者の選択、日付検証、成功時のモーダル制御は従来の動作を維持しています。
- API通信の共通処理（エラー処理・再取得）は引き続き `App.vue` の `apiAction` が担当します。

## 作業開始・完了処理のComposable化

- `composables/useWorkLifecycle.ts` にオーバーホール・メンテナンス・点検サポートの作業開始、完了モーダル初期化、完了登録処理を移しました。
- 作業一覧、担当者、完了モーダルの入力状態は `App.vue` が保持し、Composableへ `ref` と既存の `apiAction` を渡します。
- 作業開始はステータスを「作業中」に変更するだけで、契約回数は消費しません。
- 完了登録は既存の完了APIを呼び出します。バックエンドでは完了状態への更新、契約回数の消費、履歴登録を同一トランザクションで実行するため、フロントエンドから履歴を別途追加しません。
- 予定・案内・予約・作業中・変更の段階では回数を消費せず、完了APIの成功時だけ回数と履歴が更新される既存仕様を維持しています。
