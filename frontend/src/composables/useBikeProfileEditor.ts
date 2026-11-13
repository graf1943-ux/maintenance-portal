import { ref, watch, type ComputedRef, type Ref } from 'vue';
import { apiPut } from '../api';
import type { Bike } from '../types';
import { dateIso } from '../services/apiMappers';

export type BikeProfileForm = {
  maker: string;
  model: string;
  year: number;
  serial: string;
  purchaseDate: string;
};

type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;

/** 自転車プロフィール編集フォームと保存処理を管理します。 */
export function useBikeProfileEditor(
  selectedBike: ComputedRef<Bike>,
  showBikeDetail: Ref<boolean>,
  apiAction: ApiAction
) {
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

  function resetProfile() {
    const bike = selectedBike.value;
    profile.value = {
      maker: bike.maker,
      model: bike.model,
      year: bike.year,
      serial: bike.serial,
      purchaseDate: dateIso(bike.purchaseDate),
    };
  }

  function startProfileEditing() {
    resetProfile();
    profileError.value = '';
    profileEditing.value = true;
  }

  watch([showBikeDetail, () => selectedBike.value.id], ([isOpen]) => {
    if (!isOpen) return;
    resetProfile();
    profileEditing.value = false;
    profileError.value = '';
  });

  function cancelProfileEditing() {
    resetProfile();
    profileError.value = '';
    profileEditing.value = false;
  }

  async function saveProfile() {
    const bike = selectedBike.value;
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
    const succeeded = await apiAction(
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

  return {
    profile,
    profileEditing,
    profileSaving,
    profileError,
    startProfileEditing,
    cancelProfileEditing,
    saveProfile,
    resetProfile,
  };
}
