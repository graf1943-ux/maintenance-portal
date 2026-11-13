<script setup lang="ts">
  import type { Bike, Plan } from '../../types';

  type BikeCard = {
    bike: Bike;
    plan: Plan;
    remaining: { overhaul: number; maintenance: number; inspection: number };
  };
  type ScheduleRow = {
    id: string;
    bikeMaker: string;
    bikeModel: string;
    kind: string;
    title: string;
    date: string;
    endDate?: string;
    status: string;
    isAdHoc?: boolean;
  };

  defineProps<{
    customerName: string;
    bikeCards: BikeCard[];
    scheduleRows: ScheduleRow[];
    searchText: string;
    dateSortLabel: string;
    statusSortLabel: string;
  }>();

  defineEmits<{
    'update:search-text': [value: string];
    sort: [column: 'date' | 'status'];
  }>();
</script>

<template>
  <div class="page-heading">
    <div>
      <p class="eyebrow">CUSTOMER PORTAL</p>
      <h1>マイページ</h1>
      <p class="sub">ご登録のロードバイクと年間プランをご確認いただけます。</p>
    </div>
    <span class="customer-welcome">{{ customerName }} 様</span>
  </div>
  <section class="panel">
    <div class="panel-head">
      <div>
        <h2>ご契約中のロードバイク</h2>
        <p>{{ customerName }} 様 · {{ bikeCards.length }}台をご契約中</p>
      </div>
      <input
        class="search"
        :value="searchText"
        placeholder="自転車・プランで検索"
        @input="$emit('update:search-text', ($event.target as HTMLInputElement).value)"
      />
      <span class="status success">契約中</span>
    </div>
    <div v-for="card in bikeCards" :key="card.bike.id" class="customer-bike">
      <div class="bike-symbol">🚴</div>
      <div class="equipment-details">
        <b>{{ card.bike.maker }} {{ card.bike.model }}</b>
        <small>
          {{ card.plan.name }} · 契約期間 {{ card.bike.contractStart }}〜{{ card.bike.contractEnd }}
        </small>
        <small>
          オーバーホール残 {{ card.remaining.overhaul }} / {{ card.plan.overhaulCount }}回
        </small>
        <small>
          メンテナンス残 {{ card.remaining.maintenance }} / {{ card.plan.maintenanceCount }}回
        </small>
        <small>
          点検サポート残 {{ card.remaining.inspection }} / {{ card.plan.inspectionCount }}回
        </small>
      </div>
    </div>
    <div v-if="bikeCards.length === 0" class="empty-state">該当する契約自転車はありません。</div>
  </section>
  <section class="panel">
    <div class="panel-head">
      <div>
        <h2>オーバーホール、メンテナンス・点検サポート予定</h2>
        <p>目標イベントに合わせたサポート予定</p>
      </div>
    </div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>対象自転車</th>
            <th>作業区分</th>
            <th>内容</th>
            <th>
              <button class="sort-header" @click="$emit('sort', 'date')">
                開始日 {{ dateSortLabel }} / 完了日
              </button>
            </th>
            <th>
              <button class="sort-header" @click="$emit('sort', 'status')">
                状態 {{ statusSortLabel }}
              </button>
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in scheduleRows" :key="row.id">
            <td>
              <b>{{ row.bikeMaker }} {{ row.bikeModel }}</b>
            </td>
            <td>{{ row.kind }}</td>
            <td>
              {{ row.title }}
              <span v-if="row.isAdHoc" class="status warning">突発作業</span>
            </td>
            <td>{{ row.date }}〜{{ row.endDate || '未定' }}</td>
            <td>{{ row.status }}</td>
          </tr>
          <tr v-if="scheduleRows.length === 0">
            <td colspan="5" class="empty-state">メンテナンス・点検サポート予定はありません。</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
