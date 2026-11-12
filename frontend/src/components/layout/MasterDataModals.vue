<script setup lang="ts">
  import type { ApiShop } from '../../services/apiMappers';
  import type { NewPlan } from '../../composables/useMasterDataRegistration';

  defineProps<{
    showShop: boolean;
    showStaff: boolean;
    showPlan: boolean;
    shops: ApiShop[];
  }>();

  const shopName = defineModel<string>('shopName', { required: true });
  const shopAddress = defineModel<string>('shopAddress', { required: true });
  const shopPhone = defineModel<string>('shopPhone', { required: true });
  const staffName = defineModel<string>('staffName', { required: true });
  const staffShop = defineModel<number>('staffShop', { required: true });
  const plan = defineModel<NewPlan>('plan', { required: true });

  defineEmits<{
    'close-shop': [];
    'close-staff': [];
    'close-plan': [];
    'add-shop': [];
    'add-staff': [];
    'add-plan': [];
  }>();
</script>

<template>
  <div v-if="showShop" class="modal-backdrop" @click.self="$emit('close-shop')">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW STORE</p>
          <h2>店舗を登録</h2>
        </div>
        <button class="close" @click="$emit('close-shop')">×</button>
      </div>
      <label>店舗名</label><input v-model="shopName" placeholder="例：大阪店" /> <label>住所</label
      ><input v-model="shopAddress" placeholder="住所" /> <label>電話番号</label
      ><input v-model="shopPhone" placeholder="電話番号" />
      <div class="modal-actions">
        <button class="outline" @click="$emit('close-shop')">キャンセル</button>
        <button class="primary" @click="$emit('add-shop')">店舗を登録</button>
      </div>
    </div>
  </div>

  <div v-if="showStaff" class="modal-backdrop" @click.self="$emit('close-staff')">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW STAFF</p>
          <h2>受付担当者を登録</h2>
        </div>
        <button class="close" @click="$emit('close-staff')">×</button>
      </div>
      <label>所属店舗</label>
      <select v-model.number="staffShop">
        <option v-for="shop in shops" :key="shop.id" :value="shop.id">{{ shop.name }}</option>
      </select>
      <label>担当者名</label><input v-model="staffName" placeholder="担当者名" />
      <div class="modal-actions">
        <button class="outline" @click="$emit('close-staff')">キャンセル</button>
        <button class="primary" @click="$emit('add-staff')">担当者を登録</button>
      </div>
    </div>
  </div>

  <div v-if="showPlan" class="modal-backdrop" @click.self="$emit('close-plan')">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW PLAN</p>
          <h2>プランを追加</h2>
        </div>
        <button class="close" @click="$emit('close-plan')">×</button>
      </div>
      <label>プラン名</label><input v-model="plan.name" placeholder="例：競技者向けプラン" />
      <label>オーバーホール回数</label>
      <input v-model.number="plan.overhaulCount" type="number" min="0" step="1" />
      <label>メンテナンス回数</label>
      <input v-model.number="plan.maintenanceCount" type="number" min="0" step="1" />
      <label>点検サポート回数</label>
      <input v-model.number="plan.inspectionCount" type="number" min="0" step="1" />
      <div class="modal-note">回数はそれぞれ独立して設定します。</div>
      <div class="modal-actions">
        <button class="outline" @click="$emit('close-plan')">キャンセル</button>
        <button class="primary" @click="$emit('add-plan')">プランを追加</button>
      </div>
    </div>
  </div>
</template>
