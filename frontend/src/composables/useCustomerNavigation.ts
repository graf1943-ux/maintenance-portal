import type { Ref } from 'vue';
import type { Bike, Page } from '../types';

type CustomerBikePage = 'overhaul' | 'maintenance' | 'events' | 'history';
type RegistrationKind = 'overhaul' | 'maintenance' | 'event';

interface CustomerNavigationContext {
  bikes: Ref<Bike[]>;
  selectedBikeId: Ref<number>;
  selectedBikeFilter: Ref<number | null>;
  customerFilterText: Ref<string>;
  bikeFilterText: Ref<string>;
  searchOverhauls: Ref<string>;
  searchMaintenance: Ref<string>;
  searchEvents: Ref<string>;
  searchHistory: Ref<string>;
  page: Ref<Page>;
  showCustomerDetail: Ref<boolean>;
  newOverhaulBike: Ref<number>;
  newOverhaulReceptionDate: Ref<string>;
  newOverhaulDate: Ref<string>;
  newOverhaulEndDate: Ref<string>;
  overhaulBikeSearch: Ref<string>;
  newWorkBike: Ref<number>;
  newWorkDate: Ref<string>;
  newWorkEndDate: Ref<string>;
  maintenanceBikeSearch: Ref<string>;
  newEventBike: Ref<number>;
  newEventName: Ref<string>;
  newEventDate: Ref<string>;
  newInspectionDate: Ref<string>;
  newInspectionEndDate: Ref<string>;
  eventBikeSearch: Ref<string>;
  showOverhaulModal: Ref<boolean>;
  showMaintenanceModal: Ref<boolean>;
  showEventModal: Ref<boolean>;
  notice: Ref<string>;
  bikeLabel: (bikeId: number) => string;
}

/** 顧客詳細から対象自転車を引き継ぐ画面遷移と登録フォーム初期化を管理します。 */
export function useCustomerNavigation(ctx: CustomerNavigationContext) {
  function openCustomerBikePage(target: CustomerBikePage, bikeId: number) {
    ctx.selectedBikeId.value = bikeId;
    ctx.selectedBikeFilter.value = bikeId;
    ctx.customerFilterText.value = '';
    ctx.bikeFilterText.value = '';
    ctx.searchOverhauls.value = '';
    ctx.searchMaintenance.value = '';
    ctx.searchEvents.value = '';
    ctx.searchHistory.value = '';
    ctx.page.value = target;
    ctx.showCustomerDetail.value = false;
  }

  function openCustomerWork(item: { kind: string; bikeId: number }) {
    const target: CustomerBikePage =
      item.kind === 'オーバーホール'
        ? 'overhaul'
        : item.kind === '点検サポート'
          ? 'events'
          : 'maintenance';
    openCustomerBikePage(target, item.bikeId);
  }

  function openCustomerBikeRegistration(kind: RegistrationKind, bike: Bike) {
    openCustomerBikePage(kind === 'event' ? 'events' : kind, bike.id);
    if (kind === 'overhaul') {
      ctx.newOverhaulBike.value = bike.id;
      ctx.overhaulBikeSearch.value = ctx.bikeLabel(bike.id);
      const contractYear = Number(bike.contractStart.slice(0, 4));
      ctx.newOverhaulReceptionDate.value = `${contractYear}-12-15`;
      ctx.newOverhaulDate.value = `${contractYear + 1}-01-15`;
      ctx.newOverhaulEndDate.value = `${contractYear + 1}-01-16`;
      ctx.showOverhaulModal.value = true;
    } else if (kind === 'maintenance') {
      ctx.newWorkBike.value = bike.id;
      ctx.maintenanceBikeSearch.value = ctx.bikeLabel(bike.id);
      ctx.newWorkDate.value = '2027-07-01';
      ctx.newWorkEndDate.value = '2027-07-01';
      ctx.showMaintenanceModal.value = true;
    } else {
      ctx.newEventBike.value = bike.id;
      ctx.eventBikeSearch.value = ctx.bikeLabel(bike.id);
      ctx.newEventName.value = '';
      ctx.newEventDate.value = '2027-08-01';
      ctx.newInspectionDate.value = '2027-07-28';
      ctx.newInspectionEndDate.value = '2027-07-28';
      ctx.showEventModal.value = true;
    }
    ctx.notice.value = `${bike.customer} 様の ${bike.maker} ${bike.model} を登録対象に設定しました。`;
  }

  return { openCustomerBikePage, openCustomerWork, openCustomerBikeRegistration };
}
