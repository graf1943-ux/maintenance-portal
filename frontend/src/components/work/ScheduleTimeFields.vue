<script setup lang="ts">
  import type { ScheduleTimeSlot } from '../../types';

  const props = defineProps<{ startDate: string; endDate: string }>();
  const estimatedWorkMinutes = defineModel<number | null>('estimatedWorkMinutes', {
    required: true,
  });
  const scheduleSlots = defineModel<ScheduleTimeSlot[]>('scheduleSlots', { required: true });

  function updateEstimatedWorkMinutes(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    estimatedWorkMinutes.value = value ? Number(value) : null;
  }

  function addSlot() {
    scheduleSlots.value = [
      ...scheduleSlots.value,
      { date: props.startDate, startTime: '', endTime: '' },
    ];
  }

  function removeSlot(index: number) {
    scheduleSlots.value = scheduleSlots.value.filter((_, slotIndex) => slotIndex !== index);
  }
</script>

<template>
  <section class="schedule-time-fields">
    <div class="estimate-field">
      <label for="estimated-work-minutes">作業見込み時間（分）</label>
      <input
        id="estimated-work-minutes"
        :value="estimatedWorkMinutes ?? ''"
        type="number"
        min="1"
        max="10080"
        step="1"
        placeholder="未入力可"
        @input="updateEstimatedWorkMinutes"
      />
      <small>実作業の見込みです。担当者を確保する時間枠とは別に登録します。</small>
    </div>
    <div class="slot-heading">
      <div>
        <strong>日ごとの予定時間</strong>
        <small>複数日作業は、作業する日ごとに時間枠を追加できます。</small>
      </div>
      <button type="button" class="outline small" @click="addSlot">＋ 時間枠を追加</button>
    </div>
    <div v-if="scheduleSlots.length" class="slot-list">
      <div v-for="(slot, index) in scheduleSlots" :key="index" class="slot-row">
        <label>
          日付
          <input
            v-model="slot.date"
            type="date"
            :min="startDate"
            :max="endDate || startDate"
            required
          />
        </label>
        <label>
          開始
          <input v-model="slot.startTime" type="time" required />
        </label>
        <label>
          終了
          <input v-model="slot.endTime" type="time" required />
        </label>
        <button
          type="button"
          class="remove-slot"
          :aria-label="`${index + 1}件目の時間枠を削除`"
          @click="removeSlot(index)"
        >
          削除
        </button>
      </div>
    </div>
    <p v-else class="slot-empty">
      時間枠未設定の予定は、重複判定ではなく「時間要確認」として表示されます。
    </p>
  </section>
</template>

<style scoped>
  .schedule-time-fields {
    margin-top: 16px;
    padding: 14px;
    background: #f8fafc;
    border: 1px solid #e8edf3;
    border-radius: 8px;
  }
  .estimate-field {
    display: grid;
    grid-template-columns: minmax(160px, 1fr) minmax(130px, 190px);
    align-items: center;
    gap: 6px 12px;
    max-width: 520px;
  }
  .estimate-field label,
  .slot-row label {
    color: #53647a;
    font-size: 11px;
    font-weight: 600;
  }
  .estimate-field input,
  .slot-row input {
    width: 100%;
    min-width: 0;
    padding: 8px;
    color: #33475e;
    background: #fff;
    border: 1px solid #d5deea;
    border-radius: 5px;
    font: inherit;
    font-size: 11px;
  }
  .estimate-field small {
    grid-column: 1 / -1;
    color: #8190a3;
    font-size: 10px;
  }
  .slot-heading {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 10px;
    margin-top: 14px;
  }
  .slot-heading > div {
    display: flex;
    flex-direction: column;
    gap: 3px;
  }
  .slot-heading strong {
    color: #33475e;
    font-size: 11px;
  }
  .slot-heading small,
  .slot-empty {
    color: #8190a3;
    font-size: 10px;
  }
  .slot-list {
    display: grid;
    gap: 7px;
    margin-top: 8px;
  }
  .slot-row {
    display: grid;
    grid-template-columns: minmax(145px, 1fr) minmax(110px, 150px) minmax(110px, 150px) auto;
    align-items: end;
    gap: 8px;
    padding: 8px;
    background: #fff;
    border: 1px solid #e8edf3;
    border-radius: 6px;
  }
  .slot-row label {
    display: grid;
    gap: 4px;
  }
  .remove-slot {
    padding: 7px 8px;
    color: #a44f4f;
    background: transparent;
    border: 1px solid #e2caca;
    border-radius: 5px;
    font: inherit;
    font-size: 10px;
    cursor: pointer;
  }
  .slot-empty {
    margin: 8px 0 0;
  }
  @media (max-width: 700px) {
    .schedule-time-fields {
      padding: 10px;
    }
    .estimate-field {
      grid-template-columns: 1fr;
      max-width: none;
    }
    .estimate-field small {
      grid-column: auto;
    }
    .slot-heading {
      align-items: flex-start;
    }
    .slot-row {
      grid-template-columns: 1fr 1fr;
    }
    .slot-row label:first-child {
      grid-column: 1 / -1;
    }
    .remove-slot {
      justify-self: start;
    }
  }
</style>
