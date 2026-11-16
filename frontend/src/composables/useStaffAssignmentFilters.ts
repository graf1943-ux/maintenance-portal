import { computed, ref, watch, type Ref } from 'vue';
import type { ApiShop, ApiStaff } from '../services/apiMappers';
import type { Bike, EventCheck, History, Work } from '../types';

type AssignedItem = {
  shopId: number;
  receptionist: string;
  receptionistId?: number;
};

interface StaffAssignmentFilterContext {
  shops: Ref<ApiShop[]>;
  staff: Ref<ApiStaff[]>;
  bikes: Ref<Bike[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  histories: Ref<History[]>;
}

/** 店舗・担当者の選択状態と、スタッフ画面向けの絞り込みを管理します。 */
export function useStaffAssignmentFilters(ctx: StaffAssignmentFilterContext) {
  const selectedShopId = ref<number | null>(null);
  const selectedStaffId = ref<number | null>(null);
  const selectedStaff = computed(() =>
    ctx.staff.value.find((member) => member.id === selectedStaffId.value)
  );

  watch(selectedShopId, () => {
    selectedStaffId.value = null;
  });

  function matchesAssignment(item: AssignedItem) {
    if (selectedShopId.value !== null && item.shopId !== selectedShopId.value) return false;
    if (selectedStaffId.value === null) return true;

    const member = selectedStaff.value;
    if (!member || item.shopId !== member.shopId) return false;
    return item.receptionistId && item.receptionistId > 0
      ? item.receptionistId === member.id
      : item.receptionist === member.name;
  }

  const filteredBikes = computed(() => ctx.bikes.value.filter(matchesAssignment));
  const filteredOverhauls = computed(() => ctx.overhauls.value.filter(matchesAssignment));
  const filteredMaintenance = computed(() => ctx.maintenance.value.filter(matchesAssignment));
  const filteredEventChecks = computed(() => ctx.eventChecks.value.filter(matchesAssignment));
  const filteredHistories = computed(() =>
    ctx.histories.value.filter((history) => {
      if (selectedShopId.value === null && selectedStaffId.value === null) return true;

      const report = history.report;
      const reportShopId =
        report?.shopId && report.shopId > 0
          ? report.shopId
          : ctx.shops.value.find((shop) => shop.name === report?.shopName)?.id;
      if (selectedShopId.value !== null && reportShopId !== selectedShopId.value) return false;
      if (selectedStaffId.value === null) return true;

      const member = selectedStaff.value;
      if (!member || reportShopId !== member.shopId) return false;
      return report?.receptionistId && report.receptionistId > 0
        ? report.receptionistId === member.id
        : report?.staffName === member.name;
    })
  );

  return {
    selectedShopId,
    selectedStaffId,
    filteredBikes,
    filteredOverhauls,
    filteredMaintenance,
    filteredEventChecks,
    filteredHistories,
    matchesBikeAssignment: matchesAssignment,
  };
}
