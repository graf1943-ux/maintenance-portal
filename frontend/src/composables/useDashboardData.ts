import { computed, type Ref } from 'vue';
import type { Bike, EventCheck, History, Status, Work } from '../types';
import type { ListSorter } from '../utils/list';

export type DashboardRow = {
  id: number;
  bikeId: number;
  kind: string;
  date: string;
  endDate?: string;
  title: string;
  status: Status;
};

interface DashboardContext {
  bikes: Ref<Bike[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  histories: Ref<History[]>;
  sortRows: ListSorter;
}

const isActive = (item: { status: Status }) =>
  item.status !== '完了' && item.status !== 'キャンセル';

/** ダッシュボードの件数と、予定一覧に表示する統合行を作ります。 */
export function useDashboardData(ctx: DashboardContext) {
  const bikesById = computed(() => new Map(ctx.bikes.value.map((bike) => [bike.id, bike])));
  const completedCount = computed(() => ctx.histories.value.length);
  const pendingCount = computed(
    () =>
      ctx.maintenance.value.filter((item) => item.status === '案内待ち').length +
      ctx.eventChecks.value.filter((item) => item.status === '案内待ち').length
  );

  const allUpcoming = computed<DashboardRow[]>(() =>
    [
      ...ctx.overhauls.value.filter(isActive).map((item) => ({
        id: item.id,
        bikeId: item.bikeId,
        date: item.date,
        endDate: item.endDate,
        status: item.status,
        title: item.title,
        kind: 'オーバーホール',
      })),
      ...ctx.maintenance.value.filter(isActive).map((item) => ({
        id: item.id,
        bikeId: item.bikeId,
        date: item.date,
        endDate: item.endDate,
        status: item.status,
        title: item.title,
        kind: 'メンテナンス',
      })),
      ...ctx.eventChecks.value.filter(isActive).map((item) => ({
        id: item.id,
        bikeId: item.bikeId,
        date: item.inspectionDate,
        endDate: item.endDate,
        status: item.status,
        title: item.name,
        kind: 'イベント前点検',
      })),
    ].sort((a, b) => a.date.localeCompare(b.date))
  );

  const upcomingCount = computed(() => allUpcoming.value.length);

  const dashboardRows = computed(() =>
    ctx
      .sortRows(
        allUpcoming.value,
        'dashboard',
        (item) => item.date,
        (item) => bikesById.value.get(item.bikeId)?.customer ?? '',
        (item) => item.status
      )
      .slice(0, 7)
  );

  return { completedCount, upcomingCount, pendingCount, dashboardRows };
}
