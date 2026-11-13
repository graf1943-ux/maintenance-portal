import { computed, ref, watch, type ComputedRef, type Ref } from 'vue';
import { apiGet, apiPost, apiPut } from '../api';
import type { Bike, Plan } from '../types';
import { dateIso } from '../services/apiMappers';
import type { ApiBikeComponent, ApiShop } from '../services/apiMappers';
import type { ContractService } from './useContractUsage';

export type BikeProfileForm = {
  maker: string;
  model: string;
  year: number;
  serial: string;
  purchaseDate: string;
};

export type BikeComponentForm = {
  category: string;
  manufacturer: string;
  model: string;
  installedDate: string;
};

type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;

interface BikeDetailsContext {
  useMockData: boolean;
  selectedBike: ComputedRef<Bike>;
  showBikeDetail: Ref<boolean>;
  shops: Ref<ApiShop[]>;
  planFor: (bike: Bike) => Plan;
  remaining: (bike: Bike, kind: ContractService) => number;
  apiAction: ApiAction;
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

  const profile = ref<BikeProfileForm>({
    maker: '',
    model: '',
    year: 0,
    serial: '',
    purchaseDate: '',
  });
  const profileEditing = ref(false);
  const profileSaving = ref(false);
  const profileError = ref('');
  const components = ref<ApiBikeComponent[]>([]);
  const componentsLoading = ref(false);
  const componentForm = ref<BikeComponentForm>({
    category: '',
    manufacturer: '',
    model: '',
    installedDate: localDateInput(),
  });
  const componentSaving = ref(false);
  const componentError = ref('');
  let componentLoadSequence = 0;

  function resetProfile() {
    const bike = ctx.selectedBike.value;
    profile.value = {
      maker: bike.maker,
      model: bike.model,
      year: bike.year,
      serial: bike.serial,
      purchaseDate: dateIso(bike.purchaseDate),
    };
  }

  async function loadComponents() {
    const bikeId = ctx.selectedBike.value.id;
    const sequence = ++componentLoadSequence;
    componentError.value = '';
    componentsLoading.value = true;
    if (ctx.useMockData) {
      components.value = [];
      componentsLoading.value = false;
      return;
    }

    try {
      const result = await apiGet<ApiBikeComponent[]>(`/bikes/${bikeId}/components`);
      if (sequence === componentLoadSequence) components.value = result;
    } catch (cause) {
      if (sequence === componentLoadSequence) {
        componentError.value =
          cause instanceof Error ? cause.message : 'コンポーネント履歴を取得できませんでした。';
      }
    } finally {
      if (sequence === componentLoadSequence) componentsLoading.value = false;
    }
  }

  watch([() => ctx.showBikeDetail.value, () => ctx.selectedBike.value.id], ([isOpen]) => {
    if (!isOpen) return;
    resetProfile();
    profileEditing.value = false;
    profileError.value = '';
    void loadComponents();
  });

  function startProfileEditing() {
    resetProfile();
    profileError.value = '';
    profileEditing.value = true;
  }

  function cancelProfileEditing() {
    resetProfile();
    profileError.value = '';
    profileEditing.value = false;
  }

  async function saveProfile() {
    const bike = ctx.selectedBike.value;
    if (!profile.value.maker.trim() || !profile.value.model.trim()) {
      profileError.value = 'メーカーとモデルを入力してください。';
      return;
    }
    if (!Number.isInteger(profile.value.year) || profile.value.year < 1) {
      profileError.value = '年式を入力してください。';
      return;
    }

    profileSaving.value = true;
    profileError.value = '';
    const succeeded = await ctx.apiAction(
      () =>
        apiPut<void>(`/bikes/${bike.id}`, {
          id: bike.id,
          customerId: bike.customerId,
          maker: profile.value.maker.trim(),
          model: profile.value.model.trim(),
          year: profile.value.year,
          serial: profile.value.serial.trim(),
          purchaseDate: profile.value.purchaseDate || null,
        }),
      '自転車情報を更新しました。'
    );
    if (succeeded) {
      profileEditing.value = false;
    } else {
      profileError.value =
        '自転車情報を更新できませんでした。詳細は画面上部の通知を確認してください。';
    }
    profileSaving.value = false;
  }

  async function addComponent() {
    const bikeId = ctx.selectedBike.value.id;
    const input = componentForm.value;
    if (
      !input.category.trim() ||
      !input.manufacturer.trim() ||
      !input.model.trim() ||
      !input.installedDate
    ) {
      componentError.value = '区分・メーカー・モデル・取付日は必須です。';
      return;
    }

    componentSaving.value = true;
    componentError.value = '';
    const succeeded = await ctx.apiAction(
      () =>
        apiPost<ApiBikeComponent>(`/bikes/${bikeId}/components`, {
          category: input.category.trim(),
          manufacturer: input.manufacturer.trim(),
          model: input.model.trim(),
          installedDate: dateIso(input.installedDate),
        }),
      'コンポーネントを記録しました。'
    );
    if (succeeded) {
      componentForm.value = {
        category: '',
        manufacturer: '',
        model: '',
        installedDate: localDateInput(),
      };
      await loadComponents();
    } else {
      componentError.value =
        'コンポーネントを登録できませんでした。詳細は画面上部の通知を確認してください。';
    }
    componentSaving.value = false;
  }

  return {
    bikeDetails,
    profile,
    profileEditing,
    profileSaving,
    profileError,
    components,
    componentsLoading,
    componentForm,
    componentSaving,
    componentError,
    startProfileEditing,
    cancelProfileEditing,
    saveProfile,
    addComponent,
  };
}

function localDateInput() {
  const date = new Date();
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}
