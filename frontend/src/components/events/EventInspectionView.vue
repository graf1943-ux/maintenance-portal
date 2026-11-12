<script setup lang="ts">
  import type { Bike, EventCheck } from '../../types';

  type EventInspectionRow = {
    event: EventCheck;
    bike: Bike;
    remaining: number;
    shopName: string;
  };

  defineProps<{
    rows: EventInspectionRow[];
    count: number;
    customerFilterText: string;
    bikeFilterText: string;
    selectedBikeFilter: number | null;
    searchText: string;
    customerSortLabel: string;
    dateSortLabel: string;
    statusSortLabel: string;
  }>();

  defineEmits<{
    'update:customer-filter-text': [value: string];
    'update:bike-filter-text': [value: string];
    'update:search-text': [value: string];
    'clear-filters': [];
    sort: [field: 'customer' | 'date' | 'status'];
    'open-customer': [name: string];
    edit: [event: EventCheck];
    'start-work': [event: EventCheck];
    complete: [event: EventCheck];
  }>();
</script>

<template>
  <div class="page-heading">
    <div>
      <p class="eyebrow">TARGET EVENTS & INSPECTIONS</p>
      <h1>目標イベント・点検サポート</h1>
      <p class="sub">顧客のメインイベントと、その前に行う点検サポートを管理します。</p>
    </div>
  </div>
  <div class="callout">
    <b>点検サポートはメンテナンスとは別枠</b>
    <p>
      点検サポートの実施完了時にのみ、プランの点検回数を1回消費します。メンテナンス回数とは相互に影響しません。
    </p>
  </div>
  <div class="list-toolbar">
    <span class="muted">顧客詳細から登録したイベント・点検予定の一覧です。</span>
  </div>
  <section class="panel">
    <div class="panel-head">
      <div>
        <h2>イベント・点検サポート一覧</h2>
        <p>点検完了時に点検サポート枠を消費</p>
      </div>
      <div class="list-name-filters">
        <input
          class="search"
          :value="customerFilterText"
          placeholder="顧客名で検索"
          @input="$emit('update:customer-filter-text', ($event.target as HTMLInputElement).value)"
        />
        <input
          class="search"
          :value="bikeFilterText"
          placeholder="自転車名・メーカーで検索"
          @input="$emit('update:bike-filter-text', ($event.target as HTMLInputElement).value)"
        />
        <button
          v-if="customerFilterText || bikeFilterText || selectedBikeFilter"
          class="outline small"
          @click="$emit('clear-filters')"
        >
          条件クリア
        </button>
      </div>
      <input
        class="search"
        :value="searchText"
        placeholder="イベント名・メモ・店舗・担当者で検索"
        @input="$emit('update:search-text', ($event.target as HTMLInputElement).value)"
      />
      <span class="count">{{ count }}件</span>
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
            <th>イベント名</th>
            <th>開催日</th>
            <th>
              <button class="sort-header" @click="$emit('sort', 'date')">
                点検開始日 {{ dateSortLabel }}
              </button>
              / 完了日
            </th>
            <th>
              <button class="sort-header" @click="$emit('sort', 'status')">
                状態 {{ statusSortLabel }}
              </button>
            </th>
            <th>新規登録可能枠</th>
            <th>受付担当</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in rows" :key="row.event.id">
            <td>
              <button class="customer-link" @click="$emit('open-customer', row.bike.customer)">
                {{ row.bike.customer }}
              </button>
            </td>
            <td>{{ row.bike.model }}</td>
            <td>
              <b>{{ row.event.name }}</b><small class="cell-sub">{{ row.event.memo }}</small>
            </td>
            <td>{{ row.event.eventDate }}</td>
            <td>
              {{ row.event.inspectionDate
              }}<small class="cell-sub">〜 {{ row.event.endDate || '未定' }}</small>
            </td>
            <td>
              <span class="status info">{{ row.event.status }}</span>
            </td>
            <td>{{ row.remaining }}回</td>
            <td>{{ row.shopName }} / {{ row.event.receptionist }}</td>
            <td>
              <button
                v-if="row.event.status !== '完了'"
                class="outline small"
                @click="$emit('edit', row.event)"
              >
                編集
              </button>
              <button
                v-if="['予定', '予約済み', '案内済み'].includes(row.event.status)"
                class="outline small"
                @click="$emit('start-work', row.event)"
              >
                作業開始
              </button>
              <button
                v-if="row.event.status === '作業中'"
                class="primary small"
                @click="$emit('complete', row.event)"
              >
                完了登録
              </button>
              <span v-if="row.event.status === '完了'" class="done">✓ 完了</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
