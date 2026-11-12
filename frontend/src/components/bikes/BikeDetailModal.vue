<script setup lang="ts">
  import type { Bike, Plan } from '../../types';

  defineProps<{
    bike: Bike;
    plan: Plan;
    shopName: string;
    remaining: { overhaul: number; maintenance: number; inspection: number };
  }>();

  defineEmits<{ close: [] }>();
</script>

<template>
  <div class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal modal-wide bike-detail-modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">CONTRACT DETAILS</p>
          <h2>契約詳細</h2>
          <p class="sub">選択した自転車の契約内容とサービス残回数</p>
        </div>
        <button class="close" @click="$emit('close')">×</button>
      </div>
      <div class="detail-grid">
        <div>
          <label>顧客</label><b>{{ bike.customer }}</b>
        </div>
        <div>
          <label>自転車</label><b>{{ bike.maker }} {{ bike.model }}</b>
        </div>
        <div>
          <label>プラン</label><b>{{ plan.name }}</b>
        </div>
        <div>
          <label>契約期間</label><b>{{ bike.contractStart }}〜{{ bike.contractEnd }}</b>
        </div>
        <div>
          <label>担当店舗・受付</label><b>{{ shopName }} / {{ bike.receptionist }}</b>
        </div>
        <div>
          <label>オーバーホール残</label>
          <b class="remaining large">{{ remaining.overhaul }} / {{ plan.overhaulCount }}回</b>
        </div>
        <div>
          <label>メンテナンス残</label>
          <b class="remaining large">
            {{ remaining.maintenance }} / {{ plan.maintenanceCount }}回
          </b>
        </div>
        <div>
          <label>点検サポート残</label>
          <b class="remaining large">{{ remaining.inspection }} / {{ plan.inspectionCount }}回</b>
        </div>
        <div>
          <label>オーバーホール完了日</label><b>{{ bike.overhaulCompleted || '未実施' }}</b>
        </div>
        <div>
          <label>車体メーカー・モデル</label><b>{{ bike.maker }} {{ bike.model }}</b>
        </div>
        <div>
          <label>年式</label><b>{{ bike.year }}年</b>
        </div>
        <div>
          <label>シリアル番号</label><b>{{ bike.serial || '未登録' }}</b>
        </div>
      </div>
      <div class="modal-actions">
        <button class="outline" @click="$emit('close')">閉じる</button>
      </div>
    </div>
  </div>
</template>
