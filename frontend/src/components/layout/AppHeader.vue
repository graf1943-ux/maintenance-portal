<script setup lang="ts">
  import { t } from '../../locales/ja';
  defineProps<{ pageTitle: string; today: string }>();
</script>

<template>
  <header class="topbar">
    <div class="breadcrumb">ROAD BIKE CARE <span>/</span> {{ pageTitle }}</div>
    <div class="top-actions">
      <span class="today">{{ today }}</span>
      <div class="avatar">店</div>
    </div>
  </header>
</template>
{{ t('店') }}
