<script setup lang="ts">
  import { computed, onMounted, ref } from 'vue';
  import AppSidebar from './components/layout/AppSidebar.vue';
  import AppHeader from './components/layout/AppHeader.vue';
  import DashboardView from './components/dashboard/DashboardView.vue';
  import BikeContractsView from './components/bikes/BikeContractsView.vue';
  import OverhaulView from './components/overhaul/OverhaulView.vue';
  import MaintenanceView from './components/maintenance/MaintenanceView.vue';
  import EventInspectionView from './components/events/EventInspectionView.vue';
  import HistoryView from './components/history/HistoryView.vue';
  import MessagesView from './components/messages/MessagesView.vue';
  import MypageView from './components/mypage/MypageView.vue';
  import ShopsView from './components/shops/ShopsView.vue';
  import PlansView from './components/plans/PlansView.vue';
  import { apiGet, apiPost, apiPut } from './api';
  import type { Page, Status, Plan, Bike, Work, EventCheck, History } from './types';
  import { pageTitles } from './constants/pages';
  import { matches, useListSort } from './utils/list';
  import {
    dateSlash,
    dateIso,
    mapBike,
    mapWork,
    mapEvent,
    type ApiBike,
  } from './services/apiMappers';
  import {
    isOverhaulSeason,
    isAnnualContractStart,
    isApplicationWithinContractYear,
  } from './services/contractRules';
  const USE_MOCK_DATA = import.meta.env.VITE_USE_MOCK_DATA === 'true';
  const dataSourceStatus = ref<'mock' | 'api' | 'error'>(USE_MOCK_DATA ? 'mock' : 'api');
  const dataSourceLabel = USE_MOCK_DATA ? 'MOCK DATA' : 'API MODE';
  const page = ref<Page>('dashboard'),
    role = ref<'staff' | 'customer'>('staff'),
    selectedBikeId = ref(1),
    notice = ref(''),
    showMail = ref(false),
    mailTarget = ref('山田 太郎'),
    mailKind = ref('メンテナンスのご案内');
  const showContractModal = ref(false),
    showShopModal = ref(false),
    showStaffModal = ref(false),
    showPlanModal = ref(false),
    showOverhaulModal = ref(false),
    showMaintenanceModal = ref(false),
    showEventModal = ref(false);
  const showCustomerDetail = ref(false),
    showBikeDetail = ref(false),
    selectedCustomerName = ref(''),
    selectedBikeFilter = ref<number | null>(null);
  // 一覧用の顧客名・自転車名フィルター。候補一覧を開かず、部分一致で直接絞り込みます。
  const customerFilterText = ref(''),
    bikeFilterText = ref('');
  const today = '2026年11月10日（火）';
  // 一覧ごとのキーワード検索。顧客名・自転車・作業名・店舗・担当者などを横断して検索します。
  const searchShops = ref(''),
    searchBikes = ref(''),
    searchOverhauls = ref(''),
    searchMaintenance = ref(''),
    searchEvents = ref(''),
    searchHistory = ref(''),
    searchMessages = ref(''),
    searchMyPage = ref('');
  function matchesBikeFilter(bikeId: number) {
    const bike = bikes.value.find((item) => item.id === bikeId);
    return (
      (!selectedBikeFilter.value || bikeId === selectedBikeFilter.value) &&
      matches(customerFilterText.value, bike?.customer) &&
      matches(bikeFilterText.value, bike?.maker, bike?.model, bike?.serial)
    );
  }

  function clearBikeNameFilters() {
    customerFilterText.value = '';
    bikeFilterText.value = '';
    selectedBikeFilter.value = null;
  }

  const { toggleListSort, sortByDate, sortLabel } = useListSort();
  const shops = ref<{ id: number; name: string; address: string; phone: string }[]>([
    { id: 1, name: '金沢本店', address: '石川県金沢市', phone: '076-000-0001' },
    { id: 2, name: '富山店', address: '富山県富山市', phone: '076-000-0002' },
  ]);
  const staff = ref<{ id: number; shopId: number; name: string; role: string }[]>([
    { id: 1, shopId: 1, name: '田中', role: '受付担当' },
    { id: 2, shopId: 1, name: '山本', role: 'メカニック' },
    { id: 3, shopId: 2, name: '佐々木', role: '受付担当' },
  ]);
  const newShopName = ref(''),
    newShopAddress = ref(''),
    newShopPhone = ref(''),
    newStaffName = ref(''),
    newStaffShop = ref(1);
  const contractForm = ref({
    customer: '山田 太郎',
    maker: 'SPECIALIZED',
    model: 'Tarmac SL8',
    year: 2026,
    serial: '',
    planId: 1,
    applicationDate: '2026-12-15',
    start: '2026-12-01',
    shopId: 1,
    receptionist: '田中',
  });
  const editTarget = ref<{ kind: 'maintenance' | 'event' | 'overhaul'; id: number } | null>(null),
    editDate = ref(''),
    editEndDate = ref(''),
    editTitle = ref(''),
    editDetail = ref(''),
    editReceptionist = ref('');
  const completionTarget = ref<{ kind: 'maintenance' | 'event' | 'overhaul'; id: number } | null>(
      null
    ),
    completionMemo = ref(''),
    completionDate = ref('2026-11-10');
  const customers = ref<{ id: number; name: string; email: string; phone?: string }[]>([
    { id: 1, name: '山田 太郎', email: 't.yamada@example.com', phone: '090-0000-0001' },
    { id: 2, name: '佐藤 花子', email: 'h.sato@example.com', phone: '090-0000-0002' },
    { id: 3, name: '鈴木 一郎', email: 'i.suzuki@example.com', phone: '090-0000-0003' },
  ]);
  const plans = ref<Plan[]>([
    { id: 1, name: 'プレミアムプラン', overhaulCount: 1, maintenanceCount: 4, inspectionCount: 3 },
    {
      id: 2,
      name: 'スタンダードプラン',
      overhaulCount: 1,
      maintenanceCount: 3,
      inspectionCount: 2,
    },
    { id: 3, name: 'ライトプラン', overhaulCount: 1, maintenanceCount: 2, inspectionCount: 1 },
  ]);
  const newPlan = ref({ name: '', overhaulCount: 1, maintenanceCount: 2, inspectionCount: 1 });
  async function addPlan() {
    const name = newPlan.value.name.trim();
    if (!name) {
      notice.value = 'プラン名を入力してください。';
      return;
    }
    const counts = [
      newPlan.value.overhaulCount,
      newPlan.value.maintenanceCount,
      newPlan.value.inspectionCount,
    ];
    if (counts.some((n) => !Number.isInteger(n) || n < 0)) {
      notice.value = '各回数は0以上の整数で入力してください。';
      return;
    }
    const ok = await apiAction(
      () => apiPost('/plans', { ...newPlan.value, name }),
      'プランを追加しました。顧客・自転車・契約管理画面から選択できます。'
    );
    if (ok) {
      newPlan.value = { name: '', overhaulCount: 1, maintenanceCount: 2, inspectionCount: 1 };
      showPlanModal.value = false;
    }
  }

  const bikes = ref<Bike[]>([
    {
      id: 1,
      customer: '山田 太郎',
      maker: 'SPECIALIZED',
      model: 'Tarmac SL8 Expert',
      year: 2025,
      serial: 'WSBC0250A001',
      planId: 1,
      contractStart: '2026/12/01',
      contractEnd: '2027/11/30',
      overhaulUsed: 0,
      maintenanceUsed: 1,
      inspectionUsed: 1,
      overhaulCompleted: '',
      shopId: 1,
      receptionist: '田中',
    },
    {
      id: 2,
      customer: '佐藤 花子',
      maker: 'TREK',
      model: 'Madone SL 7',
      year: 2024,
      serial: 'WTU12345678',
      planId: 2,
      contractStart: '2026/12/01',
      contractEnd: '2027/11/30',
      overhaulUsed: 0,
      maintenanceUsed: 0,
      inspectionUsed: 0,
      overhaulCompleted: '',
      shopId: 1,
      receptionist: '田中',
    },
    {
      id: 3,
      customer: '鈴木 一郎',
      maker: 'GIANT',
      model: 'TCR Advanced Pro',
      year: 2023,
      serial: 'GNT2023-0088',
      planId: 3,
      contractStart: '2026/12/01',
      contractEnd: '2027/11/30',
      overhaulUsed: 1,
      maintenanceUsed: 2,
      inspectionUsed: 0,
      overhaulCompleted: '2027/01/15',
      shopId: 2,
      receptionist: '佐々木',
    },
    {
      id: 4,
      customer: '山田 太郎',
      maker: 'CANYON',
      model: 'Speedmax CF 7',
      year: 2025,
      serial: 'CNY-SM-2025-014',
      planId: 2,
      contractStart: '2026/12/01',
      contractEnd: '2027/11/30',
      overhaulUsed: 0,
      maintenanceUsed: 1,
      inspectionUsed: 0,
      overhaulCompleted: '',
      shopId: 1,
      receptionist: '田中',
    },
  ]);
  const planFor = (b: Bike) =>
    plans.value.find((p) => p.id === b.planId) ??
    plans.value[0] ??
    ({ id: 0, name: '未設定', overhaulCount: 0, maintenanceCount: 0, inspectionCount: 0 } as Plan);
  const overhauls = ref<Work[]>([
    {
      id: 1,
      bikeId: 1,
      date: '2027/01/15',
      endDate: '2027/01/16',
      status: '作業中',
      title: '年間オーバーホール',
      detail: '全体オーバーホール・消耗品点検',
      shopId: 1,
      receptionist: '田中',
    },
    {
      id: 2,
      bikeId: 2,
      date: '2027/02/05',
      status: '予約済み',
      title: '年間オーバーホール',
      detail: '納車後初回オーバーホール',
      shopId: 1,
      receptionist: '田中',
    },
    {
      id: 3,
      bikeId: 3,
      date: '2027/01/15',
      endDate: '2027/01/16',
      status: '完了',
      title: '年間オーバーホール',
      detail: '駆動系洗浄・ワイヤー交換',
      shopId: 2,
      receptionist: '佐々木',
    },
    {
      id: 4,
      bikeId: 4,
      date: '2027/02/12',
      status: '予定',
      title: '年間オーバーホール',
      detail: '専用コクピット・油圧ブレーキ・駆動系点検',
      shopId: 1,
      receptionist: '田中',
    },
  ]);
  const maintenance = ref<Work[]>([
    {
      id: 1,
      bikeId: 1,
      date: '2027/04/10',
      endDate: '2027/04/10',
      status: '完了',
      title: '春の目標イベント前メンテナンス',
      detail: '駆動系・変速・ブレーキ調整',
      shopId: 1,
      receptionist: '田中',
    },
    {
      id: 2,
      bikeId: 1,
      date: '2027/06/05',
      status: '予定',
      title: '夏季メインイベントに向けたメンテナンス',
      detail: '消耗品・ホイール・ポジション確認',
      shopId: 1,
      receptionist: '田中',
    },
    {
      id: 3,
      bikeId: 3,
      date: '2027/05/20',
      endDate: '2027/05/21',
      status: '完了',
      title: 'シーズン前メンテナンス',
      detail: '変速・ブレーキ・タイヤ点検',
      shopId: 2,
      receptionist: '佐々木',
    },
    {
      id: 4,
      bikeId: 3,
      date: '2027/08/01',
      endDate: '2027/08/01',
      status: '完了',
      title: '夏季イベント前メンテナンス',
      detail: 'チェーン清掃・注油・各部増し締め',
      shopId: 2,
      receptionist: '佐々木',
    },
    {
      id: 5,
      bikeId: 4,
      date: '2027/06/12',
      status: '予定',
      title: 'TTイベント前ポジション・機材確認',
      detail: 'エアロポジション・変速・ブレーキ・タイヤ確認',
      shopId: 1,
      receptionist: '田中',
    },
  ]);
  const eventChecks = ref<EventCheck[]>([
    {
      id: 1,
      bikeId: 1,
      name: 'シーズン開幕ロードレース',
      eventDate: '2027/04/18',
      inspectionDate: '2027/04/14',
      endDate: '2027/04/15',
      status: '作業中',
      memo: 'イベント直前の安全確認',
      shopId: 1,
      receptionist: '田中',
    },
    {
      id: 2,
      bikeId: 1,
      name: '夏のメインイベント',
      eventDate: '2027/07/25',
      inspectionDate: '2027/07/21',
      status: '予定',
      memo: 'タイヤ・ブレーキ・締結確認',
      shopId: 1,
      receptionist: '田中',
    },
    {
      id: 3,
      bikeId: 2,
      name: '冬季サイクルイベント',
      eventDate: '2027/12/10',
      inspectionDate: '2027/12/07',
      status: '予定',
      memo: '次年度契約のイベント予定例',
      shopId: 1,
      receptionist: '田中',
    },
    {
      id: 4,
      bikeId: 4,
      name: '夏季TTメインイベント',
      eventDate: '2027/06/20',
      inspectionDate: '2027/06/16',
      status: '予定',
      memo: 'TTポジション・ホイール・ブレーキの事前確認',
      shopId: 1,
      receptionist: '田中',
    },
  ]);
  const histories = ref<History[]>([
    {
      id: 1,
      bikeId: 1,
      date: '2027/04/10',
      category: 'メンテナンス',
      title: '春の目標イベント前メンテナンス',
      detail: '駆動系・変速・ブレーキ調整',
    },
    {
      id: 2,
      bikeId: 3,
      date: '2027/01/15',
      category: 'オーバーホール',
      title: '年間オーバーホール',
      detail: '駆動系洗浄・ワイヤー交換',
    },
    {
      id: 3,
      bikeId: 3,
      date: '2027/05/20',
      category: 'メンテナンス',
      title: 'シーズン前メンテナンス',
      detail: '変速・ブレーキ・タイヤ点検',
    },
    {
      id: 4,
      bikeId: 3,
      date: '2027/08/01',
      category: 'メンテナンス',
      title: '夏季イベント前メンテナンス',
      detail: 'チェーン清掃・注油・各部増し締め',
    },
    {
      id: 5,
      bikeId: 4,
      date: '2027/05/18',
      category: 'メンテナンス',
      title: '初回メンテナンス',
      detail: 'ポジション・変速・ブレーキ調整',
      memo: '左右パッドクリアランスを再確認',
    },
  ]);
  const receptionistName = (id: number) => staff.value.find((x) => x.id === id)?.name ?? '';
  async function reloadData() {
    if (USE_MOCK_DATA) return;
    const [c, sh, st, pl, b, oh, ma, ev, hi] = await Promise.all([
      apiGet<any[]>('/customers'),
      apiGet<any[]>('/shops'),
      apiGet<any[]>('/staff'),
      apiGet<Plan[]>('/plans'),
      apiGet<ApiBike[]>('/bikes'),
      apiGet<any[]>('/overhauls'),
      apiGet<any[]>('/maintenance'),
      apiGet<any[]>('/events'),
      apiGet<any[]>('/history'),
    ]);
    customers.value = c;
    shops.value = sh;
    staff.value = st;
    plans.value = pl;
    bikes.value = b.map((item) => mapBike(item, receptionistName));
    overhauls.value = oh.map((item) => mapWork(item, receptionistName));
    maintenance.value = ma.map((item) => mapWork(item, receptionistName));
    eventChecks.value = ev.map((item) => mapEvent(item, receptionistName));
    histories.value = hi.map((x: any) => ({
      id: x.id,
      bikeId: x.bikeId,
      date: dateSlash(x.date),
      startDate: dateSlash(x.startDate),
      category: x.category,
      title: x.title,
      detail: x.detail,
      memo: x.memo ?? '',
    }));
  }
  onMounted(async () => {
    if (USE_MOCK_DATA) {
      notice.value = 'Vue内のモックデータを表示しています。APIへの通信は行いません。';
      return;
    }
    // APIモードではVue側の初期モック値を消し、API取得結果だけを表示します。
    customers.value = [];
    shops.value = [];
    staff.value = [];
    plans.value = [];
    bikes.value = [];
    overhauls.value = [];
    maintenance.value = [];
    eventChecks.value = [];
    histories.value = [];
    try {
      await reloadData();
      notice.value = 'バックエンドAPIに接続しました。';
    } catch (e) {
      dataSourceStatus.value = 'error';
      notice.value = `APIに接続できません。モックデータへの自動切替は行いません。${e instanceof Error ? ` (${e.message})` : ''}`;
    }
  });
  async function apiAction(action: () => Promise<unknown>, success: string) {
    if (USE_MOCK_DATA) {
      notice.value =
        '現在はVueモックデータ表示モードです。登録・更新操作はAPIモードで行ってください。';
      return false;
    }
    try {
      await action();
      await reloadData();
      dataSourceStatus.value = 'api';
      notice.value = success;
      return true;
    } catch (e) {
      dataSourceStatus.value = 'error';
      notice.value = e instanceof Error ? e.message : 'API処理に失敗しました。';
      return false;
    }
  }
  const newOverhaulBike = ref(1),
    newOverhaulReceptionDate = ref('2026-12-15'),
    newOverhaulDate = ref('2027-01-15'),
    newOverhaulEndDate = ref('2027-01-16'),
    newOverhaulTitle = ref('年間オーバーホール'),
    newOverhaulDetail = ref('全体点検・消耗品確認・駆動系整備');
  const newWorkBike = ref(1),
    newWorkDate = ref('2027-07-01'),
    newWorkEndDate = ref('2027-07-01'),
    newWorkTitle = ref('メインイベントに向けたメンテナンス'),
    newWorkDetail = ref('イベントに向けた状態確認・調整');
  const newEventBike = ref(1),
    newEventName = ref(''),
    newEventDate = ref('2027-08-01'),
    newInspectionDate = ref('2027-07-28'),
    newInspectionEndDate = ref('2027-07-28');
  function bikeLabel(id: number) {
    const b = bikes.value.find((x) => x.id === id);
    return b ? `${b.customer} / ${b.maker} ${b.model}` : '';
  }
  function setBikeFromLabel(label: string, target: 'overhaul' | 'maintenance' | 'event') {
    const b = bikes.value.find((x) => bikeLabel(x.id) === label);
    if (!b) return;
    if (target === 'overhaul') newOverhaulBike.value = b.id;
    else if (target === 'maintenance') newWorkBike.value = b.id;
    else newEventBike.value = b.id;
  }
  const overhaulBikeSearch = ref(bikeLabel(newOverhaulBike.value)),
    maintenanceBikeSearch = ref(bikeLabel(newWorkBike.value)),
    eventBikeSearch = ref(bikeLabel(newEventBike.value));
  const selectedBike = computed(
    () =>
      bikes.value.find((b) => b.id === selectedBikeId.value) ??
      bikes.value[0] ??
      ({
        id: 0,
        customer: '',
        maker: '',
        model: '',
        year: 0,
        serial: '',
        planId: 0,
        contractStart: '',
        contractEnd: '',
        overhaulUsed: 0,
        maintenanceUsed: 0,
        inspectionUsed: 0,
        overhaulCompleted: '',
        shopId: 0,
        receptionist: '',
      } as Bike)
  );
  const historyRows = computed(() =>
    sortByDate(
      histories.value.filter(
        (x) =>
          x.bikeId === selectedBikeId.value &&
          matches(searchHistory.value, x.date, x.category, x.title, x.detail, x.memo)
      ),
      'history',
      (x) => x.date
    )
  );
  const messageRows = computed(() =>
    sortByDate(
      [
        ...maintenance.value
          .filter((x) => x.status === '案内待ち')
          .map((x) => ({ ...x, kind: 'メンテナンス' })),
        ...eventChecks.value
          .filter((x) => x.status === '案内待ち')
          .map((x) => ({ ...x, date: x.inspectionDate, title: x.name, kind: '点検サポート' })),
      ]
        .filter((x) => {
          const bike = bikes.value.find((item) => item.id === x.bikeId);
          return matches(
            searchMessages.value,
            bike?.customer,
            bike?.model,
            x.date,
            x.title,
            x.kind
          );
        })
        .map((x) => {
          const bike = bikes.value.find((item) => item.id === x.bikeId);
          return {
            id: `${x.kind}-${x.id}`,
            bikeId: x.bikeId,
            customer: bike?.customer ?? '',
            bikeModel: bike?.model ?? '',
            kind: x.kind,
            title: x.title,
            date: x.date,
          };
        }),
      'messages',
      (x) => x.date,
      (x) => x.customer
    )
  );
  const selectedCustomerBikes = computed(() =>
    bikes.value.filter((b) => b.customer === selectedCustomerName.value)
  );
  const selectedCustomer = computed(() =>
    customers.value.find((c) => c.name === selectedCustomerName.value)
  );
  const selectedCustomerWorks = computed(() =>
    [
      ...overhauls.value
        .filter((w) => selectedCustomerBikes.value.some((b) => b.id === w.bikeId))
        .map((w) => ({ ...w, kind: 'オーバーホール' })),
      ...maintenance.value
        .filter((w) => selectedCustomerBikes.value.some((b) => b.id === w.bikeId))
        .map((w) => ({ ...w, kind: 'メンテナンス' })),
      ...eventChecks.value
        .filter((w) => selectedCustomerBikes.value.some((b) => b.id === w.bikeId))
        .map((w) => ({ ...w, date: w.inspectionDate, title: w.name, kind: '点検サポート' })),
    ].sort((a, b) => a.date.localeCompare(b.date))
  );
  const selectedCustomerHistories = computed(() =>
    histories.value
      .filter((h) => selectedCustomerBikes.value.some((b) => b.id === h.bikeId))
      .sort((a, b) => b.date.localeCompare(a.date))
  );
  function composeMail(payload: { customer: string; kind: string }) {
    mailTarget.value = payload.customer || '山田 太郎';
    mailKind.value = `${payload.kind}のご案内`;
    showMail.value = true;
  }

  function openCustomerDetail(name: string) {
    selectedCustomerName.value = name;
    showCustomerDetail.value = true;
  }
  function openCustomerBikeRegistration(kind: 'overhaul' | 'maintenance' | 'event', bike: Bike) {
    // 登録フォームは顧客詳細モーダルに重ねず、該当する管理画面上で単独表示します。
    // 顧客・自転車の選択状態を引き継ぎ、キャンセル時も管理画面に留まります。
    openCustomerBikePage(kind === 'event' ? 'events' : kind, bike.id);
    if (kind === 'overhaul') {
      newOverhaulBike.value = bike.id;
      overhaulBikeSearch.value = bikeLabel(bike.id);
      const year = Number(bike.contractStart.slice(0, 4));
      newOverhaulReceptionDate.value = `${year}-12-15`;
      newOverhaulDate.value = `${year + 1}-01-15`;
      newOverhaulEndDate.value = `${year + 1}-01-16`;
      showOverhaulModal.value = true;
    } else if (kind === 'maintenance') {
      newWorkBike.value = bike.id;
      maintenanceBikeSearch.value = bikeLabel(bike.id);
      newWorkDate.value = '2027-07-01';
      newWorkEndDate.value = '2027-07-01';
      showMaintenanceModal.value = true;
    } else {
      newEventBike.value = bike.id;
      eventBikeSearch.value = bikeLabel(bike.id);
      newEventName.value = '';
      newEventDate.value = '2027-08-01';
      newInspectionDate.value = '2027-07-28';
      newInspectionEndDate.value = '2027-07-28';
      showEventModal.value = true;
    }
    notice.value = `${bike.customer} 様の ${bike.maker} ${bike.model} を登録対象に設定しました。`;
  }
  function openBikeDetail(bikeId: number) {
    selectedBikeId.value = bikeId;
    showBikeDetail.value = true;
  }
  function openCustomerBikePage(
    target: 'overhaul' | 'maintenance' | 'events' | 'history',
    bikeId: number
  ) {
    selectedBikeId.value = bikeId;
    selectedBikeFilter.value = target === 'history' ? null : bikeId;
    customerFilterText.value = '';
    bikeFilterText.value = '';
    searchOverhauls.value = '';
    searchMaintenance.value = '';
    searchEvents.value = '';
    searchHistory.value = '';
    page.value = target;
    showCustomerDetail.value = false;
  }
  function openCustomerWork(item: { kind: string; bikeId: number }) {
    const target =
      item.kind === 'オーバーホール'
        ? 'overhaul'
        : item.kind === '点検サポート'
          ? 'events'
          : 'maintenance';
    openCustomerBikePage(target, item.bikeId);
  }
  const remaining = (b: Bike, kind: 'maintenance' | 'inspection' | 'overhaul') => {
    const p = planFor(b);
    const used =
      kind === 'maintenance'
        ? b.maintenanceUsed
        : kind === 'inspection'
          ? b.inspectionUsed
          : b.overhaulUsed;
    const limit =
      kind === 'maintenance'
        ? p.maintenanceCount
        : kind === 'inspection'
          ? p.inspectionCount
          : p.overhaulCount;
    return Math.max(0, limit - used);
  };
  // 契約枠のうち、完了済み回数と未完了の予定回数を差し引いた新規登録可能数。キャンセルは枠を解放します。
  const activeScheduleCount = (bikeId: number, kind: 'maintenance' | 'inspection' | 'overhaul') => {
    const active = (items: { bikeId: number; status: Status }[]) =>
      items.filter((x) => x.bikeId === bikeId && x.status !== '完了' && x.status !== 'キャンセル')
        .length;
    return kind === 'overhaul'
      ? active(overhauls.value)
      : kind === 'maintenance'
        ? active(maintenance.value)
        : active(eventChecks.value);
  };
  const scheduleRemaining = (b: Bike, kind: 'maintenance' | 'inspection' | 'overhaul') =>
    Math.max(0, remaining(b, kind) - activeScheduleCount(b.id, kind));
  const completedCount = computed(() => histories.value.length),
    upcomingCount = computed(
      () =>
        overhauls.value.filter((x) => x.status !== '完了' && x.status !== 'キャンセル').length +
        maintenance.value.filter((x) => x.status !== '完了' && x.status !== 'キャンセル').length +
        eventChecks.value.filter((x) => x.status !== '完了' && x.status !== 'キャンセル').length
    ),
    pendingCount = computed(
      () =>
        maintenance.value.filter((x) => x.status === '案内待ち').length +
        eventChecks.value.filter((x) => x.status === '案内待ち').length
    );
  const allUpcoming = computed(() =>
    [
      ...overhauls.value
        .filter((x) => x.status !== '完了' && x.status !== 'キャンセル')
        .map((x) => ({ ...x, kind: 'オーバーホール' })),
      ...maintenance.value
        .filter((x) => x.status !== '完了' && x.status !== 'キャンセル')
        .map((x) => ({ ...x, kind: 'メンテナンス' })),
      ...eventChecks.value
        .filter((x) => x.status !== '完了' && x.status !== 'キャンセル')
        .map((x) => ({ ...x, date: x.inspectionDate, title: x.name, kind: 'イベント前点検' })),
    ].sort((a, b) => a.date.localeCompare(b.date))
  );
  const dashboardRows = computed(() =>
    sortByDate(
      allUpcoming.value,
      'dashboard',
      (x) => x.date,
      (x) => bikes.value.find((b) => b.id === x.bikeId)?.customer || '',
      (x) => x.status
    ).slice(0, 7)
  );
  const customerBikes = computed(() => bikes.value.filter((b) => b.customer === '山田 太郎'));
  const customerPortalName = '山田 太郎';
  const mypageBikeCards = computed(() =>
    customerBikes.value
      .filter((bike) => matches(searchMyPage.value, bike.model, bike.maker, planFor(bike).name))
      .map((bike) => ({
        bike,
        plan: planFor(bike),
        remaining: {
          overhaul: remaining(bike, 'overhaul'),
          maintenance: remaining(bike, 'maintenance'),
          inspection: remaining(bike, 'inspection'),
        },
      }))
  );
  const mypageScheduleRows = computed(() => {
    const customerBikeIds = new Set(customerBikes.value.map((bike) => bike.id));
    const rows = [
      ...maintenance.value
        .filter((item) => customerBikeIds.has(item.bikeId))
        .map((item) => ({
          id: `maintenance-${item.id}`,
          bikeId: item.bikeId,
          date: item.date,
          endDate: item.endDate,
          status: item.status,
          title: item.title,
          kind: 'メンテナンス',
        })),
      ...eventChecks.value
        .filter((item) => customerBikeIds.has(item.bikeId))
        .map((item) => ({
          id: `inspection-${item.id}`,
          bikeId: item.bikeId,
          date: item.inspectionDate,
          endDate: item.endDate,
          status: item.status,
          title: item.name,
          kind: '点検サポート',
        })),
    ];
    return sortByDate(
      rows.map((item) => {
        const bike = bikes.value.find((candidate) => candidate.id === item.bikeId);
        return {
          ...item,
          bikeMaker: bike?.maker ?? '',
          bikeModel: bike?.model ?? '',
          customer: bike?.customer ?? '',
        };
      }),
      'mypage',
      (item) => item.date,
      (item) => item.customer,
      (item) => item.status
    );
  });
  const bikeCountForCustomer = (name: string) =>
    bikes.value.filter((b) => b.customer === name).length;
  const bikeContractRows = computed(() =>
    sortByDate(
      bikes.value
        .filter((b) => {
          const plan = planFor(b);
          const shopName = shops.value.find((shop) => shop.id === b.shopId)?.name ?? '';
          return matches(
            searchBikes.value,
            b.customer,
            b.model,
            b.maker,
            b.serial,
            plan.name,
            shopName,
            b.receptionist
          );
        })
        .map((bike) => {
          const plan = planFor(bike);
          return {
            bike,
            planName: plan.name,
            overhaulCount: plan.overhaulCount,
            maintenanceCount: plan.maintenanceCount,
            inspectionCount: plan.inspectionCount,
            shopName: shops.value.find((shop) => shop.id === bike.shopId)?.name ?? '',
            customerBikeCount: bikeCountForCustomer(bike.customer),
            overhaulRemaining: remaining(bike, 'overhaul'),
            maintenanceRemaining: remaining(bike, 'maintenance'),
            inspectionRemaining: remaining(bike, 'inspection'),
          };
        }),
      'bikes',
      (row) => row.bike.contractStart,
      (row) => row.bike.customer
    )
  );
  const overhaulRows = computed(() =>
    sortByDate(
      overhauls.value
        .filter((work) => {
          const bike = bikes.value.find((item) => item.id === work.bikeId);
          return (
            matchesBikeFilter(work.bikeId) &&
            matches(
              searchOverhauls.value,
              bike?.customer,
              bike?.model,
              bike?.maker,
              bike?.model,
              work.date,
              work.status,
              work.title,
              work.detail,
              shops.value.find((shop) => shop.id === work.shopId)?.name,
              work.receptionist
            )
          );
        })
        .map((work) => {
          const bike = bikes.value.find((item) => item.id === work.bikeId)!;
          return { work, bike, remaining: scheduleRemaining(bike, 'overhaul') };
        }),
      'overhaul',
      (row) => row.work.date,
      (row) => row.bike.customer,
      (row) => row.work.status
    )
  );
  const overhaulBikeFilterCount = computed(
    () => overhauls.value.filter((work) => matchesBikeFilter(work.bikeId)).length
  );
  const maintenanceRows = computed(() =>
    sortByDate(
      maintenance.value
        .filter((work) => {
          const bike = bikes.value.find((item) => item.id === work.bikeId);
          return (
            matchesBikeFilter(work.bikeId) &&
            matches(
              searchMaintenance.value,
              bike?.customer,
              bike?.model,
              bike?.maker,
              bike?.model,
              work.date,
              work.status,
              work.title,
              work.detail,
              shops.value.find((shop) => shop.id === work.shopId)?.name,
              work.receptionist
            )
          );
        })
        .map((work) => {
          const bike = bikes.value.find((item) => item.id === work.bikeId)!;
          return {
            work,
            bike,
            remaining: scheduleRemaining(bike, 'maintenance'),
            shopName: shops.value.find((shop) => shop.id === work.shopId)?.name ?? '',
          };
        }),
      'maintenance',
      (row) => row.work.date,
      (row) => row.bike.customer,
      (row) => row.work.status
    )
  );
  const maintenanceBikeFilterCount = computed(
    () => maintenance.value.filter((work) => matchesBikeFilter(work.bikeId)).length
  );
  const eventInspectionRows = computed(() =>
    sortByDate(
      eventChecks.value
        .filter((event) => {
          const bike = bikes.value.find((item) => item.id === event.bikeId);
          return (
            matchesBikeFilter(event.bikeId) &&
            matches(
              searchEvents.value,
              event.name,
              event.memo,
              event.eventDate,
              event.inspectionDate,
              event.status,
              bike?.customer,
              bike?.model,
              bike?.maker,
              bike?.model,
              shops.value.find((shop) => shop.id === event.shopId)?.name,
              event.receptionist
            )
          );
        })
        .map((event) => {
          const bike = bikes.value.find((item) => item.id === event.bikeId)!;
          return {
            event,
            bike,
            remaining: scheduleRemaining(bike, 'inspection'),
            shopName: shops.value.find((shop) => shop.id === event.shopId)?.name ?? '',
          };
        }),
      'events',
      (row) => row.event.inspectionDate,
      (row) => row.bike.customer,
      (row) => row.event.status
    )
  );
  const eventInspectionCount = computed(
    () => eventChecks.value.filter((event) => matchesBikeFilter(event.bikeId)).length
  );
  function updateMaintenanceStatus(work: Work, status: Status) {
    const target = maintenance.value.find((item) => item.id === work.id);
    if (target) target.status = status;
  }

  function setPage(p: Page) {
    page.value = p;
    notice.value = '';
  }
  function addHistory(
    b: Bike,
    startDate: string,
    endDate: string,
    category: string,
    title: string,
    detail: string,
    memo = ''
  ) {
    histories.value.unshift({
      id: Math.max(0, ...histories.value.map((h) => h.id)) + 1,
      bikeId: b.id,
      startDate,
      date: endDate,
      category,
      title,
      detail,
      memo,
    });
  }
  async function startWork(kind: 'maintenance' | 'event' | 'overhaul', item: any) {
    if (item.status === '完了' || item.status === 'キャンセル') return;
    const staffMember =
      staff.value.find((x) => x.name === item.receptionist && x.shopId === item.shopId) ??
      staff.value.find((x) => x.shopId === item.shopId);
    if (!staffMember) {
      notice.value = '受付担当者を確認してください。';
      return;
    }
    const ok =
      kind === 'event'
        ? await apiAction(
            () =>
              apiPut(`/events/${item.id}`, {
                id: item.id,
                bikeId: item.bikeId,
                name: item.name,
                eventDate: dateIso(item.eventDate),
                inspectionStartDate: dateIso(item.inspectionDate),
                inspectionEndDate: dateIso(item.endDate),
                status: '作業中',
                memo: item.memo,
                shopId: item.shopId,
                receptionistId: staffMember.id,
              }),
            '点検を作業中に変更しました。'
          )
        : await apiAction(
            () =>
              apiPut(`/work/${item.id}`, {
                startDate: dateIso(item.date),
                endDate: dateIso(item.endDate) || null,
                status: '作業中',
                title: item.title,
                detail: item.detail,
                receptionistId: staffMember.id,
              }),
            '作業中に変更しました。'
          );
  }
  function startCompletion(kind: 'maintenance' | 'event' | 'overhaul', item: any) {
    completionTarget.value = { kind, id: item.id };
    completionMemo.value = '';
    const suggested = item.endDate || (kind === 'event' ? item.inspectionDate : item.date);
    completionDate.value = suggested.replace(/\//g, '-');
  }
  async function confirmCompletion() {
    if (!completionTarget.value) return;
    const { kind, id } = completionTarget.value;
    const list: any =
      kind === 'maintenance'
        ? maintenance.value
        : kind === 'event'
          ? eventChecks.value
          : overhauls.value;
    const item = list.find((x: any) => x.id === id);
    if (!item || item.status === '完了') return;
    const startDate = kind === 'event' ? item.inspectionDate : item.date;
    const endDate = completionDate.value;
    if (!endDate || endDate < startDate.replace(/\//g, '-')) {
      notice.value = '完了日は開始日以降の日付を指定してください。';
      return;
    }
    const endpoint = kind === 'event' ? `/events/${id}/complete` : `/work/${id}/complete`;
    const ok = await apiAction(
      () => apiPost(endpoint, { completedDate: endDate, memo: completionMemo.value }),
      kind === 'event'
        ? '点検サポートを完了し、点検回数を1回消費しました。'
        : kind === 'maintenance'
          ? 'メンテナンスを完了し、メンテナンス回数を1回消費しました。'
          : 'オーバーホールを完了しました。'
    );
    if (ok) {
      completionTarget.value = null;
      completionMemo.value = '';
    }
  }

  async function addContract() {
    if (USE_MOCK_DATA) {
      notice.value =
        '現在はVueモックデータ表示モードです。年間契約の登録はAPIモードで行ってください。';
      return;
    }
    const f = contractForm.value;
    const year = Number(f.start.slice(0, 4));
    if (!isAnnualContractStart(f.start)) {
      notice.value = '年間契約の開始日は12月1日にしてください。';
      return;
    }
    if (!isApplicationWithinContractYear(f.applicationDate, year)) {
      notice.value = '年間契約の受付期限は契約開始年の12月31日です。';
      return;
    }
    try {
      const existingCustomer = customers.value.find((x) => x.name === f.customer);

      let customerId: number;

      if (existingCustomer) {
        customerId = existingCustomer.id;
      } else {
        const createdCustomer = await apiPost<any>('/customers', {
          name: f.customer,
          email: '',
          phone: '',
        });

        customerId = createdCustomer.id;
      }
      const bike = await apiPost<ApiBike>('/bikes', {
        id: 0,
        customerId: customerId,
        maker: f.maker,
        model: f.model,
        year: f.year,
        serial: f.serial,
        activeContractId: null,
      });
      const person =
        staff.value.find((x) => x.shopId === f.shopId && x.name === f.receptionist) ??
        staff.value.find((x) => x.shopId === f.shopId);
      if (!person) throw new Error('受付担当者を登録してください。');
      await apiPost('/contracts', {
        bikeId: bike.id,
        planId: f.planId,
        applicationDate: f.applicationDate,
        startDate: f.start,
        shopId: f.shopId,
        receptionistId: person.id,
      });
      await reloadData();
      selectedBikeId.value = bike.id;
      newOverhaulBike.value = bike.id;
      overhaulBikeSearch.value = bikeLabel(bike.id);
      newOverhaulReceptionDate.value = f.applicationDate;
      notice.value =
        '年間契約を登録しました。次のステップとしてオーバーホールの受付・実施予定日を設定してください。';
      page.value = 'overhaul';
      showContractModal.value = false;
      showOverhaulModal.value = true;
    } catch (e) {
      notice.value = e instanceof Error ? e.message : '年間契約の登録に失敗しました。';
    }
  }
  async function addShop() {
    if (!newShopName.value.trim()) return;
    const ok = await apiAction(
      () =>
        apiPost('/shops', {
          name: newShopName.value,
          address: newShopAddress.value,
          phone: newShopPhone.value,
        }),
      '店舗を登録しました。'
    );
    if (ok) {
      newShopName.value = '';
      newShopAddress.value = '';
      newShopPhone.value = '';
      showShopModal.value = false;
    }
  }
  async function addStaff() {
    if (!newStaffName.value.trim()) return;
    const ok = await apiAction(
      () =>
        apiPost('/staff', {
          shopId: newStaffShop.value,
          name: newStaffName.value,
          role: '受付担当',
        }),
      '受付担当者を登録しました。'
    );
    if (ok) {
      newStaffName.value = '';
      showStaffModal.value = false;
    }
  }

  function openEdit(kind: 'maintenance' | 'event' | 'overhaul', item: any) {
    editTarget.value = { kind, id: item.id };
    editDate.value = (kind === 'event' ? item.inspectionDate : item.date).replace(/\//g, '-');
    editEndDate.value = (item.endDate || '').replace(/\//g, '-');
    editTitle.value = kind === 'event' ? item.name : item.title;
    editDetail.value = kind === 'event' ? item.memo : item.detail;
    editReceptionist.value = item.receptionist || '田中';
  }
  async function saveEdit() {
    if (!editTarget.value) return;
    const { kind, id } = editTarget.value;
    if (kind === 'overhaul') {
      const itemBike = overhauls.value.find((x) => x.id === id);
      const contractBike = bikes.value.find((x) => x.id === itemBike?.bikeId);
      const contractYear = Number(contractBike?.contractStart.slice(0, 4));
      if (
        !contractBike ||
        !isOverhaulSeason(editDate.value, contractYear) ||
        (editEndDate.value && !isOverhaulSeason(editEndDate.value, contractYear))
      ) {
        notice.value =
          'オーバーホールの実施予定日は契約開始年の11月1日〜翌年2月末の期間で設定してください。';
        return;
      }
    }
    const list: any =
      kind === 'maintenance'
        ? maintenance.value
        : kind === 'event'
          ? eventChecks.value
          : overhauls.value;
    const item = list.find((x: any) => x.id === id);
    if (!item) return;
    const person =
      staff.value.find((x) => x.name === editReceptionist.value && x.shopId === item.shopId) ??
      staff.value.find((x) => x.shopId === item.shopId);
    if (!person) {
      notice.value = '受付担当者を確認してください。';
      return;
    }
    if (editEndDate.value && editEndDate.value < editDate.value) {
      notice.value = '完了日は開始日以降の日付にしてください。';
      return;
    }
    const ok =
      kind === 'event'
        ? await apiAction(
            () =>
              apiPut(`/events/${id}`, {
                id,
                bikeId: item.bikeId,
                name: editTitle.value,
                eventDate: dateIso(item.eventDate),
                inspectionStartDate: editDate.value,
                inspectionEndDate: editEndDate.value || null,
                status: item.status,
                memo: editDetail.value,
                shopId: item.shopId,
                receptionistId: person.id,
              }),
            '予定内容を更新しました。契約回数は変更していません。'
          )
        : await apiAction(
            () =>
              apiPut(`/work/${id}`, {
                startDate: editDate.value,
                endDate: editEndDate.value || null,
                status: item.status,
                title: editTitle.value,
                detail: editDetail.value,
                receptionistId: person.id,
              }),
            '予定内容を更新しました。契約回数は変更していません。'
          );
    if (ok) editTarget.value = null;
  }

  async function addOverhaul() {
    if (bikeLabel(newOverhaulBike.value) !== overhaulBikeSearch.value) {
      notice.value = '顧客・自転車は候補から選択してください。';
      return;
    }
    const b = bikes.value.find((x) => x.id === newOverhaulBike.value);
    if (!b) {
      notice.value = '登録対象の自転車を選択してください。';
      return;
    }
    if (scheduleRemaining(b, 'overhaul') <= 0) {
      notice.value = `${b.maker} ${b.model} のオーバーホールは、契約上限回数に達しています。既存の予定または実施履歴を確認してください。`;
      return;
    }
    const receptionDate = newOverhaulReceptionDate.value,
      contractYear = Number(b.contractStart.slice(0, 4));
    if (!receptionDate || receptionDate > `${contractYear}-12-31`) {
      notice.value = 'オーバーホールの受付期限は契約開始年の12月31日です。';
      return;
    }
    if (
      !isOverhaulSeason(newOverhaulDate.value, contractYear) ||
      (newOverhaulEndDate.value && !isOverhaulSeason(newOverhaulEndDate.value, contractYear))
    ) {
      notice.value =
        'オーバーホールの実施予定日は契約開始年の11月1日〜翌年2月末の期間で設定してください。';
      return;
    }
    if (receptionDate > newOverhaulDate.value) {
      notice.value = '受付日は実施予定日以前の日付にしてください。同日での受付・実施は可能です。';
      return;
    }
    if (newOverhaulEndDate.value && newOverhaulEndDate.value < newOverhaulDate.value) {
      notice.value = '完了予定日は開始予定日以降にしてください。';
      return;
    }
    const person =
      staff.value.find((x) => x.shopId === b.shopId && x.name === b.receptionist) ??
      staff.value.find((x) => x.shopId === b.shopId);
    if (!person) {
      notice.value = '受付担当者を確認してください。';
      return;
    }
    if (USE_MOCK_DATA) {
      const item: Work = {
        id: Math.max(0, ...overhauls.value.map((x) => x.id)) + 1,
        bikeId: b.id,
        date: dateSlash(newOverhaulDate.value),
        endDate: newOverhaulEndDate.value ? dateSlash(newOverhaulEndDate.value) : undefined,
        status: '予定',
        title: newOverhaulTitle.value,
        detail: newOverhaulDetail.value,
        shopId: b.shopId,
        receptionist: person.name,
      };
      overhauls.value.unshift(item);
      showOverhaulModal.value = false;
      dataSourceStatus.value = 'mock';
      notice.value =
        'オーバーホール予定をVueモックデータに登録しました。画面を再読み込みすると初期データに戻ります。';
      return;
    }
    const ok = await apiAction(
      () =>
        apiPost('/overhauls', {
          bikeId: b.id,
          startDate: newOverhaulDate.value,
          endDate: newOverhaulEndDate.value || null,
          title: newOverhaulTitle.value,
          detail: newOverhaulDetail.value,
          shopId: b.shopId,
          receptionistId: person.id,
        }),
      'オーバーホール予定を登録しました。実施完了までは契約回数を消費しません。'
    );
    if (ok) showOverhaulModal.value = false;
  }
  async function addMaintenance() {
    if (bikeLabel(newWorkBike.value) !== maintenanceBikeSearch.value) {
      notice.value = '顧客・自転車は候補から選択してください。';
      return;
    }
    const b = bikes.value.find((x) => x.id === newWorkBike.value);
    if (!b) return;
    if (scheduleRemaining(b, 'maintenance') <= 0) {
      notice.value = `${b.maker} ${b.model} のメンテナンスは、契約上限回数に達しています。既存の予定または実施履歴を確認してください。`;
      return;
    }
    if (newWorkEndDate.value && newWorkEndDate.value < newWorkDate.value) {
      notice.value = '完了予定日は開始予定日以降にしてください。';
      return;
    }
    const person =
      staff.value.find((x) => x.shopId === b.shopId && x.name === b.receptionist) ??
      staff.value.find((x) => x.shopId === b.shopId);
    if (!person) {
      notice.value = '受付担当者を確認してください。';
      return;
    }
    if (USE_MOCK_DATA) {
      maintenance.value.unshift({
        id: Math.max(0, ...maintenance.value.map((x) => x.id)) + 1,
        bikeId: b.id,
        date: dateSlash(newWorkDate.value),
        endDate: newWorkEndDate.value ? dateSlash(newWorkEndDate.value) : undefined,
        status: '予定',
        title: newWorkTitle.value,
        detail: newWorkDetail.value,
        shopId: b.shopId,
        receptionist: person.name,
      });
      showMaintenanceModal.value = false;
      notice.value = 'メンテナンス計画をVueモックデータに登録しました。';
      return;
    }
    const ok = await apiAction(
      () =>
        apiPost('/maintenance', {
          bikeId: b.id,
          startDate: newWorkDate.value,
          endDate: newWorkEndDate.value || null,
          title: newWorkTitle.value,
          detail: newWorkDetail.value,
          shopId: b.shopId,
          receptionistId: person.id,
        }),
      'メンテナンス計画を追加しました。実施完了までは回数を消費しません。'
    );
    if (ok) showMaintenanceModal.value = false;
  }
  async function addEvent() {
    if (bikeLabel(newEventBike.value) !== eventBikeSearch.value) {
      notice.value = '顧客・自転車は候補から選択してください。';
      return;
    }
    const b = bikes.value.find((x) => x.id === newEventBike.value);
    if (!b || !newEventName.value.trim()) {
      notice.value = 'イベント名を入力してください。';
      return;
    }
    if (scheduleRemaining(b, 'inspection') <= 0) {
      notice.value = `${b.maker} ${b.model} の点検サポートは、契約上限回数に達しています。既存の予定または実施履歴を確認してください。`;
      return;
    }
    if (newInspectionEndDate.value && newInspectionEndDate.value < newInspectionDate.value) {
      notice.value = '点検完了予定日は開始予定日以降にしてください。';
      return;
    }
    const person =
      staff.value.find((x) => x.shopId === b.shopId && x.name === b.receptionist) ??
      staff.value.find((x) => x.shopId === b.shopId);
    if (!person) {
      notice.value = '受付担当者を確認してください。';
      return;
    }
    if (USE_MOCK_DATA) {
      eventChecks.value.unshift({
        id: Math.max(0, ...eventChecks.value.map((x) => x.id)) + 1,
        bikeId: b.id,
        name: newEventName.value,
        eventDate: dateSlash(newEventDate.value),
        inspectionDate: dateSlash(newInspectionDate.value),
        endDate: newInspectionEndDate.value ? dateSlash(newInspectionEndDate.value) : undefined,
        status: '予定',
        memo: '目標イベントに向けた点検サポート',
        shopId: b.shopId,
        receptionist: person.name,
      });
      newEventName.value = '';
      showEventModal.value = false;
      notice.value = 'イベント・点検予定をVueモックデータに登録しました。';
      return;
    }
    const ok = await apiAction(
      () =>
        apiPost('/events', {
          bikeId: b.id,
          name: newEventName.value,
          eventDate: newEventDate.value,
          inspectionStartDate: newInspectionDate.value,
          inspectionEndDate: newInspectionEndDate.value || null,
          memo: '目標イベントに向けた点検サポート',
          shopId: b.shopId,
          receptionistId: person.id,
        }),
      '目標イベントと点検予定を登録しました。'
    );
    if (ok) {
      newEventName.value = '';
      showEventModal.value = false;
    }
  }

  function toggleRole() {
    role.value = role.value === 'staff' ? 'customer' : 'staff';
    page.value = role.value === 'customer' ? 'mypage' : 'dashboard';
  }

  function sendMail() {
    showMail.value = false;
    notice.value = `${mailTarget.value} 様への「${mailKind.value}」を送信した想定のデモです（実際には送信されません）。`;
  }
</script>
<template>
  <div class="app-shell">
    <AppSidebar :page="page" :role="role" @navigate="setPage" @toggle-role="toggleRole" />
    <main class="main">
      <AppHeader :page-title="pageTitles[page]" :today="today" />
      <div class="content">
        <div class="data-source-indicator" :class="dataSourceStatus">
          <strong>{{
            dataSourceStatus === 'mock'
              ? 'MOCK DATA'
              : dataSourceStatus === 'error'
                ? 'API ERROR'
                : dataSourceLabel
          }}</strong><span>{{
            dataSourceStatus === 'mock'
              ? 'Vue内のサンプルデータを表示中（API通信なし）'
              : dataSourceStatus === 'error'
                ? 'API接続に失敗。モックデータへの切替は行っていません'
                : 'バックエンドAPIのデータを使用'
          }}</span>
        </div>
        <div v-if="notice" class="notice">
          <span>{{ notice }}</span><button @click="notice = ''">×</button>
        </div>
        <DashboardView
          v-if="page === 'dashboard'"
          :bikes="bikes"
          :dashboard-rows="dashboardRows"
          :upcoming-count="upcomingCount"
          :pending-count="pendingCount"
          :completed-count="completedCount"
          :customer-sort-label="sortLabel('dashboard', 'customer')"
          :date-sort-label="sortLabel('dashboard', 'date')"
          :status-sort-label="sortLabel('dashboard', 'status')"
          @sort="toggleListSort('dashboard', $event)"
          @open-customer="openCustomerDetail"
        />
        <ShopsView
          v-if="page === 'shops'"
          :shops="shops"
          :staff="staff"
          :search-text="searchShops"
          @update:search-text="searchShops = $event"
          @add-shop="showShopModal = true"
          @add-staff="showStaffModal = true"
        />
        <PlansView v-else-if="page === 'plans'" :plans="plans" @add-plan="showPlanModal = true" />
        <template v-else-if="page === 'bikes'">
          <BikeContractsView
            :customers-count="customers.length"
            :bikes-count="bikes.length"
            :search-text="searchBikes"
            :sort-label="sortLabel('bikes', 'customer')"
            :rows="bikeContractRows"
            @update:search-text="searchBikes = $event"
            @sort="toggleListSort('bikes', 'customer')"
            @add-contract="showContractModal = true"
            @open-customer="openCustomerDetail"
            @open-bike="openBikeDetail"
          />
        </template>
        <template v-else-if="page === 'overhaul'">
          <OverhaulView
            :rows="overhaulRows"
            :count="overhaulBikeFilterCount"
            :customer-filter-text="customerFilterText"
            :bike-filter-text="bikeFilterText"
            :selected-bike-filter="selectedBikeFilter"
            :search-text="searchOverhauls"
            :customer-sort-label="sortLabel('overhaul', 'customer')"
            :date-sort-label="sortLabel('overhaul', 'date')"
            :status-sort-label="sortLabel('overhaul', 'status')"
            @update:customer-filter-text="customerFilterText = $event"
            @update:bike-filter-text="bikeFilterText = $event"
            @update:search-text="searchOverhauls = $event"
            @clear-filters="clearBikeNameFilters"
            @sort="toggleListSort('overhaul', $event)"
            @open-customer="openCustomerDetail"
            @edit="openEdit('overhaul', $event)"
            @start-work="startWork('overhaul', $event)"
            @complete="startCompletion('overhaul', $event)"
          />
        </template>

        <template v-else-if="page === 'maintenance'">
          <MaintenanceView
            :rows="maintenanceRows"
            :count="maintenanceBikeFilterCount"
            :customer-filter-text="customerFilterText"
            :bike-filter-text="bikeFilterText"
            :selected-bike-filter="selectedBikeFilter"
            :search-text="searchMaintenance"
            :customer-sort-label="sortLabel('maintenance', 'customer')"
            :date-sort-label="sortLabel('maintenance', 'date')"
            :status-sort-label="sortLabel('maintenance', 'status')"
            @update:customer-filter-text="customerFilterText = $event"
            @update:bike-filter-text="bikeFilterText = $event"
            @update:search-text="searchMaintenance = $event"
            @clear-filters="clearBikeNameFilters"
            @sort="toggleListSort('maintenance', $event)"
            @open-customer="openCustomerDetail"
            @status-change="updateMaintenanceStatus"
            @edit="openEdit('maintenance', $event)"
            @start-work="startWork('maintenance', $event)"
            @complete="startCompletion('maintenance', $event)"
          />
        </template>

        <template v-else-if="page === 'events'">
          <EventInspectionView
            :rows="eventInspectionRows"
            :count="eventInspectionCount"
            :customer-filter-text="customerFilterText"
            :bike-filter-text="bikeFilterText"
            :selected-bike-filter="selectedBikeFilter"
            :search-text="searchEvents"
            :customer-sort-label="sortLabel('events', 'customer')"
            :date-sort-label="sortLabel('events', 'date')"
            :status-sort-label="sortLabel('events', 'status')"
            @update:customer-filter-text="customerFilterText = $event"
            @update:bike-filter-text="bikeFilterText = $event"
            @update:search-text="searchEvents = $event"
            @clear-filters="clearBikeNameFilters"
            @sort="toggleListSort('events', $event)"
            @open-customer="openCustomerDetail"
            @edit="openEdit('event', $event)"
            @start-work="startWork('event', $event)"
            @complete="startCompletion('event', $event)"
          />
        </template>

        <template v-else-if="page === 'history'">
          <HistoryView
            :bikes="bikes"
            :selected-bike-id="selectedBikeId"
            :selected-bike="selectedBike"
            :rows="historyRows"
            :count="histories.filter((h) => h.bikeId === selectedBikeId).length"
            :search-text="searchHistory"
            :date-sort-label="sortLabel('history', 'date')"
            @update:selected-bike-id="selectedBikeId = $event"
            @update:search-text="searchHistory = $event"
            @sort="toggleListSort('history', 'date')"
          />
        </template>

        <template v-else-if="page === 'messages'">
          <MessagesView
            :rows="messageRows"
            :pending-count="pendingCount"
            :search-text="searchMessages"
            :customer-sort-label="sortLabel('messages', 'customer')"
            :date-sort-label="sortLabel('messages', 'date')"
            @update:search-text="searchMessages = $event"
            @sort="toggleListSort('messages', $event)"
            @open-customer="openCustomerDetail"
            @compose-mail="composeMail"
          />
        </template>

        <template v-else>
          <MypageView
            :customer-name="customerPortalName"
            :bike-cards="mypageBikeCards"
            :schedule-rows="mypageScheduleRows"
            :search-text="searchMyPage"
            :date-sort-label="sortLabel('mypage', 'date')"
            :status-sort-label="sortLabel('mypage', 'status')"
            @update:search-text="searchMyPage = $event"
            @sort="toggleListSort('mypage', $event)"
          />
        </template>
      </div>
    </main>
  </div>
  <div v-if="showBikeDetail" class="modal-backdrop" @click.self="showBikeDetail = false">
    <div class="modal modal-wide bike-detail-modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">CONTRACT DETAILS</p>
          <h2>契約詳細</h2>
          <p class="sub">選択した自転車の契約内容とサービス残回数</p>
        </div>
        <button class="close" @click="showBikeDetail = false">×</button>
      </div>
      <div class="detail-grid">
        <div>
          <label>顧客</label><b>{{ selectedBike.customer }}</b>
        </div>
        <div>
          <label>自転車</label><b>{{ selectedBike.maker }} {{ selectedBike.model }}</b>
        </div>
        <div>
          <label>プラン</label><b>{{ planFor(selectedBike).name }}</b>
        </div>
        <div>
          <label>契約期間</label
          ><b>{{ selectedBike.contractStart }}〜{{ selectedBike.contractEnd }}</b>
        </div>
        <div>
          <label>担当店舗・受付</label
          ><b
            >{{ shops.find((x) => x.id === selectedBike.shopId)?.name }} /
            {{ selectedBike.receptionist }}</b
          >
        </div>
        <div>
          <label>オーバーホール残</label
          ><b class="remaining large"
            >{{ remaining(selectedBike, 'overhaul') }} /
            {{ planFor(selectedBike).overhaulCount }}回</b
          >
        </div>
        <div>
          <label>メンテナンス残</label
          ><b class="remaining large"
            >{{ remaining(selectedBike, 'maintenance') }} /
            {{ planFor(selectedBike).maintenanceCount }}回</b
          >
        </div>
        <div>
          <label>点検サポート残</label
          ><b class="remaining large"
            >{{ remaining(selectedBike, 'inspection') }} /
            {{ planFor(selectedBike).inspectionCount }}回</b
          >
        </div>
        <div>
          <label>オーバーホール完了日</label><b>{{ selectedBike.overhaulCompleted || '未実施' }}</b>
        </div>
        <div>
          <label>車体メーカー・モデル</label
          ><b>{{ selectedBike.maker }} {{ selectedBike.model }}</b>
        </div>
        <div>
          <label>年式</label><b>{{ selectedBike.year }}年</b>
        </div>
        <div>
          <label>シリアル番号</label><b>{{ selectedBike.serial || '未登録' }}</b>
        </div>
      </div>
      <div class="modal-actions">
        <button class="outline" @click="showBikeDetail = false">閉じる</button>
      </div>
    </div>
  </div>
  <div v-if="showCustomerDetail" class="modal-backdrop" @click.self="showCustomerDetail = false">
    <div class="modal modal-wide customer-detail-modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">CUSTOMER PROFILE</p>
          <h2>{{ selectedCustomerName }} 様の顧客情報</h2>
          <p class="sub">管理画面から顧客マイページと同等の契約・予定・履歴を確認できます。</p>
        </div>
        <button class="close" @click="showCustomerDetail = false">×</button>
      </div>
      <section class="customer-profile-summary">
        <div>
          <label>顧客名</label><b>{{ selectedCustomerName }}</b>
        </div>
        <div>
          <label>メールアドレス</label><b>{{ selectedCustomer?.email || '未登録' }}</b>
        </div>
        <div>
          <label>契約自転車</label><b>{{ selectedCustomerBikes.length }}台</b>
        </div>
      </section>
      <section class="customer-profile-section">
        <div class="panel-head">
          <div>
            <h3>契約中の自転車・年間プラン</h3>
            <p>自転車ごとの契約期間とサービス残回数</p>
          </div>
        </div>
        <div v-for="b in selectedCustomerBikes" :key="b.id" class="customer-profile-bike">
          <div class="customer-profile-bike-title">
            <b>{{ b.maker }} {{ b.model }}</b
            ><span class="status success">契約中</span>
          </div>
          <div class="customer-bike-actions customer-bike-register-actions">
            <button
              class="primary small"
              :disabled="scheduleRemaining(b, 'overhaul') <= 0"
              @click="openCustomerBikeRegistration('overhaul', b)"
            >
              ＋ オーバーホール登録</button
            ><button
              class="primary small"
              :disabled="scheduleRemaining(b, 'maintenance') <= 0"
              @click="openCustomerBikeRegistration('maintenance', b)"
            >
              ＋ メンテナンス登録</button
            ><button
              class="primary small"
              :disabled="scheduleRemaining(b, 'inspection') <= 0"
              @click="openCustomerBikeRegistration('event', b)"
            >
              ＋ 点検登録
            </button>
          </div>
          <div class="customer-bike-actions customer-bike-manage-actions">
            <button class="outline small" @click="openCustomerBikePage('overhaul', b.id)">
              オーバーホール管理画面</button
            ><button class="outline small" @click="openCustomerBikePage('maintenance', b.id)">
              メンテナンス管理画面</button
            ><button class="outline small" @click="openCustomerBikePage('events', b.id)">
              点検管理画面</button
            ><button class="outline small" @click="openCustomerBikePage('history', b.id)">
              作業履歴
            </button>
          </div>
          <div class="detail-grid">
            <div>
              <label>プラン</label><b>{{ planFor(b).name }}</b>
            </div>
            <div>
              <label>契約期間</label><b>{{ b.contractStart }}〜{{ b.contractEnd }}</b>
            </div>
            <div>
              <label>担当店舗・受付</label
              ><b>{{ shops.find((x) => x.id === b.shopId)?.name }} / {{ b.receptionist }}</b>
            </div>
            <div>
              <label>オーバーホール残</label
              ><b class="remaining"
                >{{ remaining(b, 'overhaul') }} / {{ planFor(b).overhaulCount }}回</b
              >
            </div>
            <div>
              <label>メンテナンス残</label
              ><b class="remaining"
                >{{ remaining(b, 'maintenance') }} / {{ planFor(b).maintenanceCount }}回</b
              >
            </div>
            <div>
              <label>点検サポート残</label
              ><b class="remaining"
                >{{ remaining(b, 'inspection') }} / {{ planFor(b).inspectionCount }}回</b
              >
            </div>
          </div>
        </div>
        <div v-if="!selectedCustomerBikes.length" class="empty-state">
          契約中の自転車はありません。
        </div>
      </section>
      <section class="customer-profile-section">
        <div class="panel-head">
          <div>
            <h3>メンテナンス・点検予定</h3>
            <p>対象自転車・作業期間・状態</p>
          </div>
        </div>
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>対象自転車</th>
                <th>作業区分</th>
                <th>内容</th>
                <th>開始日〜完了日</th>
                <th>状態</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="w in selectedCustomerWorks" :key="w.kind + '-' + w.id">
                <td>{{ selectedCustomerBikes.find((b) => b.id === w.bikeId)?.model }}</td>
                <td>{{ w.kind }}</td>
                <td>{{ w.title }}</td>
                <td>{{ w.date }}〜{{ w.endDate || '未定' }}</td>
                <td>
                  <span class="status info">{{ w.status }}</span>
                </td>
              </tr>
              <tr v-if="!selectedCustomerWorks.length">
                <td colspan="5" class="empty-state">予定は登録されていません。</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
      <section class="customer-profile-section">
        <div class="panel-head">
          <div>
            <h3>作業履歴</h3>
            <p>完了した作業と記録メモ</p>
          </div>
        </div>
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>実施日</th>
                <th>対象自転車</th>
                <th>作業区分</th>
                <th>作業内容</th>
                <th>メモ</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="h in selectedCustomerHistories" :key="h.id">
                <td>{{ h.date }}</td>
                <td>{{ selectedCustomerBikes.find((b) => b.id === h.bikeId)?.model }}</td>
                <td>{{ h.category }}</td>
                <td>{{ h.title }}</td>
                <td>{{ h.memo || '—' }}</td>
              </tr>
              <tr v-if="!selectedCustomerHistories.length">
                <td colspan="5" class="empty-state">作業履歴はありません。</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
      <div class="modal-actions">
        <button class="outline" @click="showCustomerDetail = false">閉じる</button>
      </div>
    </div>
  </div>
  <div v-if="showContractModal" class="modal-backdrop" @click.self="showContractModal = false">
    <div class="modal modal-wide">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW ANNUAL CONTRACT</p>
          <h2>年間契約を登録</h2>
        </div>
        <button class="close" @click="showContractModal = false">×</button>
      </div>
      <div class="detail-grid">
        <div>
          <label>顧客</label
          ><input
            v-model="contractForm.customer"
            list="customer-options"
            placeholder="顧客名を入力して検索"
          />
        </div>
        <div><label>メーカー</label><input v-model="contractForm.maker" /></div>
        <div><label>モデル</label><input v-model="contractForm.model" /></div>
        <div><label>年式</label><input v-model.number="contractForm.year" type="number" /></div>
        <div><label>車体番号</label><input v-model="contractForm.serial" /></div>
        <div>
          <label>契約プラン</label
          ><select v-model.number="contractForm.planId">
            <option v-for="p in plans" :key="p.id" :value="p.id">
              {{ p.name }}（OH {{ p.overhaulCount }} / メンテ {{ p.maintenanceCount }} / 点検
              {{ p.inspectionCount }}）
            </option>
          </select>
        </div>
        <div>
          <label>契約受付日</label><input v-model="contractForm.applicationDate" type="date" />
        </div>
        <div><label>契約開始日</label><input v-model="contractForm.start" type="date" /></div>
        <div>
          <label>担当店舗</label
          ><select v-model.number="contractForm.shopId">
            <option v-for="shop in shops" :key="shop.id" :value="shop.id">{{ shop.name }}</option>
          </select>
        </div>
        <div>
          <label>受付担当者</label
          ><select v-model="contractForm.receptionist">
            <option
              v-for="person in staff.filter((x) => x.shopId === contractForm.shopId)"
              :key="person.id"
            >
              {{ person.name }}
            </option>
          </select>
        </div>
      </div>
      <div class="modal-note">
        契約受付期限は契約開始年の12月31日。契約期間は12月1日〜翌年11月30日です。
      </div>
      <div class="modal-actions">
        <button class="outline" @click="showContractModal = false">キャンセル</button
        ><button class="primary" @click="addContract">契約を登録してOH予定へ進む</button>
      </div>
    </div>
  </div>
  <div v-if="showShopModal" class="modal-backdrop" @click.self="showShopModal = false">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW STORE</p>
          <h2>店舗を登録</h2>
        </div>
        <button class="close" @click="showShopModal = false">×</button>
      </div>
      <label>店舗名</label><input v-model="newShopName" placeholder="例：大阪店" /><label
        >住所</label
      ><input v-model="newShopAddress" placeholder="住所" /><label>電話番号</label
      ><input v-model="newShopPhone" placeholder="電話番号" />
      <div class="modal-actions">
        <button class="outline" @click="showShopModal = false">キャンセル</button
        ><button class="primary" @click="addShop">店舗を登録</button>
      </div>
    </div>
  </div>
  <div v-if="showStaffModal" class="modal-backdrop" @click.self="showStaffModal = false">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW STAFF</p>
          <h2>受付担当者を登録</h2>
        </div>
        <button class="close" @click="showStaffModal = false">×</button>
      </div>
      <label>所属店舗</label
      ><select v-model.number="newStaffShop">
        <option v-for="shop in shops" :key="shop.id" :value="shop.id">
          {{ shop.name }}
        </option></select
      ><label>担当者名</label><input v-model="newStaffName" placeholder="担当者名" />
      <div class="modal-actions">
        <button class="outline" @click="showStaffModal = false">キャンセル</button
        ><button class="primary" @click="addStaff">担当者を登録</button>
      </div>
    </div>
  </div>
  <div v-if="showPlanModal" class="modal-backdrop" @click.self="showPlanModal = false">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW PLAN</p>
          <h2>プランを追加</h2>
        </div>
        <button class="close" @click="showPlanModal = false">×</button>
      </div>
      <label>プラン名</label
      ><input v-model="newPlan.name" placeholder="例：競技者向けプラン" /><label
        >オーバーホール回数</label
      ><input v-model.number="newPlan.overhaulCount" type="number" min="0" step="1" /><label
        >メンテナンス回数</label
      ><input v-model.number="newPlan.maintenanceCount" type="number" min="0" step="1" /><label
        >点検サポート回数</label
      ><input v-model.number="newPlan.inspectionCount" type="number" min="0" step="1" />
      <div class="modal-note">回数はそれぞれ独立して設定します。</div>
      <div class="modal-actions">
        <button class="outline" @click="showPlanModal = false">キャンセル</button
        ><button class="primary" @click="addPlan">プランを追加</button>
      </div>
    </div>
  </div>
  <div v-if="showOverhaulModal" class="modal-backdrop" @click.self="showOverhaulModal = false">
    <div class="modal modal-wide">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW OVERHAUL</p>
          <h2>オーバーホールを登録</h2>
        </div>
        <button class="close" @click="showOverhaulModal = false">×</button>
      </div>
      <div class="detail-grid">
        <div>
          <label>顧客・自転車</label
          ><input
            v-model="overhaulBikeSearch"
            @change="setBikeFromLabel(overhaulBikeSearch, 'overhaul')"
            list="bike-options"
            placeholder="顧客名・自転車名を入力して検索"
          />
        </div>
        <div><label>受付日</label><input v-model="newOverhaulReceptionDate" type="date" /></div>
        <div><label>作業開始予定日</label><input v-model="newOverhaulDate" type="date" /></div>
        <div><label>作業完了予定日</label><input v-model="newOverhaulEndDate" type="date" /></div>
        <div><label>作業名</label><input v-model="newOverhaulTitle" /></div>
        <div><label>作業内容</label><input v-model="newOverhaulDetail" /></div>
      </div>
      <div class="modal-note">
        受付期限は契約開始年の12月31日。実施期間は契約開始年11月〜翌年2月末です。新規登録可能：{{
          bikes.find((b) => b.id === newOverhaulBike)
            ? scheduleRemaining(
                bikes.find((b) => b.id === newOverhaulBike)!,
                'overhaul'
              )
            : 0
        }}
        / 契約枠
        {{
          bikes.find((b) => b.id === newOverhaulBike)
            ? planFor(bikes.find((b) => b.id === newOverhaulBike)!).overhaulCount
            : 0
        }}回（予定・完了を含む）
      </div>
      <div class="modal-actions">
        <button class="outline" @click="showOverhaulModal = false">キャンセル</button
        ><button
          class="primary"
          :disabled="
            !bikes.find((b) => b.id === newOverhaulBike) ||
            scheduleRemaining(
              bikes.find((b) => b.id === newOverhaulBike)!,
              'overhaul'
            ) <= 0
          "
          @click="addOverhaul"
        >
          オーバーホールを登録
        </button>
      </div>
    </div>
  </div>
  <div
    v-if="showMaintenanceModal"
    class="modal-backdrop"
    @click.self="showMaintenanceModal = false"
  >
    <div class="modal modal-wide">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW MAINTENANCE</p>
          <h2>メンテナンス計画を追加</h2>
        </div>
        <button class="close" @click="showMaintenanceModal = false">×</button>
      </div>
      <div class="detail-grid">
        <div>
          <label>顧客・自転車</label
          ><input
            v-model="maintenanceBikeSearch"
            @change="setBikeFromLabel(maintenanceBikeSearch, 'maintenance')"
            list="bike-options"
            placeholder="顧客名・自転車名を入力して検索"
          />
        </div>
        <div><label>作業開始予定日</label><input v-model="newWorkDate" type="date" /></div>
        <div><label>作業完了予定日</label><input v-model="newWorkEndDate" type="date" /></div>
        <div><label>計画名</label><input v-model="newWorkTitle" /></div>
        <div><label>作業内容</label><input v-model="newWorkDetail" /></div>
      </div>
      <div class="modal-note">
        新規登録可能：{{
          bikes.find((b) => b.id === newWorkBike)
            ? scheduleRemaining(
                bikes.find((b) => b.id === newWorkBike)!,
                'maintenance'
              )
            : 0
        }}
        / 契約枠
        {{
          bikes.find((b) => b.id === newWorkBike)
            ? planFor(bikes.find((b) => b.id === newWorkBike)!).maintenanceCount
            : 0
        }}回（予定・完了を含む）
      </div>
      <div class="modal-actions">
        <button class="outline" @click="showMaintenanceModal = false">キャンセル</button
        ><button
          class="primary"
          :disabled="
            !bikes.find((b) => b.id === newWorkBike) ||
            scheduleRemaining(
              bikes.find((b) => b.id === newWorkBike)!,
              'maintenance'
            ) <= 0
          "
          @click="addMaintenance"
        >
          計画を登録
        </button>
      </div>
    </div>
  </div>
  <div v-if="showEventModal" class="modal-backdrop" @click.self="showEventModal = false">
    <div class="modal modal-wide">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW EVENT & INSPECTION</p>
          <h2>イベント・点検を登録</h2>
        </div>
        <button class="close" @click="showEventModal = false">×</button>
      </div>
      <div class="detail-grid">
        <div>
          <label>顧客・自転車</label
          ><input
            v-model="eventBikeSearch"
            @change="setBikeFromLabel(eventBikeSearch, 'event')"
            list="bike-options"
            placeholder="顧客名・自転車名を入力して検索"
          />
        </div>
        <div>
          <label>イベント名</label
          ><input v-model="newEventName" placeholder="例：年間メインイベント" />
        </div>
        <div><label>イベント開催日</label><input v-model="newEventDate" type="date" /></div>
        <div><label>点検開始予定日</label><input v-model="newInspectionDate" type="date" /></div>
        <div><label>点検完了予定日</label><input v-model="newInspectionEndDate" type="date" /></div>
      </div>
      <div class="modal-note">
        新規登録可能：{{
          bikes.find((b) => b.id === newEventBike)
            ? scheduleRemaining(
                bikes.find((b) => b.id === newEventBike)!,
                'inspection'
              )
            : 0
        }}
        / 契約枠
        {{
          bikes.find((b) => b.id === newEventBike)
            ? planFor(bikes.find((b) => b.id === newEventBike)!).inspectionCount
            : 0
        }}回（予定・完了を含む）
      </div>
      <div class="modal-actions">
        <button class="outline" @click="showEventModal = false">キャンセル</button
        ><button
          class="primary"
          :disabled="
            !bikes.find((b) => b.id === newEventBike) ||
            scheduleRemaining(
              bikes.find((b) => b.id === newEventBike)!,
              'inspection'
            ) <= 0
          "
          @click="addEvent"
        >
          イベント・点検を登録
        </button>
      </div>
    </div>
  </div>
  <div v-if="completionTarget" class="modal-backdrop" @click.self="completionTarget = null">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">WORK COMPLETION</p>
          <h2>作業完了・メモ登録</h2>
        </div>
        <button class="close" @click="completionTarget = null">×</button>
      </div>
      <p class="sub">作業内容や交換部品、調整箇所、次回確認事項などを記録できます。</p>
      <label>作業完了日</label><input v-model="completionDate" type="date" /><label
        >作業時のメモ</label
      ><textarea
        v-model="completionMemo"
        rows="6"
        placeholder="例：チェーン・スプロケットを洗浄。リアブレーキパッド交換。変速調整済み。次回はタイヤ摩耗を確認。"
      ></textarea>
      <div class="modal-note">
        メモは作業履歴に保存され、後から「メンテナンス履歴」画面で確認できます。
      </div>
      <div class="modal-actions">
        <button class="outline" @click="completionTarget = null">キャンセル</button
        ><button class="primary" @click="confirmCompletion">作業完了として保存</button>
      </div>
    </div>
  </div>
  <div v-if="editTarget" class="modal-backdrop" @click.self="editTarget = null">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">EDIT SCHEDULE</p>
          <h2>予定の変更</h2>
        </div>
        <button class="close" @click="editTarget = null">×</button>
      </div>
      <label>作業開始日</label><input v-model="editDate" type="date" /><label>作業完了予定日</label
      ><input v-model="editEndDate" type="date" /><label>予定名・イベント名</label
      ><input v-model="editTitle" /><label>内容・メモ</label><input v-model="editDetail" /><label
        >受付担当者</label
      ><select v-model="editReceptionist">
        <option v-for="person in staff" :key="person.id">{{ person.name }}</option>
      </select>
      <div class="modal-note">
        開始日・完了予定日を変更できます。完了日は開始日以降にしてください。契約回数は作業完了時のみ消費します。
      </div>
      <div class="modal-actions">
        <button class="outline" @click="editTarget = null">キャンセル</button
        ><button class="primary" @click="saveEdit">変更を保存</button>
      </div>
    </div>
  </div>
  <div v-if="showMail" class="modal-backdrop" @click.self="showMail = false">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">EMAIL PREVIEW</p>
          <h2>メンテナンス案内メール</h2>
        </div>
        <button class="close" @click="showMail = false">×</button>
      </div>
      <label>送信先</label
      ><select v-model="mailTarget">
        <option v-for="c in customers" :key="c.name">{{ c.name }}</option></select
      ><label>件名</label><input v-model="mailKind" /><label>本文</label>
      <div class="mail-body">
        {{ mailTarget }}
        様<br /><br />いつもご利用いただきありがとうございます。<br />メンテナンス・点検予定についてご案内いたします。<br /><br />ご希望の日程がございましたら店舗までご連絡ください。<br /><br />今後ともよろしくお願いいたします。
      </div>
      <div class="modal-note">※ モックのため、実際のメール送信は行われません。</div>
      <div class="modal-actions">
        <button class="outline" @click="showMail = false">閉じる</button
        ><button class="primary" @click="sendMail">送信（デモ）</button>
      </div>
    </div>
  </div>
  <datalist id="customer-options">
    <option v-for="c in customers" :key="c.name" :value="c.name" /></datalist
  ><datalist id="bike-options">
    <option v-for="b in bikes" :key="b.id" :value="bikeLabel(b.id)" />
  </datalist>
</template>
