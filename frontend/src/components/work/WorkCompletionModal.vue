<script setup lang="ts">
  import type { WorkChecklistItem, WorkReportPart } from '../../types';

  const props = defineProps<{ error: string }>();
  const date = defineModel<string>('date', { required: true });
  const memo = defineModel<string>('memo', { required: true });
  const workMinutes = defineModel<number>('work-minutes', { required: true });
  const checklist = defineModel<WorkChecklistItem[]>('checklist', { required: true });
  const parts = defineModel<WorkReportPart[]>('parts', { required: true });

  defineEmits<{ close: []; submit: [] }>();

  function addChecklistItem() {
    checklist.value = [...checklist.value, { item: '', result: '良好', note: '' }];
  }

  function removeChecklistItem(index: number) {
    checklist.value = checklist.value.filter((_, itemIndex) => itemIndex !== index);
  }

  function addPart() {
    parts.value = [...parts.value, { name: '', quantity: 1, unit: '個', unitPrice: 0 }];
  }

  function removePart(index: number) {
    parts.value = parts.value.filter((_, itemIndex) => itemIndex !== index);
  }
</script>

<template>
  <div class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal modal-wide work-completion-modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">WORK COMPLETION</p>
          <h2>作業完了・記録登録</h2>
        </div>
        <button class="close" @click="$emit('close')">×</button>
      </div>
      <p class="sub">記録内容は作業履歴に保存され、報告書として印刷・PDF保存できます。</p>
      <div class="detail-grid work-completion-basics">
        <div>
          <label>作業完了日</label>
          <input v-model="date" type="date" required />
        </div>
        <div>
          <label>作業時間（分）</label>
          <input v-model.number="workMinutes" type="number" min="1" max="1440" step="5" required />
        </div>
      </div>

      <section class="completion-record-section">
        <div class="panel-head">
          <div>
            <h3>作業チェック</h3>
            <p>状態を選び、必要に応じて作業内容を記録してください。</p>
          </div>
          <button class="outline small" @click="addChecklistItem">項目を追加</button>
        </div>
        <div class="completion-checklist">
          <div v-for="(entry, index) in checklist" :key="index" class="completion-check-row">
            <input v-model="entry.item" aria-label="チェック項目" maxlength="100" />
            <select v-model="entry.result" aria-label="チェック結果">
              <option>良好</option>
              <option>調整</option>
              <option>交換</option>
              <option>対象外</option>
            </select>
            <input
              v-model="entry.note"
              aria-label="チェックメモ"
              maxlength="500"
              placeholder="メモ（任意）"
            />
            <button class="outline small" @click="removeChecklistItem(index)">削除</button>
          </div>
          <p v-if="checklist.length === 0" class="muted">チェック項目はありません。</p>
        </div>
      </section>

      <section class="completion-record-section">
        <div class="panel-head">
          <div>
            <h3>使用部品</h3>
            <p>使用した部品がある場合に登録してください。</p>
          </div>
          <button class="outline small" @click="addPart">部品を追加</button>
        </div>
        <div class="completion-parts">
          <div v-for="(part, index) in parts" :key="index" class="completion-part-row">
            <input v-model="part.name" aria-label="部品名" maxlength="100" placeholder="部品名" />
            <input
              v-model.number="part.quantity"
              aria-label="数量"
              type="number"
              min="0.01"
              max="100000"
              step="0.01"
            />
            <input v-model="part.unit" aria-label="単位" maxlength="20" placeholder="個・本など" />
            <input
              v-model.number="part.unitPrice"
              aria-label="部品単価（円）"
              type="number"
              min="0"
              max="100000000"
              step="1"
              placeholder="単価（円）"
            />
            <button class="outline small" @click="removePart(index)">削除</button>
          </div>
          <p v-if="parts.length === 0" class="muted">使用部品はありません。</p>
        </div>
      </section>

      <label for="work-completion-memo">作業メモ</label>
      <textarea
        id="work-completion-memo"
        v-model="memo"
        rows="4"
        maxlength="4000"
        placeholder="追加の作業内容、調整箇所、次回確認事項など"
      ></textarea>
      <p v-if="props.error" class="customer-identity-feedback error">{{ props.error }}</p>
      <div class="modal-note">
        作業完了時に契約回数を1回消費し、入力内容を作業履歴と報告書に保存します。
      </div>
      <div class="modal-actions">
        <button class="outline" @click="$emit('close')">キャンセル</button>
        <button class="primary" @click="$emit('submit')">作業完了として保存</button>
      </div>
    </div>
  </div>
</template>
