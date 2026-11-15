<script setup lang="ts">
  import type { PortalRole } from '../../auth';

  defineProps<{
    loading: boolean;
    error: string;
    customerEnabled: boolean;
    staffEnabled: boolean;
    authenticated: boolean;
  }>();

  defineEmits<{
    signIn: [role: PortalRole];
    signOut: [];
  }>();
</script>

<template>
  <main class="auth-screen">
    <section class="auth-panel" aria-labelledby="auth-title">
      <div class="auth-brand">
        <span class="brand-mark">R</span>
        <span>ROAD BIKE CARE</span>
      </div>
      <p class="eyebrow">メンテナンスポータル</p>
      <h1 id="auth-title">ログイン</h1>
      <p class="auth-intro">ご利用のアカウントを選択してください。</p>
      <div class="auth-options">
        <button
          class="auth-option customer"
          :disabled="loading || !customerEnabled"
          @click="$emit('signIn', 'customer')"
        >
          <span class="auth-option-icon" aria-hidden="true">人</span>
          <span class="auth-option-copy">
            <strong>お客様</strong>
            <small>ご予約や自転車の情報を確認</small>
          </span>
          <span class="auth-arrow" aria-hidden="true">›</span>
        </button>
        <button
          class="auth-option staff"
          :disabled="loading || !staffEnabled"
          @click="$emit('signIn', 'staff')"
        >
          <span class="auth-option-icon" aria-hidden="true">店</span>
          <span class="auth-option-copy">
            <strong>店舗スタッフ</strong>
            <small>店舗の業務管理はこちら</small>
          </span>
          <span class="auth-arrow" aria-hidden="true">›</span>
        </button>
      </div>
      <p v-if="!customerEnabled && !staffEnabled" class="auth-feedback error">
        サインイン設定が完了していません。
      </p>
      <p v-if="authenticated" class="auth-session">
        <button class="outline small" @click="$emit('signOut')">別のアカウントでサインイン</button>
      </p>
      <p v-if="loading" class="auth-feedback" role="status">認証中...</p>
      <p v-else-if="error" class="auth-feedback error" role="alert">{{ error }}</p>
    </section>
  </main>
</template>
