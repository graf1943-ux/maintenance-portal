import { computed, type Ref } from 'vue';
import type { Bike, EventCheck, Plan, Work } from '../types';
import type { ApiShop } from '../services/apiMappers';
import { matches, matchesEventSearch, matchesWorkSearch, type ListSorter } from '../utils/list';
import type { ContractService } from './useContractUsage';

interface StaffListsContext {
  bikes: Ref<Bike[]>;
  shops: Ref<ApiShop[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  searchBikes: Ref<string>;
  searchOverhauls: Ref<string>;
  searchMaintenance: Ref<string>;
  searchEvents: Ref<string>;
  matchesBikeFilter: (bikeId: number) => boolean;
  planFor: (bike: Bike) => Plan;
  remaining: (bike: Bike, kind: ContractService) => number;
  scheduleRemaining: (bike: Bike, kind: ContractService) => number;
  sortRows: ListSorter;
}

/** スタッフ向け契約・作業一覧の表示行、検索結果、件数を作ります。 */
export function useStaffLists(ctx: StaffListsContext) {
  const bikesById = computed(() => new Map(ctx.bikes.value.map((bike) => [bike.id, bike])));
  const shopNamesById = computed(
    () => new Map(ctx.shops.value.map((shop) => [shop.id, shop.name]))
  );
  const bikeCountsByCustomer = computed(() => {
    const counts = new Map<string, number>();
    for (const bike of ctx.bikes.value) {
      counts.set(bike.customer, (counts.get(bike.customer) ?? 0) + 1);
    }
    return counts;
  });

  const bikeContractRows = computed(() =>
    ctx.sortRows(
      ctx.bikes.value
        .filter((bike) => {
          const plan = ctx.planFor(bike);
          const shopName = shopNamesById.value.get(bike.shopId) ?? '';
          return matches(
            ctx.searchBikes.value,
            bike.customer,
            bike.model,
            bike.maker,
            bike.serial,
            plan.name,
            shopName,
            bike.receptionist
          );
        })
        .map((bike) => {
          const plan = ctx.planFor(bike);
          return {
            bike,
            planName: plan.name,
            overhaulCount: plan.overhaulCount,
            maintenanceCount: plan.maintenanceCount,
            inspectionCount: plan.inspectionCount,
            shopName: shopNamesById.value.get(bike.shopId) ?? '',
            customerBikeCount: bikeCountsByCustomer.value.get(bike.customer) ?? 0,
            overhaulRemaining: ctx.remaining(bike, 'overhaul'),
            maintenanceRemaining: ctx.remaining(bike, 'maintenance'),
            inspectionRemaining: ctx.remaining(bike, 'inspection'),
          };
        }),
      'bikes',
      (row) => row.bike.contractStart,
      (row) => row.bike.customer
    )
  );

  const overhaulRows = computed(() =>
    ctx.sortRows(
      ctx.overhauls.value
        .filter((work) => {
          const bike = bikesById.value.get(work.bikeId);
          return (
            ctx.matchesBikeFilter(work.bikeId) &&
            matchesWorkSearch(
              ctx.searchOverhauls.value,
              work,
              bike,
              shopNamesById.value.get(work.shopId)
            )
          );
        })
        .map((work) => {
          const bike = bikesById.value.get(work.bikeId)!;
          return { work, bike, remaining: ctx.scheduleRemaining(bike, 'overhaul') };
        }),
      'overhaul',
      (row) => row.work.date,
      (row) => row.bike.customer,
      (row) => row.work.status
    )
  );
  const overhaulBikeFilterCount = computed(
    () => ctx.overhauls.value.filter((work) => ctx.matchesBikeFilter(work.bikeId)).length
  );

  const maintenanceRows = computed(() =>
    ctx.sortRows(
      ctx.maintenance.value
        .filter((work) => {
          const bike = bikesById.value.get(work.bikeId);
          return (
            ctx.matchesBikeFilter(work.bikeId) &&
            matchesWorkSearch(
              ctx.searchMaintenance.value,
              work,
              bike,
              shopNamesById.value.get(work.shopId)
            )
          );
        })
        .map((work) => {
          const bike = bikesById.value.get(work.bikeId)!;
          return {
            work,
            bike,
            remaining: ctx.scheduleRemaining(bike, 'maintenance'),
            shopName: shopNamesById.value.get(work.shopId) ?? '',
          };
        }),
      'maintenance',
      (row) => row.work.date,
      (row) => row.bike.customer,
      (row) => row.work.status
    )
  );
  const maintenanceBikeFilterCount = computed(
    () => ctx.maintenance.value.filter((work) => ctx.matchesBikeFilter(work.bikeId)).length
  );

  const eventInspectionRows = computed(() =>
    ctx.sortRows(
      ctx.eventChecks.value
        .filter((event) => {
          const bike = bikesById.value.get(event.bikeId);
          return (
            ctx.matchesBikeFilter(event.bikeId) &&
            matchesEventSearch(
              ctx.searchEvents.value,
              event,
              bike,
              shopNamesById.value.get(event.shopId)
            )
          );
        })
        .map((event) => {
          const bike = bikesById.value.get(event.bikeId)!;
          return {
            event,
            bike,
            remaining: ctx.scheduleRemaining(bike, 'inspection'),
            shopName: shopNamesById.value.get(event.shopId) ?? '',
          };
        }),
      'events',
      (row) => row.event.inspectionDate,
      (row) => row.bike.customer,
      (row) => row.event.status
    )
  );
  const eventInspectionCount = computed(
    () => ctx.eventChecks.value.filter((event) => ctx.matchesBikeFilter(event.bikeId)).length
  );

  return {
    bikeContractRows,
    overhaulRows,
    overhaulBikeFilterCount,
    maintenanceRows,
    maintenanceBikeFilterCount,
    eventInspectionRows,
    eventInspectionCount,
  };
}
