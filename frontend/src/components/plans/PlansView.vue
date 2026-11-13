<script setup lang="ts">
  import type { Plan } from '../../types';

  defineProps<{ plans: Plan[] }>();
  defineEmits<{ 'add-plan': [] }>();
</script>

<template>
  <div class="page-heading">
    <div>
      <p class="eyebrow">PLAN MASTER</p>
      <h1>プラン設定</h1>
      <p class="sub">
        プランごとにオーバーホール・メンテナンス・点検サポートの回数を別々に設定します。
      </p>
    </div>
    <button class="primary" @click="$emit('add-plan')">＋ プランを追加</button>
  </div>
  <div class="callout">
    <b>回数枠はそれぞれ独立</b>
    <p>
      メンテナンスを実施しても点検サポートの回数は減りません。契約終了時の残回数は失効し、翌年度への繰越はありません。
    </p>
  </div>
  <div class="section-grid">
    <section v-for="plan in plans" :key="plan.id" class="panel">
      <div class="panel-head">
        <div>
          <h2>{{ plan.name }}</h2>
          <p>年間契約プラン</p>
        </div>
        <span class="status success">有効</span>
      </div>
      <div class="detail-grid">
        <div>
          <label>オーバーホール</label>
          <b class="large">{{ plan.overhaulCount }}回</b>
        </div>
        <div>
          <label>メンテナンス</label>
          <b class="large">{{ plan.maintenanceCount }}回</b>
        </div>
        <div>
          <label>点検サポート</label>
          <b class="large">{{ plan.inspectionCount }}回</b>
        </div>
      </div>
    </section>
    <div v-if="plans.length === 0" class="empty-state">登録されているプランはありません。</div>
  </div>
  <div class="panel-foot">
    ※
    追加したプランは年間契約登録時のプラン選択肢に反映されます。既存契約のプラン内容は、新しいプランを追加しても変更されません。
  </div>
</template>
