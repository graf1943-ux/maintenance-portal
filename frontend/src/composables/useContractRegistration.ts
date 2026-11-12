import { ref, type Ref } from 'vue';
import { apiPost } from '../api';
import type { Page } from '../types';
import type { ApiCustomer } from '../services/apiMappers';
import { isAnnualContractStart, isApplicationWithinContractYear } from '../services/contractRules';

export type ContractForm = {
  customer: string;
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
    customer: '山田 太郎',
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

    try {
      const existingCustomer = ctx.customers.value.find(
        (customer) => customer.name === form.customer
      );
      let customerId: number;

      if (existingCustomer) {
        customerId = existingCustomer.id;
      } else {
        const customer = await apiPost<ApiCustomer>('/customers', {
          name: form.customer,
          email: '',
          phone: '',
        });
        customerId = customer.id;
      }

      const bike = await apiPost<{ id: number }>('/bikes', {
        id: 0,
        customerId,
        maker: form.maker,
        model: form.model,
        year: form.year,
        serial: form.serial,
        activeContractId: null,
      });
      const staffMember =
        ctx.staff.value.find(
          (member) => member.shopId === form.shopId && member.name === form.receptionist
        ) ?? ctx.staff.value.find((member) => member.shopId === form.shopId);
      if (!staffMember) throw new Error('受付担当者を登録してください。');

      await apiPost('/contracts', {
        bikeId: bike.id,
        planId: form.planId,
        applicationDate: form.applicationDate,
        startDate: form.start,
        shopId: form.shopId,
        receptionistId: staffMember.id,
      });
      await ctx.reloadData();
      ctx.selectedBikeId.value = bike.id;
      ctx.newOverhaulBike.value = bike.id;
      ctx.overhaulBikeSearch.value = ctx.bikeLabel(bike.id);
      ctx.newOverhaulReceptionDate.value = form.applicationDate;
      ctx.notice.value =
        '年間契約を登録しました。次のステップとしてオーバーホールの受付・実施予定日を設定してください。';
      ctx.page.value = 'overhaul';
      showContractModal.value = false;
      ctx.showOverhaulModal.value = true;
    } catch (error) {
      ctx.notice.value = error instanceof Error ? error.message : '年間契約の登録に失敗しました。';
    }
  }

  return { contractForm, showContractModal, addContract };
}
