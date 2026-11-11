<script setup lang="ts">
  import type { Bike, Work } from '../../types';

  type OverhaulRow = { work: Work; bike: Bike; remaining: number };

  defineProps<{
    rows: OverhaulRow[];
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
    edit: [work: Work];
    'start-work': [work: Work];
    complete: [work: Work];
  }>();
</script>

<template>
  <div class="page-heading">
    <div>
      <p class="eyebrow">OVERHAUL MANAGEMENT</p>
      <h1>オーバーホール</h1>
      <p class="sub">年間プランに含まれるオーバーホールの予定・実績を管理します。</p>
    </div>
  </div>
  <div class="callout">
    <b>受付期限：契約開始年の12月31日 ／ 基本実施期間：契約開始年11月1日〜翌年2月末</b>
    <p>
      オーバーホールの受付は12月末まで可能で、11月中の契約・受付・実施もシステム上登録できます。実施は契約開始年の11月から翌年2月末までです。オーバーホールとメンテナンスは同じ年間プランに含まれます。完了日は記録しますが、その日から一定間隔でメンテナンス予定を自動生成することはありません。
    </p>
  </div>
  <div class="list-toolbar">
    <span class="muted">顧客詳細から登録した予定・実績の一覧です。</span>
  </div>
  <section class="panel">
    <div class="panel-head">
      <div>
        <h2>オーバーホール予定・実績</h2>
        <p>実施完了時にオーバーホール回数を消費</p>
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
        placeholder="予定名・作業内容・店舗・担当者で検索"
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
            <th>作業内容</th>
            <th>
              <button class="sort-header" @click="$emit('sort', 'date')">
                開始日 {{ dateSortLabel }}
              </button>
              / 完了日
            </th>
            <th>
              <button class="sort-header" @click="$emit('sort', 'status')">
                状態 {{ statusSortLabel }}
              </button>
            </th>
            <th>新規登録可能枠</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in rows" :key="row.work.id">
            <td>
              <button class="customer-link" @click="$emit('open-customer', row.bike.customer)">
                {{ row.bike.customer }}
              </button>
            </td>
            <td>{{ row.bike.model }}</td>
            <td>
              <b>{{ row.work.title }}</b
              ><small class="cell-sub">{{ row.work.detail }}</small>
            </td>
            <td>
              {{ row.work.date }}<small class="cell-sub">〜 {{ row.work.endDate || '未定' }}</small>
            </td>
            <td>
              <span class="status info">{{ row.work.status }}</span>
            </td>
            <td>{{ row.remaining }}回</td>
            <td>
              <button
                v-if="row.work.status !== '完了'"
                class="outline small"
                @click="$emit('edit', row.work)"
              >
                編集
              </button>
              <button
                v-if="
                  row.work.status === '予定' ||
                  row.work.status === '予約済み' ||
                  row.work.status === '案内済み'
                "
                class="outline small"
                @click="$emit('start-work', row.work)"
              >
                作業開始
              </button>
              <button
                v-if="row.work.status === '作業中'"
                class="primary small"
                @click="$emit('complete', row.work)"
              >
                完了登録
              </button>
              <span v-if="row.work.status === '完了'" class="done">✓ 完了</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
