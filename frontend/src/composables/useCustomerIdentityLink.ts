import { ref, type ComputedRef } from 'vue';
import { apiPut } from '../api';
import type { ApiCustomer } from '../services/apiMappers';

/** 管理者向け顧客ID連携フォームの状態と保存処理を管理します。 */
export function useCustomerIdentityLink(customer: ComputedRef<ApiCustomer | undefined>) {
  const tenantId = ref('');
  const objectId = ref('');
  const error = ref('');
  const message = ref('');
  const saving = ref(false);

  function reset() {
    tenantId.value = '';
    objectId.value = '';
    error.value = '';
    message.value = '';
  }

  async function link() {
    const customerId = customer.value?.id;
    const tenant = tenantId.value.trim();
    const object = objectId.value.trim();
    error.value = '';
    message.value = '';
    if (!customerId || !tenant || !object) {
      error.value = '顧客、Tenant ID、Object IDを確認してください。';
      return;
    }

    saving.value = true;
    try {
      await apiPut<void>(`/customers/${customerId}/identity`, {
        tenantId: tenant,
        objectId: object,
      });
      message.value = 'Entra IDを顧客に紐付けました。';
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'Entra IDを紐付けできませんでした。';
    } finally {
      saving.value = false;
    }
  }

  return { tenantId, objectId, error, message, saving, reset, link };
}
