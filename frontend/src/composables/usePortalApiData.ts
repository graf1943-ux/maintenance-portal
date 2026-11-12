import { computed, type Ref } from 'vue';
import { apiGet } from '../api';
import type { Bike, EventCheck, History, Plan, Work } from '../types';
import {
  mapBike,
  mapEvent,
  mapHistory,
  mapWork,
  type ApiBike,
  type ApiCustomer,
  type ApiEvent,
  type ApiHistory,
  type ApiShop,
  type ApiStaff,
  type ApiWork,
} from '../services/apiMappers';

type DataSourceStatus = 'mock' | 'api' | 'error';
type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;

interface PortalApiDataContext {
  useMockData: boolean;
  dataSourceStatus: Ref<DataSourceStatus>;
  role: Ref<'staff' | 'customer'>;
  notice: Ref<string>;
  customers: Ref<ApiCustomer[]>;
  shops: Ref<ApiShop[]>;
  staff: Ref<ApiStaff[]>;
  plans: Ref<Plan[]>;
  bikes: Ref<Bike[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  histories: Ref<History[]>;
}

/** APIデータの再取得・画面用変換と共通更新処理を担当します。 */
export function usePortalApiData(ctx: PortalApiDataContext) {
  const dataSourceTitle = computed(() =>
    ctx.dataSourceStatus.value === 'mock'
      ? 'MOCK DATA'
      : ctx.dataSourceStatus.value === 'error'
        ? 'API ERROR'
        : 'API MODE'
  );
  const dataSourceMessage = computed(() =>
    ctx.dataSourceStatus.value === 'mock'
      ? 'Vue内のサンプルデータを表示中（API通信なし）'
      : ctx.dataSourceStatus.value === 'error'
        ? 'API接続に失敗。モックデータへの切替は行っていません'
        : 'バックエンドAPIのデータを使用'
  );

  async function reloadData() {
    if (ctx.useMockData) return;
    const isStaff = ctx.role.value === 'staff';
    const [customers, shops, staff, plans, bikes, overhauls, maintenance, events, histories] =
      await Promise.all([
        apiGet<ApiCustomer[]>('/customers'),
        isStaff ? apiGet<ApiShop[]>('/shops') : Promise.resolve([]),
        isStaff ? apiGet<ApiStaff[]>('/staff') : Promise.resolve([]),
        apiGet<Plan[]>('/plans'),
        apiGet<ApiBike[]>('/bikes'),
        apiGet<ApiWork[]>('/overhauls'),
        apiGet<ApiWork[]>('/maintenance'),
        apiGet<ApiEvent[]>('/events'),
        apiGet<ApiHistory[]>('/history'),
      ]);

    ctx.customers.value = customers;
    ctx.shops.value = shops;
    ctx.staff.value = staff;
    ctx.plans.value = plans;
    const receptionistName = (id: number) =>
      ctx.staff.value.find((member) => member.id === id)?.name ?? '';
    ctx.bikes.value = bikes.map((item) => mapBike(item, receptionistName));
    ctx.overhauls.value = overhauls.map((item) => mapWork(item, receptionistName));
    ctx.maintenance.value = maintenance.map((item) => mapWork(item, receptionistName));
    ctx.eventChecks.value = events.map((item) => mapEvent(item, receptionistName));
    ctx.histories.value = histories.map(mapHistory);
  }

  async function apiAction(action: () => Promise<unknown>, success: string) {
    if (ctx.useMockData) {
      ctx.notice.value =
        '現在はVueモックデータ表示モードです。登録・更新操作はAPIモードで行ってください。';
      return false;
    }
    try {
      await action();
      await reloadData();
      ctx.dataSourceStatus.value = 'api';
      ctx.notice.value = success;
      return true;
    } catch (error) {
      ctx.dataSourceStatus.value = 'error';
      ctx.notice.value = error instanceof Error ? error.message : 'API処理に失敗しました。';
      return false;
    }
  }

  return { dataSourceTitle, dataSourceMessage, reloadData, apiAction };
}
