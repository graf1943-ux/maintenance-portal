<script setup lang="ts">
  import type { Bike, Status, Work } from '../../types';

  type MaintenanceRow = { work: Work; bike: Bike; remaining: number; shopName: string };

  defineProps<{
    rows: MaintenanceRow[];
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
    'open-bike': [bikeId: number];
    'status-change': [work: Work, status: Status];
    edit: [work: Work];
    'start-work': [work: Work];
    complete: [work: Work];
    stop: [work: Work];
  }>();
</script>

<template>
  <div class="page-heading">
    <div>
      <p class="eyebrow">MAINTENANCE PLANNING</p>
      <h1>メンテナンス計画</h1>
      <p class="sub">顧客の目標イベントに合わせてスタッフが実施計画を作成します。</p>
    </div>
  </div>
  <div class="callout">
    <b>固定周期ではなく、目標イベントに合わせた計画</b>
    <p>
      2か月おきの自動生成は行いません。メインイベントの日程・目標・車体の状態を踏まえて必要なタイミングで計画します。実施完了時のみメンテナンス回数を消費します。
    </p>
  </div>
  <div class="list-toolbar">
    <span class="muted">顧客詳細から登録した計画の一覧です。</span>
  </div>
  <section class="panel">
    <div class="panel-head">
      <div>
        <h2>メンテナンス計画一覧</h2>
        <p>案内待ち・案内済み・予約は回数を消費しません</p>
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
        placeholder="計画名・作業内容・店舗・担当者で検索"
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
            <th>計画・作業内容</th>
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
            <th>受付担当者</th>
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
            <td>
              <button class="customer-link" @click="$emit('open-bike', row.bike.id)">
                {{ row.bike.maker }} {{ row.bike.model }}
              </button>
            </td>
            <td>
              <b>{{ row.work.title }}</b>
              <small class="cell-sub">{{ row.work.detail }}</small>
              <span v-if="row.work.isAdHoc" class="status warning">突発作業</span>
            </td>
            <td>
              <b>{{ row.work.date }}</b>
              <small class="cell-sub">〜 {{ row.work.endDate || '未定' }}</small>
            </td>
            <td>
              <select
                v-if="row.work.status !== '完了' && row.work.status !== '作業中止'"
                class="status-select"
                :value="row.work.status"
                @change="
                  $emit(
                    'status-change',
                    row.work,
                    ($event.target as HTMLSelectElement).value as Status
                  )
                "
              >
                <option>案内待ち</option>
                <option>案内済み</option>
                <option>予約済み</option>
                <option>作業中</option>
                <option>キャンセル</option>
              </select>
              <span v-else-if="row.work.status === '作業中止'" class="status warning">
                作業中止
              </span>
              <span v-else class="done">✓ 完了</span>
            </td>
            <td>{{ row.remaining }}回</td>
            <td>{{ row.shopName }} / {{ row.work.receptionist }}</td>
            <td>
              <button
                v-if="row.work.status !== '完了' && row.work.status !== '作業中止'"
                class="outline small"
                @click="$emit('edit', row.work)"
              >
                編集
              </button>
              <button
                v-if="['案内待ち', '予約済み', '案内済み'].includes(row.work.status)"
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
              <button
                v-if="row.work.status === '作業中'"
                class="outline small"
                @click="$emit('stop', row.work)"
              >
                作業中止
              </button>
              <span v-if="row.work.status === '完了'" class="done">✓ 完了</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
