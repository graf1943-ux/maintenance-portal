<script setup lang="ts">
  import type { Bike, History } from '../../types';

  type HistoryRow = History & { bike?: Bike };

  defineProps<{
    rows: HistoryRow[];
    count: number;
    searchText: string;
    dateSortLabel: string;
    selectedBikeName: string;
  }>();

  defineEmits<{
    'update:search-text': [value: string];
    sort: [];
    'clear-bike-filter': [];
    'open-report': [history: HistoryRow];
  }>();
</script>

<template>
  <div class="page-heading">
    <div>
      <p class="eyebrow">SERVICE HISTORY</p>
      <h1>作業履歴</h1>
      <p class="sub">オーバーホール・メンテナンス・点検サポートの作業履歴を確認できます。</p>
    </div>
    <div v-if="selectedBikeName" class="selected-bike-filter">
      <span class="status info">対象自転車：{{ selectedBikeName }}</span>
      <button class="outline small" @click="$emit('clear-bike-filter')">自転車指定を解除</button>
    </div>
  </div>
  <section class="panel">
    <div class="panel-head">
      <div>
        <h2>作業履歴一覧</h2>
        <p>顧客名・自転車名・作業内容で絞り込めます</p>
      </div>
      <input
        class="search"
        :value="searchText"
        placeholder="顧客名・自転車名・作業区分・作業名・詳細で検索"
        @input="$emit('update:search-text', ($event.target as HTMLInputElement).value)"
      />
      <span class="count">{{ count }}件</span>
    </div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>
              <button class="sort-header" @click="$emit('sort')">
                開始日 {{ dateSortLabel }} / 完了日
              </button>
            </th>
            <th>顧客</th>
            <th>自転車</th>
            <th>作業区分</th>
            <th>作業内容</th>
            <th>作業時のメモ</th>
            <th>詳細</th>
            <th>報告書</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="history in rows" :key="history.id">
            <td>
              <b>{{ history.startDate || history.date }}</b>
              <small v-if="history.startDate" class="cell-sub">〜 {{ history.date }}</small>
            </td>
            <td>
              <span v-if="history.bike">{{ history.bike.customer }}</span>
              <span v-else>—</span>
            </td>
            <td>
              <span v-if="history.bike"> {{ history.bike.maker }} {{ history.bike.model }} </span>
              <span v-else>—</span>
            </td>
            <td>
              {{ history.category }}
              <span v-if="history.isAdHoc" class="status warning">突発作業</span>
            </td>
            <td>{{ history.title }}</td>
            <td class="memo-cell">{{ history.memo || '—' }}</td>
            <td>{{ history.detail }}</td>
            <td>
              <button
                v-if="history.category !== '作業中止'"
                class="outline small"
                @click="$emit('open-report', history)"
              >
                報告書
              </button>
              <span v-else>—</span>
            </td>
          </tr>
          <tr v-if="rows.length === 0">
            <td colspan="8" class="empty-state">該当する作業履歴はありません。</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
