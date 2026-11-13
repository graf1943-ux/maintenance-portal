<script setup lang="ts">
  type MessageRow = {
    id: string;
    bikeId: number;
    customer: string;
    bikeModel: string;
    kind: string;
    title: string;
    date: string;
  };

  defineProps<{
    rows: MessageRow[];
    pendingCount: number;
    searchText: string;
    customerSortLabel: string;
    dateSortLabel: string;
  }>();

  defineEmits<{
    'update:search-text': [value: string];
    sort: [column: 'customer' | 'date'];
    'open-customer': [name: string];
    'open-bike': [bikeId: number];
    'compose-mail': [payload: { customer: string; kind: string }];
  }>();
</script>

<template>
  <div class="page-heading">
    <div>
      <p class="eyebrow">COMMUNICATION</p>
      <h1>連絡・メール管理</h1>
      <p class="sub">イベント・メンテナンス予定に関する顧客連絡を管理します。</p>
    </div>
    <button class="primary" @click="$emit('compose-mail', { customer: '', kind: 'メンテナンス' })">
      ✉ メールを作成
    </button>
  </div>
  <section class="panel">
    <div class="panel-head">
      <div>
        <h2>案内時期が到来した予定</h2>
        <p>開始予定日まで7日以内または期限超過の予定</p>
      </div>
      <input
        class="search"
        :value="searchText"
        placeholder="顧客・自転車・予定日・区分で検索"
        @input="$emit('update:search-text', ($event.target as HTMLInputElement).value)"
      />
      <span class="status warning">{{ pendingCount }}件</span>
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
            <th>予定内容</th>
            <th>
              <button class="sort-header" @click="$emit('sort', 'date')">
                予定日 {{ dateSortLabel }}
              </button>
            </th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in rows" :key="row.id">
            <td>
              <button class="customer-link" @click="$emit('open-customer', row.customer)">
                {{ row.customer }}
              </button>
            </td>
            <td>
              <button class="customer-link" @click="$emit('open-bike', row.bikeId)">
                {{ row.bikeModel }}
              </button>
            </td>
            <td>{{ row.kind }}</td>
            <td>{{ row.title }}</td>
            <td>{{ row.date }}</td>
            <td>
              <button
                class="outline small"
                @click="$emit('compose-mail', { customer: row.customer, kind: row.kind })"
              >
                内容確認
              </button>
            </td>
          </tr>
          <tr v-if="rows.length === 0">
            <td colspan="6" class="empty-state">案内時期が到来した予定はありません。</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
