import { computed, type Ref } from 'vue';
import type { Bike, Plan } from '../types';

/** 自転車の選択状態と、契約プランの参照をまとめます。 */
export function useBikeCatalog(
  bikes: Ref<Bike[]>,
  plans: Ref<Plan[]>,
  selectedBikeId: Ref<number>
) {
  const plansById = computed(() => new Map(plans.value.map((plan) => [plan.id, plan])));

  function planFor(bike: Bike): Plan {
    return (
      plansById.value.get(bike.planId) ??
      plans.value[0] ?? {
        id: 0,
        name: '未設定',
        overhaulCount: 0,
        maintenanceCount: 0,
        inspectionCount: 0,
      }
    );
  }

  const selectedBike = computed<Bike>(
    () =>
      bikes.value.find((bike) => bike.id === selectedBikeId.value) ??
      bikes.value[0] ?? {
        id: 0,
        customer: '',
        maker: '',
        model: '',
        year: 0,
        serial: '',
        planId: 0,
        contractStart: '',
        contractEnd: '',
        overhaulUsed: 0,
        maintenanceUsed: 0,
        inspectionUsed: 0,
        overhaulCompleted: '',
        shopId: 0,
        receptionist: '',
      }
  );

  return { planFor, selectedBike };
}
