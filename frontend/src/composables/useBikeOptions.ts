import type { Ref } from 'vue';
import type { Bike } from '../types';

type BikeTarget = 'overhaul' | 'maintenance' | 'event';

interface BikeOptionsContext {
  bikes: Ref<Bike[]>;
  overhaulBikeId: Ref<number>;
  maintenanceBikeId: Ref<number>;
  eventBikeId: Ref<number>;
}

/** 自転車選択用の表示ラベル生成とラベルからIDへの反映をまとめます。 */
export function useBikeOptions(ctx: BikeOptionsContext) {
  function bikeLabel(id: number) {
    const bike = ctx.bikes.value.find((item) => item.id === id);
    return bike ? `${bike.customer} / ${bike.maker} ${bike.model}` : '';
  }

  function setBikeFromLabel(label: string, target: BikeTarget) {
    const bike = ctx.bikes.value.find((item) => bikeLabel(item.id) === label);
    if (!bike) return;

    if (target === 'overhaul') ctx.overhaulBikeId.value = bike.id;
    else if (target === 'maintenance') ctx.maintenanceBikeId.value = bike.id;
    else ctx.eventBikeId.value = bike.id;
  }

  return { bikeLabel, setBikeFromLabel };
}
