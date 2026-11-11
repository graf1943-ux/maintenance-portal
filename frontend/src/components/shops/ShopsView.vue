<script setup lang="ts">
  type Shop = { id: number; name: string; address: string; phone: string };
  type Staff = { id: number; shopId: number; name: string; role: string };

  const props = defineProps<{ shops: Shop[]; staff: Staff[]; searchText: string }>();

  defineEmits<{
    'update:search-text': [value: string];
    'add-shop': [];
    'add-staff': [];
  }>();

  function staffNames(shopId: number) {
    return (
      props.staff
        .filter((person) => person.shopId === shopId)
        .map((person) => person.name)
        .join('、') || '未登録'
    );
  }

  function includesSearch(shop: Shop) {
    const keyword = props.searchText.trim().toLocaleLowerCase();
    if (!keyword) return true;
    return [shop.name, shop.address, shop.phone, staffNames(shop.id)]
      .join(' ')
      .toLocaleLowerCase()
      .includes(keyword);
  }
</script>

<template>
  <div class="page-heading">
    <div>
      <p class="eyebrow">MULTI-STORE MANAGEMENT</p>
      <h1>店舗・受付担当者管理</h1>
      <p class="sub">複数店舗と、店舗ごとの受付担当者を管理します。</p>
    </div>
    <div class="heading-actions">
      <button class="outline" @click="$emit('add-staff')">＋ 担当者を登録</button>
      <button class="primary" @click="$emit('add-shop')">＋ 店舗を登録</button>
    </div>
  </div>
  <section class="panel">
    <div class="panel-head">
      <div>
        <h2>登録店舗一覧</h2>
        <p>{{ shops.length }}店舗</p>
      </div>
      <input
        class="search"
        :value="searchText"
        placeholder="店舗名・住所・電話・担当者で検索"
        @input="$emit('update:search-text', ($event.target as HTMLInputElement).value)"
      />
    </div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>店舗名</th>
            <th>住所</th>
            <th>電話番号</th>
            <th>受付担当者</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="shop in shops.filter(includesSearch)" :key="shop.id">
            <td>
              <b>{{ shop.name }}</b>
            </td>
            <td>{{ shop.address }}</td>
            <td>{{ shop.phone }}</td>
            <td>{{ staffNames(shop.id) }}</td>
          </tr>
          <tr v-if="shops.filter(includesSearch).length === 0">
            <td colspan="4" class="empty-state">該当する店舗はありません。</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
