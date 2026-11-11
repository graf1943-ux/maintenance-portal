<script setup lang="ts">
  import type { Bike, History } from '../../types';

  defineProps<{
    bikes: Bike[];
    selectedBikeId: number;
    selectedBike: Bike;
    rows: History[];
    count: number;
    searchText: string;
    dateSortLabel: string;
  }>();

  defineEmits<{
    'update:selected-bike-id': [value: number];
    'update:search-text': [value: string];
    sort: [];
  }>();
</script>

<template>
  <div class="page-heading">
    <div>
      <p class="eyebrow">SERVICE HISTORY</p>
      <h1>メンテナンス履歴</h1>
      <p class="sub">自転車ごとのオーバーホール・メンテナンス・点検サポートの履歴です。</p>
    </div>
    <select
      class="search"
      :value="selectedBikeId"
      @change="$emit('update:selected-bike-id', Number(($event.target as HTMLSelectElement).value))"
    >
      <option v-for="bike in bikes" :key="bike.id" :value="bike.id">
        {{ bike.customer }} / {{ bike.model }}
      </option>
    </select>
  </div>
  <section class="panel">
    <div class="panel-head">
      <div>
        <h2>{{ selectedBike.maker }} {{ selectedBike.model }}</h2>
        <p>{{ selectedBike.customer }} 様の作業履歴</p>
      </div>
      <input
        class="search"
        :value="searchText"
        placeholder="作業区分・作業名・詳細で検索"
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
                開始日 {{ dateSortLabel }} / 完了日 {{ dateSortLabel }}
              </button>
            </th>
            <th>作業区分</th>
            <th>作業内容</th>
            <th>作業時のメモ</th>
            <th>詳細</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="history in rows" :key="history.id">
            <td>
              <b>{{ history.startDate || history.date }}</b>
              <small class="cell-sub">〜 {{ history.date }}</small>
            </td>
            <td>{{ history.category }}</td>
            <td>{{ history.title }}</td>
            <td class="memo-cell">{{ history.memo || '—' }}</td>
            <td>{{ history.detail }}</td>
          </tr>
          <tr v-if="rows.length === 0">
            <td colspan="5" class="empty-state">該当する作業履歴はありません。</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
