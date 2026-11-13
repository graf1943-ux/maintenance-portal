import { computed, type Ref } from 'vue';
import type { Bike, EventCheck, History, Status, Work } from '../types';
import type { ListSorter } from '../utils/list';
import { useDashboardReminders } from './useDashboardReminders';
export type { DashboardReminder } from './useDashboardReminders';

export type DashboardRow = {
  id: number;
  bikeId: number;
  kind: string;
  date: string;
  endDate?: string;
  title: string;
  status: Status;
  isAdHoc?: boolean;
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
  item.status !== '完了' && item.status !== 'キャンセル' && item.status !== '作業中止';

/** ダッシュボードの件数と、予定一覧に表示する統合行を作ります。 */
export function useDashboardData(ctx: DashboardContext) {
  const bikesById = computed(() => new Map(ctx.bikes.value.map((bike) => [bike.id, bike])));
  const { todayLabel, reminders } = useDashboardReminders({
    overhauls: ctx.overhauls,
    maintenance: ctx.maintenance,
    eventChecks: ctx.eventChecks,
  });

  const completedCount = computed(() => ctx.histories.value.length);

  const allUpcoming = computed<DashboardRow[]>(() =>
    [
      ...ctx.overhauls.value.filter(isActive).map((item) => ({
        id: item.id,
        bikeId: item.bikeId,
        date: item.date,
        endDate: item.endDate,
        status: item.status,
        isAdHoc: item.isAdHoc,
        title: item.title,
        kind: 'オーバーホール',
      })),
      ...ctx.maintenance.value.filter(isActive).map((item) => ({
        id: item.id,
        bikeId: item.bikeId,
        date: item.date,
        endDate: item.endDate,
        status: item.status,
        isAdHoc: item.isAdHoc,
        title: item.title,
        kind: 'メンテナンス',
      })),
      ...ctx.eventChecks.value.filter(isActive).map((item) => ({
        id: item.id,
        bikeId: item.bikeId,
        date: item.inspectionDate,
        endDate: item.endDate,
        status: item.status,
        isAdHoc: item.isAdHoc,
        title: item.name,
        kind: 'イベント前点検',
      })),
    ].sort((a, b) => a.date.localeCompare(b.date))
  );

  const upcomingCount = computed(() => allUpcoming.value.length);

  const pendingCount = computed(() => reminders.value.length);

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

  return { completedCount, upcomingCount, pendingCount, dashboardRows, reminders, todayLabel };
}
