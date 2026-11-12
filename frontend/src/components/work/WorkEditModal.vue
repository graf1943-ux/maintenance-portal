<script setup lang="ts">
  import type { ApiStaff } from '../../services/apiMappers';

  const startDate = defineModel<string>('startDate', { required: true });
  const endDate = defineModel<string>('endDate', { required: true });
  const title = defineModel<string>('title', { required: true });
  const detail = defineModel<string>('detail', { required: true });
  const receptionist = defineModel<string>('receptionist', { required: true });

  defineProps<{ staff: ApiStaff[] }>();
  defineEmits<{ close: []; submit: [] }>();
</script>

<template>
  <div class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">EDIT SCHEDULE</p>
          <h2>予定の変更</h2>
        </div>
        <button class="close" @click="$emit('close')">×</button>
      </div>
      <label>作業開始日</label><input v-model="startDate" type="date" />
      <label>作業完了予定日</label><input v-model="endDate" type="date" />
      <label>予定名・イベント名</label><input v-model="title" /> <label>内容・メモ</label
      ><input v-model="detail" />
      <label>受付担当者</label>
      <select v-model="receptionist">
        <option v-for="person in staff" :key="person.id">{{ person.name }}</option>
      </select>
      <div class="modal-note">
        開始日・完了予定日を変更できます。完了日は開始日以降にしてください。契約回数は作業完了時のみ消費します。
      </div>
      <div class="modal-actions">
        <button class="outline" @click="$emit('close')">キャンセル</button>
        <button class="primary" @click="$emit('submit')">変更を保存</button>
      </div>
    </div>
  </div>
</template>
