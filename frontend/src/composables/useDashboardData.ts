import { computed, onMounted, onUnmounted, ref, type Ref } from 'vue';
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
  isAdHoc?: boolean;
};

export type DashboardReminder = {
  id: string;
  bikeId: number;
  kind: string;
  date: string;
  title: string;
  reason: string;
  daysUntil: number;
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

function daysUntilDate(dateValue: string, today: Date): number | null {
  const match = /^(\d{4})[/-](\d{1,2})[/-](\d{1,2})$/.exec(dateValue);
  if (!match) return null;

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const targetDate = new Date(year, month - 1, day);
  if (
    targetDate.getFullYear() !== year ||
    targetDate.getMonth() !== month - 1 ||
    targetDate.getDate() !== day
  ) {
    return null;
  }

  const todayStart = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  return Math.round((targetDate.getTime() - todayStart.getTime()) / 86_400_000);
}

/** ダッシュボードの件数と、予定一覧に表示する統合行を作ります。 */
export function useDashboardData(ctx: DashboardContext) {
  const bikesById = computed(() => new Map(ctx.bikes.value.map((bike) => [bike.id, bike])));
  const today = ref(new Date());
  let midnightTimer: number | undefined;

  const scheduleMidnightRefresh = () => {
    const tomorrow = new Date(
      today.value.getFullYear(),
      today.value.getMonth(),
      today.value.getDate() + 1
    );
    midnightTimer = window.setTimeout(() => {
      today.value = new Date();
      scheduleMidnightRefresh();
    }, tomorrow.getTime() - Date.now());
  };

  onMounted(scheduleMidnightRefresh);
  onUnmounted(() => {
    if (midnightTimer !== undefined) window.clearTimeout(midnightTimer);
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

  const reminders = computed<DashboardReminder[]>(() => {
    const rows: DashboardReminder[] = [];

    const addReminder = (
      id: string,
      bikeId: number,
      kind: string,
      date: string,
      title: string,
      dateLabel: string
    ) => {
      const daysUntil = daysUntilDate(date, today.value);
      if (daysUntil === null || daysUntil > 7) return;
      const reason =
        daysUntil < 0
          ? `${dateLabel}を${Math.abs(daysUntil)}日超過`
          : daysUntil === 0
            ? `${dateLabel}当日`
            : `${dateLabel}まであと${daysUntil}日`;
      rows.push({
        id,
        bikeId,
        kind,
        date,
        title,
        reason,
        daysUntil,
      });
    };

    for (const work of ctx.overhauls.value) {
      if (work.status === '案内待ち') {
        addReminder(
          `overhaul-${work.id}`,
          work.bikeId,
          'オーバーホール',
          work.date,
          work.title,
          '作業開始日'
        );
      }
    }

    for (const work of ctx.maintenance.value) {
      if (work.status === '案内待ち') {
        addReminder(
          `maintenance-${work.id}`,
          work.bikeId,
          'メンテナンス',
          work.date,
          work.title,
          '作業開始日'
        );
      }
    }

    for (const event of ctx.eventChecks.value) {
      if (event.status === '案内待ち') {
        addReminder(
          `event-${event.id}`,
          event.bikeId,
          'イベント前点検',
          event.inspectionDate,
          event.name,
          '点検開始日'
        );
      }
    }

    return rows.sort((a, b) => a.daysUntil - b.daysUntil || a.title.localeCompare(b.title));
  });

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

  return { completedCount, upcomingCount, pendingCount, dashboardRows, reminders, today };
}
