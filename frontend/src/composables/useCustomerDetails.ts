import { computed, type Ref } from 'vue';
import type { Bike, EventCheck, History, Plan, Status, Work } from '../types';
import type { ApiShop } from '../services/apiMappers';
import type { ContractService } from './useContractUsage';

export type CustomerWorkRow = {
  id: number;
  bikeId: number;
  date: string;
  endDate?: string;
  status: Status;
  title: string;
  kind: string;
};

interface CustomerDetailsContext {
  selectedCustomerName: Ref<string>;
  selectedCustomerId: Ref<number | null>;
  customers: Ref<{ id: number; name: string; email: string; phone?: string }[]>;
  bikes: Ref<Bike[]>;
  shops: Ref<ApiShop[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  histories: Ref<History[]>;
  planFor: (bike: Bike) => Plan;
  remaining: (bike: Bike, kind: ContractService) => number;
  scheduleRemaining: (bike: Bike, kind: ContractService) => number;
}

/** 顧客詳細モーダルに表示する顧客・自転車・作業・履歴を集計します。 */
export function useCustomerDetails(ctx: CustomerDetailsContext) {
  const customer = computed(() =>
    ctx.selectedCustomerId.value !== null
      ? ctx.customers.value.find((item) => item.id === ctx.selectedCustomerId.value)
      : ctx.customers.value.find((item) => item.name === ctx.selectedCustomerName.value)
  );

  const customerBikes = computed(() =>
    ctx.bikes.value.filter((bike) => bike.customer === ctx.selectedCustomerName.value)
  );

  const customerBikeCards = computed(() =>
    customerBikes.value.map((bike) => {
      const plan = ctx.planFor(bike);
      return {
        bike,
        plan,
        shopName: ctx.shops.value.find((shop) => shop.id === bike.shopId)?.name ?? '',
        remaining: {
          overhaul: ctx.remaining(bike, 'overhaul'),
          maintenance: ctx.remaining(bike, 'maintenance'),
          inspection: ctx.remaining(bike, 'inspection'),
        },
        scheduleRemaining: {
          overhaul: ctx.scheduleRemaining(bike, 'overhaul'),
          maintenance: ctx.scheduleRemaining(bike, 'maintenance'),
          inspection: ctx.scheduleRemaining(bike, 'inspection'),
        },
      };
    })
  );

  const customerWorks = computed<CustomerWorkRow[]>(() => {
    const bikeIds = new Set(customerBikes.value.map((bike) => bike.id));
    return [
      ...ctx.overhauls.value
        .filter((work) => bikeIds.has(work.bikeId))
        .map((work) => ({
          id: work.id,
          bikeId: work.bikeId,
          date: work.date,
          endDate: work.endDate,
          status: work.status,
          title: work.title,
          kind: 'オーバーホール',
        })),
      ...ctx.maintenance.value
        .filter((work) => bikeIds.has(work.bikeId))
        .map((work) => ({
          id: work.id,
          bikeId: work.bikeId,
          date: work.date,
          endDate: work.endDate,
          status: work.status,
          title: work.title,
          kind: 'メンテナンス',
        })),
      ...ctx.eventChecks.value
        .filter((event) => bikeIds.has(event.bikeId))
        .map((event) => ({
          id: event.id,
          bikeId: event.bikeId,
          date: event.inspectionDate,
          endDate: event.endDate,
          status: event.status,
          title: event.name,
          kind: '点検サポート',
        })),
    ].sort((a, b) => a.date.localeCompare(b.date));
  });

  const customerHistories = computed(() => {
    const bikeIds = new Set(customerBikes.value.map((bike) => bike.id));
    return ctx.histories.value
      .filter((history) => bikeIds.has(history.bikeId))
      .sort((a, b) => b.date.localeCompare(a.date));
  });

  return { customer, customerBikes, customerBikeCards, customerWorks, customerHistories };
}
