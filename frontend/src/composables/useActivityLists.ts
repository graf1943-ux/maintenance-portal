import { computed, type Ref } from 'vue';
import type { Bike, History } from '../types';
import { matches, type ListSorter } from '../utils/list';
import type { DashboardReminder } from './useDashboardData';

interface ActivityListsContext {
  bikes: Ref<Bike[]>;
  histories: Ref<History[]>;
  reminders: Ref<DashboardReminder[]>;
  selectedBikeFilter: Ref<number | null>;
  searchHistory: Ref<string>;
  searchMessages: Ref<string>;
  sortRows: ListSorter;
}

/** 履歴画面と、案内待ちのメッセージ一覧を組み立てます。 */
export function useActivityLists(ctx: ActivityListsContext) {
  const bikesById = computed(() => new Map(ctx.bikes.value.map((bike) => [bike.id, bike])));

  const historyRows = computed(() => {
    const rows = ctx.histories.value
      .filter((history) => {
        const bike = bikesById.value.get(history.bikeId);
        return (
          matches(
            ctx.searchHistory.value,
            history.date,
            history.category,
            history.title,
            history.detail,
            history.memo,
            bike?.customer,
            bike?.maker,
            bike?.model
          ) &&
          (ctx.selectedBikeFilter.value === null || history.bikeId === ctx.selectedBikeFilter.value)
        );
      })
      .map((history) => ({ ...history, bike: bikesById.value.get(history.bikeId) }));

    return ctx.sortRows(rows, 'history', (history) => history.date);
  });
  const historyCount = computed(() => historyRows.value.length);

  const messageRows = computed(() => {
    const rows = ctx.reminders.value
      .map((item) => {
        const bike = bikesById.value.get(item.bikeId);
        return {
          id: item.id,
          bikeId: item.bikeId,
          customer: bike?.customer ?? '',
          bikeModel: bike?.model ?? '',
          kind: item.kind === 'イベント前点検' ? '点検サポート' : item.kind,
          title: item.title,
          date: item.date,
        };
      })
      .filter((item) =>
        matches(
          ctx.searchMessages.value,
          item.customer,
          item.bikeModel,
          item.date,
          item.title,
          item.kind
        )
      );

    return ctx.sortRows(
      rows,
      'messages',
      (item) => item.date,
      (item) => item.customer
    );
  });

  return { historyRows, historyCount, messageRows };
}
