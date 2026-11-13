import { computed, type ComputedRef, type Ref } from 'vue';
import type { Bike, EventCheck, Plan, Work } from '../types';

export type ContractService = 'maintenance' | 'inspection' | 'overhaul';
export type ScheduleCapacity = { bike?: Bike; available: number; limit: number };

interface ContractUsageContext {
  bikes: Ref<Bike[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  planFor: (bike: Bike) => Plan;
}

/** 契約の残回数と、未完了予定を考慮した登録可能回数を計算します。 */
export function useContractUsage(ctx: ContractUsageContext) {
  function limitFor(bike: Bike, kind: ContractService) {
    const plan = ctx.planFor(bike);
    return kind === 'maintenance'
      ? plan.maintenanceCount
      : kind === 'inspection'
        ? plan.inspectionCount
        : plan.overhaulCount;
  }

  function remaining(bike: Bike, kind: ContractService) {
    const used =
      kind === 'maintenance'
        ? bike.maintenanceUsed
        : kind === 'inspection'
          ? bike.inspectionUsed
          : bike.overhaulUsed;
    return Math.max(0, limitFor(bike, kind) - used);
  }

  function activeScheduleCount(bikeId: number, kind: ContractService) {
    const items =
      kind === 'overhaul'
        ? ctx.overhauls.value
        : kind === 'maintenance'
          ? ctx.maintenance.value
          : ctx.eventChecks.value;
    return items.filter(
      (item) =>
        item.bikeId === bikeId &&
        item.status !== '完了' &&
        item.status !== 'キャンセル' &&
        item.status !== '作業中止'
    ).length;
  }

  function scheduleRemaining(bike: Bike, kind: ContractService) {
    return Math.max(0, remaining(bike, kind) - activeScheduleCount(bike.id, kind));
  }

  function scheduleCapacityFor(
    bikeId: Ref<number>,
    kind: ContractService
  ): ComputedRef<ScheduleCapacity> {
    return computed(() => {
      const bike = ctx.bikes.value.find((item) => item.id === bikeId.value);
      return {
        bike,
        available: bike ? scheduleRemaining(bike, kind) : 0,
        limit: bike ? limitFor(bike, kind) : 0,
      };
    });
  }

  return { remaining, activeScheduleCount, scheduleRemaining, scheduleCapacityFor };
}
