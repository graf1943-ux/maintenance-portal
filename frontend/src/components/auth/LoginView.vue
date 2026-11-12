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
      <p class="eyebrow">MAINTENANCE PORTAL</p>
      <h1 id="auth-title">サインイン</h1>
      <div class="auth-options">
        <button
          class="auth-option customer"
          :disabled="loading || !customerEnabled"
          @click="$emit('signIn', 'customer')"
        >
          <span>顧客アカウント</span>
          <span class="auth-arrow" aria-hidden="true">→</span>
        </button>
        <button
          class="auth-option staff"
          :disabled="loading || !staffEnabled"
          @click="$emit('signIn', 'staff')"
        >
          <span>店舗スタッフ</span>
          <span class="auth-arrow" aria-hidden="true">→</span>
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
