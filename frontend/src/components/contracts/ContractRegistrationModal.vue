<script setup lang="ts">
  import { computed } from 'vue';
  import type { Bike, Plan } from '../../types';
  import type { ApiCustomer, ApiShop, ApiStaff } from '../../services/apiMappers';
  import type { ContractForm } from '../../composables/useContractRegistration';

  const form = defineModel<ContractForm>('form', { required: true });

  const props = defineProps<{
    bikes: Bike[];
    customers: ApiCustomer[];
    plans: Plan[];
    shops: ApiShop[];
    staff: ApiStaff[];
  }>();

  const matchingCustomers = computed(() => {
    const normalize = (value: string) => value.toLowerCase().replace(/[\s()-]/g, '');
    const query = normalize(form.value.customerSearch.trim());
    if (!query) return [];
    return props.customers.filter((customer) =>
      [customer.name, customer.email, customer.phone ?? ''].some((value) =>
        normalize(value).includes(query)
      )
    );
  });
  const duplicateNameCustomers = computed(() => {
    const name = form.value.customer.trim().toLowerCase();
    if (form.value.customerMode !== 'new' || !name) return [];
    return props.customers.filter((customer) => customer.name.trim().toLowerCase() === name);
  });
  const selectedCustomerId = computed({
    get: () => String(form.value.customerId ?? ''),
    set: (value: string) => {
      const customer = props.customers.find((item) => item.id === Number(value));
      form.value.customerId = customer?.id ?? null;
      if (customer) {
        form.value.customer = customer.name;
        form.value.email = customer.email;
        form.value.phone = customer.phone ?? '';
        form.value.bikeId = null;
        form.value.bikeMode = props.bikes.some((bike) => bike.customerId === customer.id)
          ? 'existing'
          : 'new';
      }
    },
  });
  const customerBikes = computed(() =>
    props.bikes.filter((bike) => bike.customerId === form.value.customerId)
  );
  const selectedBikeId = computed({
    get: () => String(form.value.bikeId ?? ''),
    set: (value: string) => {
      const bike = customerBikes.value.find((item) => item.id === Number(value));
      form.value.bikeId = bike?.id ?? null;
    },
  });
  const selectedBike = computed(() =>
    customerBikes.value.find((bike) => bike.id === form.value.bikeId)
  );
  const selectedBikeHasOverlappingContract = computed(() => {
    const end = selectedBike.value?.contractEnd.replace(/\//g, '-');
    return Boolean(end && end >= form.value.start);
  });

  function setCustomerMode(mode: 'existing' | 'new', search = '') {
    form.value.customerMode = mode;
    form.value.customerId = null;
    form.value.customerSearch = search;
    form.value.customer = '';
    form.value.email = '';
    form.value.phone = '';
    form.value.bikeMode = 'new';
    form.value.bikeId = null;
  }

  function clearCustomerSelection() {
    form.value.customerId = null;
    form.value.customer = '';
    form.value.email = '';
    form.value.phone = '';
    form.value.bikeMode = 'new';
    form.value.bikeId = null;
  }

  function setBikeMode(mode: 'existing' | 'new') {
    form.value.bikeMode = mode;
    form.value.bikeId = null;
  }

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
      <div class="customer-registration">
        <div class="filter-tabs">
          <button
            type="button"
            :class="{ selected: form.customerMode === 'existing' }"
            @click="setCustomerMode('existing')"
          >
            登録済み顧客を選択
          </button>
          <button
            type="button"
            :class="{ selected: form.customerMode === 'new' }"
            @click="setCustomerMode('new')"
          >
            新規顧客を登録
          </button>
        </div>
        <template v-if="form.customerMode === 'existing'">
          <div class="detail-grid">
            <div>
              <label>顧客を検索</label>
              <input
                v-model="form.customerSearch"
                placeholder="顧客名・メールアドレス・電話番号"
                @input="clearCustomerSelection"
              />
            </div>
            <div>
              <label>検索結果</label>
              <select v-model="selectedCustomerId">
                <option disabled value="">顧客を選択してください</option>
                <option
                  v-for="customer in matchingCustomers"
                  :key="customer.id"
                  :value="customer.id"
                >
                  {{ customer.name }} — {{ customer.email || 'メール未登録' }} /
                  {{ customer.phone || '電話未登録' }}
                </option>
              </select>
            </div>
          </div>
          <p v-if="!form.customerSearch.trim()" class="muted">
            顧客名・メールアドレス・電話番号のいずれかで検索してください。
          </p>
          <p v-else-if="matchingCustomers.length === 0" class="muted">
            該当する顧客がいません。新規顧客として登録できます。
          </p>
          <div v-if="form.customerId !== null" class="detail-grid">
            <div><label>選択した顧客</label><input :value="form.customer" readonly /></div>
            <div><label>メールアドレス</label><input v-model="form.email" type="email" /></div>
            <div><label>電話番号</label><input v-model="form.phone" type="tel" /></div>
          </div>
          <p v-if="form.customerId !== null" class="modal-note">
            入力したメールアドレス・電話番号を更新します。空欄の項目は既存情報を保持します。
          </p>
        </template>
        <template v-else>
          <div class="detail-grid">
            <div><label>顧客名</label><input v-model="form.customer" /></div>
            <div><label>メールアドレス</label><input v-model="form.email" type="email" /></div>
            <div><label>電話番号</label><input v-model="form.phone" type="tel" /></div>
          </div>
          <div v-if="duplicateNameCustomers.length > 0" class="customer-duplicate-warning">
            <span
              >同じ顧客名が登録済みです。連絡先を確認し、既存顧客かどうか確認してください。</span
            >
            <button type="button" @click="setCustomerMode('existing', form.customer)">
              登録済み顧客を検索
            </button>
          </div>
        </template>
      </div>
      <div v-if="form.customerMode === 'new' || form.customerId !== null" class="bike-registration">
        <div
          v-if="form.customerMode === 'existing' && customerBikes.length > 0"
          class="filter-tabs"
        >
          <button
            type="button"
            :class="{ selected: form.bikeMode === 'existing' }"
            @click="setBikeMode('existing')"
          >
            登録済み自転車を選択
          </button>
          <button
            type="button"
            :class="{ selected: form.bikeMode === 'new' }"
            @click="setBikeMode('new')"
          >
            新しい自転車を登録
          </button>
        </div>
        <p
          v-if="
            form.customerMode === 'existing' &&
            customerBikes.length === 0 &&
            form.customerId !== null
          "
          class="muted"
        >
          この顧客に登録済みの自転車はありません。新しい自転車を登録してください。
        </p>
        <div v-if="form.bikeMode === 'existing'" class="detail-grid">
          <div>
            <label>登録済み自転車</label>
            <select v-model="selectedBikeId">
              <option disabled value="">自転車を選択してください</option>
              <option v-for="bike in customerBikes" :key="bike.id" :value="bike.id">
                {{ bike.maker }} {{ bike.model }}（車体番号 {{ bike.serial || '未登録' }} /
                {{
                  bike.contractEnd
                    ? `契約 ${bike.contractStart}〜${bike.contractEnd}`
                    : '有効契約なし'
                }}）
              </option>
            </select>
          </div>
          <div v-if="selectedBike">
            <label>契約期間の確認</label>
            <p v-if="selectedBikeHasOverlappingContract" class="contract-overlap-warning">
              現在の契約（{{ selectedBike.contractEnd }}まで）と開始日が重複します。
            </p>
            <p v-else class="modal-note">
              {{
                selectedBike.contractEnd
                  ? `前契約の終了後に新しい契約を開始できます（契約終了日：${selectedBike.contractEnd}）。`
                  : '有効な契約がない自転車です。'
              }}
            </p>
          </div>
        </div>
        <div v-else class="detail-grid">
          <div><label>メーカー</label><input v-model="form.maker" /></div>
          <div><label>モデル</label><input v-model="form.model" /></div>
          <div><label>年式</label><input v-model.number="form.year" type="number" /></div>
          <div><label>車体番号</label><input v-model="form.serial" /></div>
        </div>
      </div>
      <div class="detail-grid">
        <div>
          <label>契約プラン</label>
          <select v-model.number="form.planId">
            <option v-for="plan in plans" :key="plan.id" :value="plan.id">
              {{ plan.name }}（OH {{ plan.overhaulCount }} / メンテ {{ plan.maintenanceCount }} /
              点検サポート {{ plan.inspectionCount }}）
            </option>
          </select>
        </div>
        <div><label>契約受付日</label><input v-model="form.applicationDate" type="date" /></div>
        <div><label>契約開始日</label><input v-model="form.start" type="date" /></div>
        <div>
          <label>店舗</label>
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
