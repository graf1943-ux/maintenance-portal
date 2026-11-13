import { computed, ref, type Ref } from 'vue';
import type { Bike } from '../types';
import { matches } from '../utils/list';

export function useBikeListFilters(bikes: Ref<Bike[]>) {
  const selectedBikeFilter = ref<number | null>(null);
  const customerFilterText = ref('');
  const bikeFilterText = ref('');
  const selectedBikeFilterName = computed(() => {
    const bike = bikes.value.find((item) => item.id === selectedBikeFilter.value);
    return bike ? `${bike.maker} ${bike.model}` : '';
  });

  function matchesBikeFilter(bikeId: number) {
    const bike = bikes.value.find((item) => item.id === bikeId);
    return (
      (!selectedBikeFilter.value || bikeId === selectedBikeFilter.value) &&
      matches(customerFilterText.value, bike?.customer) &&
      matches(bikeFilterText.value, bike?.maker, bike?.model, bike?.serial)
    );
  }

  function clearBikeNameFilters() {
    customerFilterText.value = '';
    bikeFilterText.value = '';
    selectedBikeFilter.value = null;
  }

  return {
    selectedBikeFilter,
    selectedBikeFilterName,
    customerFilterText,
    bikeFilterText,
    matchesBikeFilter,
    clearBikeNameFilters,
  };
}
