import { computed, type Ref } from 'vue';
import type { Bike, EventCheck, History, Work } from '../types';
import { matches, type ListSorter } from '../utils/list';

interface ActivityListsContext {
  bikes: Ref<Bike[]>;
  histories: Ref<History[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  selectedBikeId: Ref<number>;
  searchHistory: Ref<string>;
  searchMessages: Ref<string>;
  sortRows: ListSorter;
}

/** 履歴画面と、案内待ちのメッセージ一覧を組み立てます。 */
export function useActivityLists(ctx: ActivityListsContext) {
  const bikesById = computed(() => new Map(ctx.bikes.value.map((bike) => [bike.id, bike])));
  const selectedBikeHistoryCount = computed(
    () =>
      ctx.histories.value.filter((history) => history.bikeId === ctx.selectedBikeId.value).length
  );

  const historyRows = computed(() =>
    ctx.sortRows(
      ctx.histories.value.filter(
        (history) =>
          history.bikeId === ctx.selectedBikeId.value &&
          matches(
            ctx.searchHistory.value,
            history.date,
            history.category,
            history.title,
            history.detail,
            history.memo
          )
      ),
      'history',
      (history) => history.date
    )
  );

  const messageRows = computed(() => {
    const pendingItems = [
      ...ctx.maintenance.value
        .filter((item) => item.status === '案内待ち')
        .map((item) => ({
          id: item.id,
          bikeId: item.bikeId,
          date: item.date,
          title: item.title,
          kind: 'メンテナンス' as const,
        })),
      ...ctx.eventChecks.value
        .filter((item) => item.status === '案内待ち')
        .map((item) => ({
          id: item.id,
          bikeId: item.bikeId,
          date: item.inspectionDate,
          title: item.name,
          kind: '点検サポート' as const,
        })),
    ];

    const rows = pendingItems
      .map((item) => {
        const bike = bikesById.value.get(item.bikeId);
        return {
          id: `${item.kind}-${item.id}`,
          bikeId: item.bikeId,
          customer: bike?.customer ?? '',
          bikeModel: bike?.model ?? '',
          kind: item.kind,
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

  return { historyRows, selectedBikeHistoryCount, messageRows };
}
