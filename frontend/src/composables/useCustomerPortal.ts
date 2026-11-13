import { computed, type Ref } from 'vue';
import type { Bike, EventCheck, Plan, Work } from '../types';
import type { ContractService } from './useContractUsage';
import { matches, type ListSorter } from '../utils/list';

interface CustomerPortalContext {
  useMockData: boolean;
  portalProfile: Ref<{ name: string; customerId?: number } | null>;
  bikes: Ref<Bike[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  searchText: Ref<string>;
  planFor: (bike: Bike) => Plan;
  remaining: (bike: Bike, kind: ContractService) => number;
  sortRows: ListSorter;
}

/** 顧客マイページ向けの契約カードと予定一覧を組み立てます。 */
export function useCustomerPortal(ctx: CustomerPortalContext) {
  const bikesById = computed(() => new Map(ctx.bikes.value.map((bike) => [bike.id, bike])));
  const customerBikes = computed(() =>
    ctx.bikes.value.filter((bike) =>
      ctx.useMockData
        ? bike.customer === '山田 太郎'
        : bike.customerId === ctx.portalProfile.value?.customerId
    )
  );
  const customerName = computed(() => ctx.portalProfile.value?.name ?? '山田 太郎');

  const bikeCards = computed(() =>
    customerBikes.value
      .filter((bike) =>
        matches(ctx.searchText.value, bike.model, bike.maker, ctx.planFor(bike).name)
      )
      .map((bike) => ({
        bike,
        plan: ctx.planFor(bike),
        remaining: {
          overhaul: ctx.remaining(bike, 'overhaul'),
          maintenance: ctx.remaining(bike, 'maintenance'),
          inspection: ctx.remaining(bike, 'inspection'),
        },
      }))
  );

  const scheduleRows = computed(() => {
    const customerBikeIds = new Set(customerBikes.value.map((bike) => bike.id));
    const rows = [
      ...ctx.overhauls.value
        .filter((item) => customerBikeIds.has(item.bikeId))
        .map((item) => ({
          id: `overhaul-${item.id}`,
          bikeId: item.bikeId,
          date: item.date,
          endDate: item.endDate,
          status: item.status,
          title: item.title,
          kind: 'オーバーホール',
          isAdHoc: item.isAdHoc,
        })),
      ...ctx.maintenance.value
        .filter((item) => customerBikeIds.has(item.bikeId))
        .map((item) => ({
          id: `maintenance-${item.id}`,
          bikeId: item.bikeId,
          date: item.date,
          endDate: item.endDate,
          status: item.status,
          title: item.title,
          kind: 'メンテナンス',
          isAdHoc: item.isAdHoc,
        })),
      ...ctx.eventChecks.value
        .filter((item) => customerBikeIds.has(item.bikeId))
        .map((item) => ({
          id: `inspection-${item.id}`,
          bikeId: item.bikeId,
          date: item.inspectionDate,
          endDate: item.endDate,
          status: item.status,
          title: item.name,
          kind: '点検サポート',
          isAdHoc: item.isAdHoc,
        })),
    ];

    const rowsWithBike = rows.map((item) => {
      const bike = bikesById.value.get(item.bikeId);
      return {
        ...item,
        bikeMaker: bike?.maker ?? '',
        bikeModel: bike?.model ?? '',
        customer: bike?.customer ?? '',
      };
    });

    return ctx.sortRows(
      rowsWithBike,
      'mypage',
      (item) => item.date,
      (item) => item.customer,
      (item) => item.status
    );
  });

  return { customerBikes, customerName, bikeCards, scheduleRows };
}
