import { computed, type ComputedRef, type Ref } from 'vue';
import type { Bike, Plan } from '../types';
import type { ApiShop } from '../services/apiMappers';
import type { ContractService } from './useContractUsage';

interface BikeDetailsContext {
  selectedBike: ComputedRef<Bike>;
  shops: Ref<ApiShop[]>;
  planFor: (bike: Bike) => Plan;
  remaining: (bike: Bike, kind: ContractService) => number;
}

/** 自転車詳細モーダル用の契約・店舗・残回数表示データを組み立てます。 */
export function useBikeDetails(ctx: BikeDetailsContext) {
  const bikeDetails = computed(() => {
    const bike = ctx.selectedBike.value;
    return {
      bike,
      plan: ctx.planFor(bike),
      shopName: ctx.shops.value.find((shop) => shop.id === bike.shopId)?.name ?? '',
      remaining: {
        overhaul: ctx.remaining(bike, 'overhaul'),
        maintenance: ctx.remaining(bike, 'maintenance'),
        inspection: ctx.remaining(bike, 'inspection'),
      },
    };
  });

  return { bikeDetails };
}
