<script setup lang="ts">
  import { computed } from 'vue';
  import type { ApiShop, ApiStaff } from '../../services/apiMappers';

  const props = defineProps<{
    shops: ApiShop[];
    staff: ApiStaff[];
    selectedShopId: number | null;
    selectedStaffId: number | null;
  }>();
  const emit = defineEmits<{
    'shop-change': [shopId: number | null];
    'staff-change': [staffId: number | null];
  }>();

  const availableStaff = computed(() =>
    props.selectedShopId === null
      ? props.staff
      : props.staff.filter((member) => member.shopId === props.selectedShopId)
  );

  function selectedId(event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    return value ? Number(value) : null;
  }
</script>

<template>
  <section class="panel assignment-filter" aria-label="店舗・担当者で作業を絞り込み">
    <div class="filter-description">
      <strong>作業の表示範囲</strong>
      <span>契約・作業に登録された店舗と担当者で表示を絞り込みます。</span>
      <span>履歴は報告書の担当情報で絞り込み、未記録の履歴は条件選択中に表示されません。</span>
    </div>
    <div class="filter-controls">
      <label>
        店舗
        <select :value="selectedShopId ?? ''" @change="emit('shop-change', selectedId($event))">
          <option value="">全店舗</option>
          <option v-for="shop in shops" :key="shop.id" :value="shop.id">{{ shop.name }}</option>
        </select>
      </label>
      <label>
        担当者
        <select
          :value="selectedStaffId ?? ''"
          :disabled="availableStaff.length === 0"
          @change="emit('staff-change', selectedId($event))"
        >
          <option value="">全担当者</option>
          <option v-for="member in availableStaff" :key="member.id" :value="member.id">
            {{ member.name }}
          </option>
        </select>
      </label>
    </div>
  </section>
</template>

<style scoped>
  .assignment-filter {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    padding: 14px 18px;
  }
  .filter-description {
    display: flex;
    flex-direction: column;
    gap: 3px;
  }
  .filter-description strong {
    color: #293b52;
    font-size: 12px;
  }
  .filter-description span {
    color: #8190a3;
    font-size: 10px;
  }
  .filter-controls {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
  }
  label {
    display: flex;
    align-items: center;
    gap: 7px;
    color: #596a80;
    font-size: 11px;
    font-weight: 600;
  }
  select {
    min-width: 150px;
    padding: 8px 10px;
    color: #33475e;
    background: #fff;
    border: 1px solid #d5deea;
    border-radius: 6px;
    font: inherit;
    font-weight: 400;
  }
  select:focus-visible {
    border-color: #6b95c1;
    outline: 2px solid #6b95c133;
  }
  @media (max-width: 700px) {
    .assignment-filter {
      align-items: stretch;
      flex-direction: column;
      gap: 10px;
      padding: 12px;
    }
    .filter-controls {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 8px;
    }
    label {
      align-items: stretch;
      flex-direction: column;
      gap: 4px;
    }
    select {
      min-width: 0;
      width: 100%;
    }
  }
</style>
