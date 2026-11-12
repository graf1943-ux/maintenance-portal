<script setup lang="ts">
  const bikeSearch = defineModel<string>('bikeSearch', { required: true });
  const receptionDate = defineModel<string>('receptionDate', { required: true });
  const startDate = defineModel<string>('startDate', { required: true });
  const endDate = defineModel<string>('endDate', { required: true });
  const title = defineModel<string>('title', { required: true });
  const detail = defineModel<string>('detail', { required: true });

  defineProps<{
    bikeSelected: boolean;
    availableCount: number;
    contractLimit: number;
  }>();

  defineEmits<{ 'bike-change': [label: string]; close: []; submit: [] }>();
</script>

<template>
  <div class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal modal-wide">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW OVERHAUL</p>
          <h2>オーバーホールを登録</h2>
        </div>
        <button class="close" @click="$emit('close')">×</button>
      </div>
      <div class="detail-grid">
        <div>
          <label>顧客・自転車</label>
          <input
            v-model="bikeSearch"
            list="bike-options"
            placeholder="顧客名・自転車名を入力して検索"
            @change="$emit('bike-change', bikeSearch)"
          />
        </div>
        <div><label>受付日</label><input v-model="receptionDate" type="date" /></div>
        <div><label>作業開始予定日</label><input v-model="startDate" type="date" /></div>
        <div><label>作業完了予定日</label><input v-model="endDate" type="date" /></div>
        <div><label>作業名</label><input v-model="title" /></div>
        <div><label>作業内容</label><input v-model="detail" /></div>
      </div>
      <div class="modal-note">
        受付期限は契約開始年の12月31日。実施期間は契約開始年11月〜翌年2月末です。新規登録可能：{{
          availableCount
        }}
        / 契約枠 {{ contractLimit }}回（予定・完了を含む）
      </div>
      <div class="modal-actions">
        <button class="outline" @click="$emit('close')">キャンセル</button>
        <button
          class="primary"
          :disabled="!bikeSelected || availableCount <= 0"
          @click="$emit('submit')"
        >
          オーバーホールを登録
        </button>
      </div>
    </div>
  </div>
</template>
