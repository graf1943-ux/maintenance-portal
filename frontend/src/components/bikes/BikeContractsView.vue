<script setup lang="ts">
  import type { Bike } from '../../types';

  type ContractRow = {
    bike: Bike;
    planName: string;
    overhaulCount: number;
    maintenanceCount: number;
    inspectionCount: number;
    shopName: string;
    customerBikeCount: number;
    overhaulRemaining: number;
    maintenanceRemaining: number;
    inspectionRemaining: number;
  };

  defineProps<{
    customersCount: number;
    bikesCount: number;
    searchText: string;
    sortLabel: string;
    rows: ContractRow[];
  }>();

  defineEmits<{
    'update:searchText': [value: string];
    sort: [];
    'add-contract': [];
    'open-customer': [name: string];
    'open-bike': [bikeId: number];
  }>();
</script>

<template>
  <div class="page-heading">
    <div>
      <p class="eyebrow">CUSTOMERS & BICYCLES</p>
      <h1>顧客・自転車・契約管理</h1>
      <p class="sub">自転車ごとに年間プランと契約期間、各サービスの残回数を管理します。</p>
    </div>
    <button class="primary" @click="$emit('add-contract')">＋ 年間契約を登録</button>
  </div>
  <div class="callout">
    <b>年間契約の登録と管理をこの画面に集約</b>
    <p>
      新しい契約は「年間契約を登録」から追加できます。契約期間は12月1日〜翌年11月30日、受付期限は契約開始年の12月31日です。登録後はオーバーホールの受付・実施予定を設定します。
    </p>
  </div>
  <section class="panel">
    <div class="panel-head">
      <div>
        <h2>年間契約一覧</h2>
        <p>{{ customersCount }}名・{{ bikesCount }}台の自転車が登録されています</p>
      </div>
      <input
        class="search"
        :value="searchText"
        @input="$emit('update:searchText', ($event.target as HTMLInputElement).value)"
        placeholder="顧客・自転車・プラン・店舗で検索"
      />
      <span class="count">契約期間：12月1日〜翌年11月30日</span>
    </div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>
              <button class="sort-header" @click="$emit('sort')">
                顧客・自転車 {{ sortLabel }}
              </button>
            </th>
            <th>契約プラン</th>
            <th>契約期間</th>
            <th>担当店舗・受付</th>
            <th>OH残</th>
            <th>メンテ残</th>
            <th>点検残</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in rows" :key="row.bike.id">
            <td>
              <button class="customer-link" @click="$emit('open-customer', row.bike.customer)">
                {{ row.bike.customer }}
                <small v-if="row.customerBikeCount > 1" class="multi-bike-tag">
                  {{ row.customerBikeCount }}台契約
                </small>
              </button>
              <small class="cell-sub">{{ row.bike.maker }} {{ row.bike.model }}</small>
            </td>
            <td>{{ row.planName }}</td>
            <td>{{ row.bike.contractStart }}〜{{ row.bike.contractEnd }}</td>
            <td>{{ row.shopName }} / {{ row.bike.receptionist }}</td>
            <td>{{ row.overhaulRemaining }}/{{ row.overhaulCount }}</td>
            <td>{{ row.maintenanceRemaining }}/{{ row.maintenanceCount }}</td>
            <td>{{ row.inspectionRemaining }}/{{ row.inspectionCount }}</td>
            <td>
              <button class="outline small" @click="$emit('open-bike', row.bike.id)">詳細</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
