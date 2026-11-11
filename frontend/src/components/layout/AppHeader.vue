<script setup lang="ts">
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
