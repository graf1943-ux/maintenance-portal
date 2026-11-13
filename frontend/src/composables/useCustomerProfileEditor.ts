import { ref, watch, type ComputedRef } from 'vue';
import { apiPut } from '../api';
import type { ApiCustomer } from '../services/apiMappers';

export type CustomerProfileForm = {
  name: string;
  email: string;
  phone: string;
};

export function useCustomerProfileEditor(
  customer: ComputedRef<ApiCustomer | undefined>,
  useMockData: boolean,
  reloadData: () => Promise<void>
) {
  const profile = ref<CustomerProfileForm>({ name: '', email: '', phone: '' });
  const editing = ref(false);
  const saving = ref(false);
  const error = ref('');
  const message = ref('');

  function resetProfile() {
    profile.value = {
      name: customer.value?.name ?? '',
      email: customer.value?.email ?? '',
      phone: customer.value?.phone ?? '',
    };
  }

  watch(
    () => customer.value?.id,
    () => {
      editing.value = false;
      error.value = '';
      message.value = '';
      resetProfile();
    },
    { immediate: true }
  );

  function startEditing() {
    resetProfile();
    error.value = '';
    message.value = '';
    editing.value = true;
  }

  function cancelEditing() {
    resetProfile();
    error.value = '';
    editing.value = false;
  }

  async function save() {
    const current = customer.value;
    const name = profile.value.name.trim();
    error.value = '';
    message.value = '';

    if (!current) {
      error.value = '更新する顧客を選択してください。';
      return false;
    }
    if (!name) {
      error.value = '顧客名を入力してください。';
      return false;
    }
    if (useMockData) {
      error.value = '顧客情報の更新にはAPIモードが必要です。';
      return false;
    }

    saving.value = true;
    try {
      await apiPut<void>(`/customers/${current.id}`, {
        id: current.id,
        name,
        email: profile.value.email.trim(),
        phone: profile.value.phone.trim(),
      });
      await reloadData();
      profile.value.name = name;
      editing.value = false;
      message.value = '顧客情報を更新しました。';
      return true;
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : '顧客情報を更新できませんでした。';
      return false;
    } finally {
      saving.value = false;
    }
  }

  return { profile, editing, saving, error, message, startEditing, cancelEditing, save };
}
