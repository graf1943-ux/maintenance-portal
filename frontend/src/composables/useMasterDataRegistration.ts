import { ref, type Ref } from 'vue';
import { apiPost } from '../api';

type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;
export type NewPlan = {
  name: string;
  overhaulCount: number;
  maintenanceCount: number;
  inspectionCount: number;
};

interface MasterDataRegistrationContext {
  notice: Ref<string>;
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

  async function addPlan() {
    const name = newPlan.value.name.trim();
    if (!name) {
      ctx.notice.value = 'プラン名を入力してください。';
      return;
    }
    const counts = [
      newPlan.value.overhaulCount,
      newPlan.value.maintenanceCount,
      newPlan.value.inspectionCount,
    ];
    if (counts.some((count) => !Number.isInteger(count) || count < 0)) {
      ctx.notice.value = '各回数は0以上の整数で入力してください。';
      return;
    }

    const ok = await ctx.apiAction(
      () => apiPost('/plans', { ...newPlan.value, name }),
      'プランを追加しました。顧客・自転車・契約管理画面から選択できます。'
    );
    if (ok) {
      newPlan.value = { name: '', overhaulCount: 1, maintenanceCount: 2, inspectionCount: 1 };
      showPlanModal.value = false;
    }
  }

  async function addShop() {
    if (!newShopName.value.trim()) return;
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
    }
  }

  async function addStaff() {
    if (!newStaffName.value.trim()) return;
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
    addPlan,
    addShop,
    addStaff,
  };
}
