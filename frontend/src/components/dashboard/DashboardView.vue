<script setup lang="ts">
  import type { Bike } from '../../types';
  import type { DashboardReminder } from '../../composables/useDashboardData';

  type DashboardRow = {
    id: number;
    bikeId: number;
    kind: string;
    date: string;
    endDate?: string;
    title: string;
    status: string;
    isAdHoc?: boolean;
  };
  type CustomerFollowUp = {
    id: number;
    customerId: number;
    customerName?: string | null;
    channel: string;
    summary: string;
    followUpDate?: string | null;
    followUpStaffName?: string | null;
  };

  defineProps<{
    bikes: Bike[];
    bikesCount: number;
    dashboardRows: DashboardRow[];
    reminders: DashboardReminder[];
    customerFollowUps: CustomerFollowUp[];
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
    'open-bike': [bikeId: number];
  }>();

  function isOverdueFollowUp(date?: string | null) {
    if (!date) return false;
    const today = new Date();
    today.setMinutes(today.getMinutes() - today.getTimezoneOffset());
    return date < today.toISOString().slice(0, 10);
  }
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
      <strong>{{ bikesCount }}</strong><small>台</small>
    </div>
    <div class="stat-card">
      <div class="stat-top">
        <span>未完了の作業予定</span><span class="stat-icon purple">⚙</span>
      </div>
      <strong>{{ upcomingCount }}</strong><small>件</small>
    </div>
    <div class="stat-card">
      <div class="stat-top"><span>案内時期</span><span class="stat-icon orange">✉</span></div>
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
        <h2>要対応のリマインダー</h2>
        <p>開始予定日の7日前から表示し、期限超過を優先します。</p>
      </div>
      <span class="status warning">{{ reminders.length }}件</span>
    </div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>顧客</th>
            <th>自転車</th>
            <th>区分・内容</th>
            <th>予定日</th>
            <th>要対応の理由</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="reminder in reminders" :key="reminder.id">
            <td>
              <button
                class="customer-link"
                @click="
                  $emit(
                    'open-customer',
                    bikes.find((bike) => bike.id === reminder.bikeId)?.customer || ''
                  )
                "
              >
                {{ bikes.find((bike) => bike.id === reminder.bikeId)?.customer }}
              </button>
            </td>
            <td>
              <button class="customer-link" @click="$emit('open-bike', reminder.bikeId)">
                {{ bikes.find((bike) => bike.id === reminder.bikeId)?.maker }}
                {{ bikes.find((bike) => bike.id === reminder.bikeId)?.model }}
              </button>
            </td>
            <td>
              <b>{{ reminder.kind }}</b><small class="cell-sub">{{ reminder.title }}</small>
            </td>
            <td>{{ reminder.date || '未定' }}</td>
            <td>
              <span class="status warning">{{ reminder.reason }}</span>
            </td>
          </tr>
          <tr v-if="reminders.length === 0">
            <td colspan="5" class="empty-state">現在、要対応の項目はありません。</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
  <section class="panel">
    <div class="panel-head">
      <div>
        <h2>顧客フォローアップ</h2>
        <p>未対応の次回連絡予定。期限の近い順に表示します。</p>
      </div>
      <span class="status warning">{{ customerFollowUps.length }}件</span>
    </div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>顧客</th>
            <th>次回連絡日</th>
            <th>連絡方法・内容</th>
            <th>担当者</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="followUp in customerFollowUps.slice(0, 8)" :key="followUp.id">
            <td>
              <button
                class="customer-link"
                @click="$emit('open-customer', followUp.customerName || '')"
              >
                {{ followUp.customerName || '顧客' }}
              </button>
            </td>
            <td>
              <span
                class="status"
                :class="isOverdueFollowUp(followUp.followUpDate) ? 'danger' : 'warning'"
              >
                {{ (followUp.followUpDate || '').replace(/-/g, '/') }}
              </span>
            </td>
            <td>
              <b>{{ followUp.channel }}</b>
              <small class="cell-sub">{{ followUp.summary }}</small>
            </td>
            <td>{{ followUp.followUpStaffName || '未指定' }}</td>
          </tr>
          <tr v-if="customerFollowUps.length === 0">
            <td colspan="4" class="empty-state">未対応のフォローアップはありません。</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
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
                  $emit('open-customer', bikes.find((bike) => bike.id === w.bikeId)?.customer || '')
                "
              >
                {{ bikes.find((bike) => bike.id === w.bikeId)?.customer }}
              </button>
            </td>
            <td>
              <button class="customer-link" @click="$emit('open-bike', w.bikeId)">
                {{ bikes.find((bike) => bike.id === w.bikeId)?.maker }}
                {{ bikes.find((bike) => bike.id === w.bikeId)?.model }}
              </button>
            </td>
            <td>{{ w.kind }} <span v-if="w.isAdHoc" class="status warning">突発作業</span></td>
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
