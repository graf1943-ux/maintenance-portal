<script setup lang="ts">
  const date = defineModel<string>('date', { required: true });
  const memo = defineModel<string>('memo', { required: true });

  defineEmits<{ close: []; submit: [] }>();
</script>

<template>
  <div class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">WORK COMPLETION</p>
          <h2>作業完了・メモ登録</h2>
        </div>
        <button class="close" @click="$emit('close')">×</button>
      </div>
      <p class="sub">作業内容や交換部品、調整箇所、次回確認事項などを記録できます。</p>
      <label>作業完了日</label><input v-model="date" type="date" />
      <label>作業時のメモ</label>
      <textarea
        v-model="memo"
        rows="6"
        placeholder="例：チェーン・スプロケットを洗浄。リアブレーキパッド交換。変速調整済み。次回はタイヤ摩耗を確認。"
      ></textarea>
      <div class="modal-note">
        メモは作業履歴に保存され、後から「メンテナンス履歴」画面で確認できます。
      </div>
      <div class="modal-actions">
        <button class="outline" @click="$emit('close')">キャンセル</button>
        <button class="primary" @click="$emit('submit')">作業完了として保存</button>
      </div>
    </div>
  </div>
</template>
