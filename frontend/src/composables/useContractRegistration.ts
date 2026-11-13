import { ref, type Ref } from 'vue';
import { apiPost, apiPut } from '../api';
import type { Bike, Page } from '../types';
import type { ApiCustomer } from '../services/apiMappers';
import { isAnnualContractStart, isApplicationWithinContractYear } from '../services/contractRules';

export type ContractForm = {
  customerMode: 'existing' | 'new';
  customerId: number | null;
  customerSearch: string;
  customer: string;
  email: string;
  phone: string;
  bikeMode: 'existing' | 'new';
  bikeId: number | null;
  maker: string;
  model: string;
  year: number;
  serial: string;
  planId: number;
  applicationDate: string;
  start: string;
  shopId: number;
  receptionist: string;
};

type StaffMember = { id: number; shopId: number; name: string; role: string };

interface ContractRegistrationContext {
  useMockData: boolean;
  customers: Ref<ApiCustomer[]>;
  bikes: Ref<Bike[]>;
  staff: Ref<StaffMember[]>;
  notice: Ref<string>;
  selectedBikeId: Ref<number>;
  newOverhaulBike: Ref<number>;
  overhaulBikeSearch: Ref<string>;
  newOverhaulReceptionDate: Ref<string>;
  page: Ref<Page>;
  showOverhaulModal: Ref<boolean>;
  reloadData: () => Promise<void>;
  bikeLabel: (bikeId: number) => string;
}

/** 顧客・自転車・年間契約を順に登録し、オーバーホール受付へつなぎます。 */
export function useContractRegistration(ctx: ContractRegistrationContext) {
  const contractForm = ref<ContractForm>({
    customerMode: 'existing',
    customerId: null,
    customerSearch: '',
    customer: '',
    email: '',
    phone: '',
    bikeMode: 'new',
    bikeId: null,
    maker: 'SPECIALIZED',
    model: 'Tarmac SL8',
    year: 2026,
    serial: '',
    planId: 1,
    applicationDate: '2026-12-15',
    start: '2026-12-01',
    shopId: 1,
    receptionist: '田中',
  });
  const showContractModal = ref(false);

  function resetCustomerSelection() {
    contractForm.value.customerMode = 'existing';
    contractForm.value.customerId = null;
    contractForm.value.customerSearch = '';
    contractForm.value.customer = '';
    contractForm.value.email = '';
    contractForm.value.phone = '';
    contractForm.value.bikeMode = 'new';
    contractForm.value.bikeId = null;
  }

  async function addContract() {
    if (ctx.useMockData) {
      ctx.notice.value =
        '現在はVueモックデータ表示モードです。年間契約の登録はAPIモードで行ってください。';
      return;
    }

    const form = contractForm.value;
    const contractYear = Number(form.start.slice(0, 4));
    if (!isAnnualContractStart(form.start)) {
      ctx.notice.value = '年間契約の開始日は12月1日にしてください。';
      return;
    }
    if (!isApplicationWithinContractYear(form.applicationDate, contractYear)) {
      ctx.notice.value = '年間契約の受付期限は契約開始年の12月31日です。';
      return;
    }
    if (form.customerMode === 'existing' && form.customerId === null) {
      ctx.notice.value = '登録済み顧客を検索して選択してください。';
      return;
    }
    if (form.customerMode === 'new' && !form.customer.trim()) {
      ctx.notice.value = '新規顧客の顧客名を入力してください。';
      return;
    }
    if (form.customerMode === 'existing' && form.bikeMode === 'existing' && form.bikeId === null) {
      ctx.notice.value = '登録済み自転車を選択してください。';
      return;
    }

    const selectedBike =
      form.customerMode === 'existing' && form.bikeMode === 'existing'
        ? ctx.bikes.value.find(
            (bike) => bike.id === form.bikeId && bike.customerId === form.customerId
          )
        : undefined;
    if (form.bikeMode === 'existing' && !selectedBike) {
      ctx.notice.value = '選択した自転車が見つかりません。顧客と自転車を再選択してください。';
      return;
    }
    if (selectedBike?.contractEnd && selectedBike.contractEnd.replace(/\//g, '-') >= form.start) {
      ctx.notice.value = `選択した自転車の契約は${selectedBike.contractEnd}まで有効です。開始日を確認してください。`;
      return;
    }

    try {
      let customerId: number;

      if (form.customerMode === 'existing') {
        const existingCustomer = ctx.customers.value.find(
          (customer) => customer.id === form.customerId
        );
        if (!existingCustomer)
          throw new Error('選択した顧客が見つかりません。再検索してください。');
        customerId = existingCustomer.id;
        const email = form.email.trim() || existingCustomer.email;
        const phone = form.phone.trim() || existingCustomer.phone || '';
        if (email !== existingCustomer.email || phone !== (existingCustomer.phone || '')) {
          await apiPut<void>(`/customers/${existingCustomer.id}`, {
            ...existingCustomer,
            email,
            phone,
          });
        }
      } else {
        const customer = await apiPost<ApiCustomer>('/customers', {
          name: form.customer.trim(),
          email: form.email.trim(),
          phone: form.phone.trim(),
        });
        customerId = customer.id;
      }

      let bikeId = selectedBike?.id;
      if (form.bikeMode === 'new') {
        const bike = await apiPost<{ id: number }>('/bikes', {
          id: 0,
          customerId,
          maker: form.maker,
          model: form.model,
          year: form.year,
          serial: form.serial,
          activeContractId: null,
        });
        bikeId = bike.id;
      }
      if (bikeId === undefined) throw new Error('登録する自転車を選択してください。');
      const staffMember =
        ctx.staff.value.find(
          (member) => member.shopId === form.shopId && member.name === form.receptionist
        ) ?? ctx.staff.value.find((member) => member.shopId === form.shopId);
      if (!staffMember) throw new Error('受付担当者を登録してください。');

      await apiPost('/contracts', {
        bikeId,
        planId: form.planId,
        applicationDate: form.applicationDate,
        startDate: form.start,
        shopId: form.shopId,
        receptionistId: staffMember.id,
      });
      await ctx.reloadData();
      ctx.selectedBikeId.value = bikeId;
      ctx.newOverhaulBike.value = bikeId;
      ctx.overhaulBikeSearch.value = ctx.bikeLabel(bikeId);
      ctx.newOverhaulReceptionDate.value = form.applicationDate;
      ctx.notice.value =
        '年間契約を登録しました。次のステップとしてオーバーホールの受付・実施予定日を設定してください。';
      ctx.page.value = 'overhaul';
      showContractModal.value = false;
      resetCustomerSelection();
      ctx.showOverhaulModal.value = true;
    } catch (error) {
      ctx.notice.value = error instanceof Error ? error.message : '年間契約の登録に失敗しました。';
    }
  }

  return { contractForm, showContractModal, addContract, resetCustomerSelection };
}
