import type { Ref } from 'vue';

type CustomerSelection = string | { id?: number; name: string };

interface DetailSelectionContext {
  selectedCustomerName: Ref<string>;
  selectedCustomerId: Ref<number | null>;
  selectedBikeId: Ref<number>;
  showCustomerDetail: Ref<boolean>;
  showBikeDetail: Ref<boolean>;
  resetCustomerIdentity: () => void;
}

/** 顧客・自転車詳細を開く際の選択状態と関連フォームの初期化を管理します。 */
export function useDetailSelection(ctx: DetailSelectionContext) {
  function openCustomerDetail(customer: CustomerSelection) {
    ctx.selectedCustomerName.value = typeof customer === 'string' ? customer : customer.name;
    ctx.selectedCustomerId.value = typeof customer === 'string' ? null : (customer.id ?? null);
    ctx.resetCustomerIdentity();
    ctx.showCustomerDetail.value = true;
  }

  function openBikeDetail(bikeId: number) {
    ctx.selectedBikeId.value = bikeId;
    ctx.showBikeDetail.value = true;
  }

  return { openCustomerDetail, openBikeDetail };
}
