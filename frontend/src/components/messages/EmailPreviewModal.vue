<script setup lang="ts">
  defineProps<{
    customers: { name: string }[];
    target: string;
    subject: string;
  }>();

  defineEmits<{
    'update:target': [value: string];
    'update:subject': [value: string];
    close: [];
    send: [];
  }>();
</script>

<template>
  <div class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">EMAIL PREVIEW</p>
          <h2>メンテナンス案内メール</h2>
        </div>
        <button class="close" @click="$emit('close')">×</button>
      </div>
      <label>送信先</label>
      <select
        :value="target"
        @change="$emit('update:target', ($event.target as HTMLSelectElement).value)"
      >
        <option v-for="customer in customers" :key="customer.name">{{ customer.name }}</option>
      </select>
      <label>件名</label>
      <input
        :value="subject"
        @input="$emit('update:subject', ($event.target as HTMLInputElement).value)"
      />
      <label>本文</label>
      <div class="mail-body">
        {{ target }} 様<br /><br />
        いつもご利用いただきありがとうございます。<br />
        メンテナンス・点検サポート予定についてご案内いたします。<br /><br />
        ご希望の日程がございましたら店舗までご連絡ください。<br /><br />
        今後ともよろしくお願いいたします。
      </div>
      <div class="modal-note">※ モックのため、実際のメール送信は行われません。</div>
      <div class="modal-actions">
        <button class="outline" @click="$emit('close')">閉じる</button>
        <button class="primary" @click="$emit('send')">送信（デモ）</button>
      </div>
    </div>
  </div>
</template>
