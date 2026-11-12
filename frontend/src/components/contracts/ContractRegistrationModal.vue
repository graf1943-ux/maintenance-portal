<script setup lang="ts">
  import type { Plan } from '../../types';
  import type { ApiShop, ApiStaff } from '../../services/apiMappers';
  import type { ContractForm } from '../../composables/useContractRegistration';

  const form = defineModel<ContractForm>('form', { required: true });

  defineProps<{
    plans: Plan[];
    shops: ApiShop[];
    staff: ApiStaff[];
  }>();

  defineEmits<{ close: []; submit: [] }>();
</script>

<template>
  <div class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal modal-wide">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW ANNUAL CONTRACT</p>
          <h2>年間契約を登録</h2>
        </div>
        <button class="close" @click="$emit('close')">×</button>
      </div>
      <div class="detail-grid">
        <div>
          <label>顧客</label>
          <input
            v-model="form.customer"
            list="customer-options"
            placeholder="顧客名を入力して検索"
          />
        </div>
        <div><label>メーカー</label><input v-model="form.maker" /></div>
        <div><label>モデル</label><input v-model="form.model" /></div>
        <div><label>年式</label><input v-model.number="form.year" type="number" /></div>
        <div><label>車体番号</label><input v-model="form.serial" /></div>
        <div>
          <label>契約プラン</label>
          <select v-model.number="form.planId">
            <option v-for="plan in plans" :key="plan.id" :value="plan.id">
              {{ plan.name }}（OH {{ plan.overhaulCount }} / メンテ {{ plan.maintenanceCount }} /
              点検 {{ plan.inspectionCount }}）
            </option>
          </select>
        </div>
        <div><label>契約受付日</label><input v-model="form.applicationDate" type="date" /></div>
        <div><label>契約開始日</label><input v-model="form.start" type="date" /></div>
        <div>
          <label>担当店舗</label>
          <select v-model.number="form.shopId">
            <option v-for="shop in shops" :key="shop.id" :value="shop.id">
              {{ shop.name }}
            </option>
          </select>
        </div>
        <div>
          <label>受付担当者</label>
          <select v-model="form.receptionist">
            <option
              v-for="person in staff.filter((item) => item.shopId === form.shopId)"
              :key="person.id"
            >
              {{ person.name }}
            </option>
          </select>
        </div>
      </div>
      <div class="modal-note">
        契約受付期限は契約開始年の12月31日。契約期間は12月1日〜翌年11月30日です。
      </div>
      <div class="modal-actions">
        <button class="outline" @click="$emit('close')">キャンセル</button>
        <button class="primary" @click="$emit('submit')">契約を登録してOH予定へ進む</button>
      </div>
    </div>
  </div>
</template>
