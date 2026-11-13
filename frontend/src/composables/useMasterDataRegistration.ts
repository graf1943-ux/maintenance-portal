import { ref, watch, type Ref } from 'vue';
import { apiPost } from '../api';
import type { ApiShop } from '../services/apiMappers';

type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;
export type NewPlan = {
  name: string;
  overhaulCount: number;
  maintenanceCount: number;
  inspectionCount: number;
};

interface MasterDataRegistrationContext {
  notice: Ref<string>;
  shops: Ref<ApiShop[]>;
  apiAction: ApiAction;
}

/** プラン・店舗・受付担当者の登録処理と成功後のフォーム初期化をまとめます。 */
export function useMasterDataRegistration(ctx: MasterDataRegistrationContext) {
  const newPlan = ref<NewPlan>({
    name: '',
    overhaulCount: 1,
    maintenanceCount: 2,
    inspectionCount: 1,
  });
  const newShopName = ref('');
  const newShopAddress = ref('');
  const newShopPhone = ref('');
  const newStaffName = ref('');
  const newStaffShop = ref(1);
  const showPlanModal = ref(false);
  const showShopModal = ref(false);
  const showStaffModal = ref(false);
  const shopError = ref('');
  const staffError = ref('');
  const planError = ref('');
  watch(showShopModal, (isOpen) => {
    if (isOpen) shopError.value = '';
  });
  watch(showStaffModal, (isOpen) => {
    if (isOpen) staffError.value = '';
  });
  watch(showPlanModal, (isOpen) => {
    if (isOpen) planError.value = '';
  });

  async function addPlan() {
    planError.value = '';
    const name = newPlan.value.name.trim();
    if (!name) {
      planError.value = 'プラン名を入力してください。';
      ctx.notice.value = planError.value;
      return;
    }
    const counts = [
      newPlan.value.overhaulCount,
      newPlan.value.maintenanceCount,
      newPlan.value.inspectionCount,
    ];
    if (counts.some((count) => !Number.isInteger(count) || count < 0)) {
      planError.value = '各回数は0以上の整数で入力してください。';
      ctx.notice.value = planError.value;
      return;
    }

    const ok = await ctx.apiAction(
      () => apiPost('/plans', { ...newPlan.value, name }),
      'プランを追加しました。顧客・自転車・契約管理画面から選択できます。'
    );
    if (ok) {
      newPlan.value = { name: '', overhaulCount: 1, maintenanceCount: 2, inspectionCount: 1 };
      showPlanModal.value = false;
    } else {
      planError.value = ctx.notice.value;
    }
  }

  async function addShop() {
    shopError.value = '';
    if (!newShopName.value.trim()) {
      shopError.value = '店舗名を入力してください。';
      return;
    }
    const ok = await ctx.apiAction(
      () =>
        apiPost('/shops', {
          name: newShopName.value,
          address: newShopAddress.value,
          phone: newShopPhone.value,
        }),
      '店舗を登録しました。'
    );
    if (ok) {
      newShopName.value = '';
      newShopAddress.value = '';
      newShopPhone.value = '';
      showShopModal.value = false;
    } else {
      shopError.value = ctx.notice.value;
    }
  }

  async function addStaff() {
    staffError.value = '';
    if (!newStaffName.value.trim()) {
      staffError.value = '担当者名を入力してください。';
      return;
    }
    if (ctx.shops.value.length === 0) {
      staffError.value = '先に店舗を登録してください。';
      return;
    }
    const ok = await ctx.apiAction(
      () =>
        apiPost('/staff', {
          shopId: newStaffShop.value,
          name: newStaffName.value,
          role: '受付担当',
        }),
      '受付担当者を登録しました。'
    );
    if (ok) {
      newStaffName.value = '';
      showStaffModal.value = false;
    } else {
      staffError.value = ctx.notice.value;
    }
  }

  return {
    newPlan,
    newShopName,
    newShopAddress,
    newShopPhone,
    newStaffName,
    newStaffShop,
    showPlanModal,
    showShopModal,
    showStaffModal,
    shopError,
    staffError,
    planError,
    addPlan,
    addShop,
    addStaff,
  };
}
