<script setup lang="ts">
  const date = defineModel<string>('date', { required: true });
  const reason = defineModel<string>('reason', { required: true });

  defineEmits<{ close: []; submit: [] }>();
</script>

<template>
  <div class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">STOP WORK</p>
          <h2>作業中止を記録</h2>
        </div>
        <button class="close" @click="$emit('close')">×</button>
      </div>
      <p class="sub">作業開始後の中止として記録します。中止では契約回数を消費しません。</p>
      <label>中止日</label>
      <input v-model="date" type="date" />
      <label>中止理由（任意）</label>
      <textarea v-model="reason" rows="4" placeholder="中止理由や再受付時の申し送り"></textarea>
      <div class="modal-note">
        この作業は終了状態になります。再開する場合は新しい作業として登録してください。
      </div>
      <div class="modal-actions">
        <button class="outline" @click="$emit('close')">キャンセル</button>
        <button class="primary" @click="$emit('submit')">作業中止として保存</button>
      </div>
    </div>
  </div>
</template>
