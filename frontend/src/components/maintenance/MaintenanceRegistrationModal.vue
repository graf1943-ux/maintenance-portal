<script setup lang="ts">
  const bikeSearch = defineModel<string>('bikeSearch', { required: true });
  const startDate = defineModel<string>('startDate', { required: true });
  const endDate = defineModel<string>('endDate', { required: true });
  const isAdHoc = defineModel<boolean>('isAdHoc', { required: true });
  const title = defineModel<string>('title', { required: true });
  const detail = defineModel<string>('detail', { required: true });

  defineProps<{
    bikeSelected: boolean;
    availableCount: number;
    contractLimit: number;
    error: string;
  }>();

  defineEmits<{
    'bike-change': [label: string];
    close: [];
    submit: [];
  }>();
</script>

<template>
  <div class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal modal-wide">
      <div class="modal-head">
        <div>
          <p class="eyebrow">NEW MAINTENANCE</p>
          <h2>メンテナンス計画を登録</h2>
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
        <div>
          <label>作業開始予定日</label>
          <input v-model="startDate" type="date" :disabled="isAdHoc" />
        </div>
        <div>
          <label>作業完了予定日</label>
          <input v-model="endDate" type="date" />
        </div>
        <div><label>計画名</label><input v-model="title" /></div>
        <div><label>作業内容</label><input v-model="detail" /></div>
      </div>
      <label class="checkbox-label">
        <input v-model="isAdHoc" type="checkbox" />
        予約なしの突発作業として受付（受付と同時に作業開始）
      </label>
      <div class="modal-note">
        新規登録可能：{{ availableCount }} / 契約枠
        {{ contractLimit }}回（予定・完了を含む。キャンセル・中止を除く）
      </div>
      <p v-if="!bikeSelected" class="customer-identity-feedback error" role="alert">
        顧客・自転車を候補から選択してください。
      </p>
      <p v-else-if="availableCount < 1" class="customer-identity-feedback error" role="alert">
        この自転車は契約上限回数に達しているため登録できません。
      </p>
      <p v-if="error" class="customer-identity-feedback error" role="alert">{{ error }}</p>
      <div class="modal-actions">
        <button class="outline" @click="$emit('close')">キャンセル</button>
        <button
          class="primary"
          :disabled="!bikeSelected || availableCount <= 0"
          @click="$emit('submit')"
        >
          計画を登録
        </button>
      </div>
    </div>
  </div>
</template>
