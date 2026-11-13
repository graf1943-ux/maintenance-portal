import { ref, watch, type ComputedRef, type Ref } from 'vue';
import { apiGet, apiPost } from '../api';
import type { Bike } from '../types';
import { dateIso, type ApiBikeComponent } from '../services/apiMappers';
import { localDateInput } from '../utils/date';

export type BikeComponentForm = {
  category: string;
  manufacturer: string;
  model: string;
  installedDate: string;
};

type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;

interface BikeComponentsContext {
  useMockData: boolean;
  selectedBike: ComputedRef<Bike>;
  showBikeDetail: Ref<boolean>;
  apiAction: ApiAction;
}

/** 詳細モーダルのコンポーネント履歴取得と登録フォームを管理します。 */
export function useBikeComponents(ctx: BikeComponentsContext) {
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
    if (isOpen) void loadComponents();
  });

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
    components,
    componentsLoading,
    componentForm,
    componentSaving,
    componentError,
    addComponent,
  };
}
