<script setup lang="ts">
  import type { Bike } from '../../types';

  type DashboardRow = {
    id: number;
    bikeId: number;
    kind: string;
    date: string;
    endDate?: string;
    title: string;
    status: string;
  };

  defineProps<{
    bikes: Bike[];
    dashboardRows: DashboardRow[];
    upcomingCount: number;
    pendingCount: number;
    completedCount: number;
    customerSortLabel: string;
    dateSortLabel: string;
    statusSortLabel: string;
  }>();

  defineEmits<{
    sort: [field: 'customer' | 'date' | 'status'];
    'open-customer': [name: string];
  }>();
</script>

<template>
  <div class="page-heading">
    <div>
      <p class="eyebrow">ROAD BIKE SERVICE OVERVIEW</p>
      <h1>ダッシュボード</h1>
      <p class="sub">年間契約・オーバーホール・目標イベントに向けた作業計画を管理します。</p>
    </div>
  </div>
  <div class="callout">
    <b>年間契約受付：12月1日〜12月31日 ／ 契約期間：12月1日〜翌年11月30日</b>
    <p>
      オーバーホールは11月〜翌年2月末に実施（受付は12月31日まで）。メンテナンス・点検サポートは目標イベントに合わせて計画します。
    </p>
  </div>
  <div class="stats">
    <div class="stat-card">
      <div class="stat-top"><span>登録自転車数</span><span class="stat-icon blue">🚴</span></div>
      <strong>{{ bikes.length }}</strong><small>台</small>
    </div>
    <div class="stat-card">
      <div class="stat-top">
        <span>未完了の作業予定</span><span class="stat-icon purple">⚙</span>
      </div>
      <strong>{{ upcomingCount }}</strong><small>件</small>
    </div>
    <div class="stat-card">
      <div class="stat-top"><span>案内待ち</span><span class="stat-icon orange">✉</span></div>
      <strong>{{ pendingCount }}</strong><small>件</small>
    </div>
    <div class="stat-card">
      <div class="stat-top"><span>作業履歴</span><span class="stat-icon green">✓</span></div>
      <strong>{{ completedCount }}</strong><small>件</small>
    </div>
  </div>
  <section class="panel">
    <div class="panel-head">
      <div>
        <h2>直近の作業予定</h2>
        <p>オーバーホール・メンテナンス・イベント前点検</p>
      </div>
    </div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>
              <button class="sort-header" @click="$emit('sort', 'customer')">
                顧客 {{ customerSortLabel }}
              </button>
            </th>
            <th>自転車</th>
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
          <tr v-for="w in dashboardRows" :key="w.id + '-' + w.kind">
            <td>
              <button
                class="customer-link"
                @click="
                  $emit('open-customer', bikes.find((b) => b.id === w.bikeId)?.customer || '')
                "
              >
                {{ bikes.find((b) => b.id === w.bikeId)?.customer }}
              </button>
            </td>
            <td>{{ bikes.find((b) => b.id === w.bikeId)?.model }}</td>
            <td>{{ w.kind }}</td>
            <td>{{ w.title }}</td>
            <td>
              <b>{{ w.date }}</b><small class="cell-sub">〜 {{ w.endDate || '未定' }}</small>
            </td>
            <td>
              <span class="status info">{{ w.status }}</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
