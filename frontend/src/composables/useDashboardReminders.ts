import { computed, onMounted, onUnmounted, ref, type Ref } from 'vue';
import type { EventCheck, Work } from '../types';
import { daysUntilLocalDate } from '../utils/date';

export type DashboardReminder = {
  id: string;
  bikeId: number;
  kind: string;
  date: string;
  title: string;
  reason: string;
  daysUntil: number;
};

interface DashboardRemindersContext {
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
}

/** 案内待ち予定の日付監視と、ダッシュボード用リマインダーを管理します。 */
export function useDashboardReminders(ctx: DashboardRemindersContext) {
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
      const daysUntil = daysUntilLocalDate(date, today.value);
      if (daysUntil === null || daysUntil > 7) return;
      const reason =
        daysUntil < 0
          ? `${dateLabel}を${Math.abs(daysUntil)}日超過`
          : daysUntil === 0
            ? `${dateLabel}当日`
            : `${dateLabel}まであと${daysUntil}日`;
      rows.push({ id, bikeId, kind, date, title, reason, daysUntil });
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

  const todayLabel = computed(() => {
    const date = today.value;
    const weekdays = ['日', '月', '火', '水', '木', '金', '土'];
    return `${date.getFullYear()}年${date.getMonth() + 1}月${date.getDate()}日（${weekdays[date.getDay()]}）`;
  });

  return { today, todayLabel, reminders };
}
