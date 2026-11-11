<script setup lang="ts">
  import type { Page } from '../../types';

  withDefaults(defineProps<{ page: Page; role: 'staff' | 'customer' }>(), {
    role: 'staff',
  });

  defineEmits<{
    navigate: [page: Page];
    'toggle-role': [];
  }>();
</script>

<template>
  <aside class="sidebar">
    <div class="brand">
      <div class="brand-mark">R</div>
      <div><strong>ROAD BIKE CARE</strong><small>メンテナンス管理</small></div>
    </div>
    <template v-if="role === 'staff'">
      <div class="nav-group">
        <div class="nav-group-title">ホーム</div>
        <button
          class="nav-item"
          :class="{ active: page === 'dashboard' }"
          @click="$emit('navigate', 'dashboard')"
        >
          <span class="nav-icon">▦</span>ダッシュボード
        </button>
      </div>
      <div class="nav-group">
        <div class="nav-group-title">顧客・契約管理</div>
        <button
          v-for="item in [{ id: 'bikes', label: '顧客・自転車・契約管理', icon: '🚴' }]"
          :key="item.id"
          class="nav-item"
          :class="{ active: page === item.id }"
          @click="$emit('navigate', item.id as Page)"
        >
          <span class="nav-icon">{{ item.icon }}</span>{{ item.label }}
        </button>
      </div>
      <div class="nav-group">
        <div class="nav-group-title">顧客向けサービス業務</div>
        <button
          v-for="item in [
            { id: 'overhaul', label: 'オーバーホール', icon: '⚙' },
            { id: 'maintenance', label: 'メンテナンス計画', icon: '◷' },
            { id: 'events', label: '目標イベント・点検', icon: '◇' },
            { id: 'history', label: 'メンテナンス履歴', icon: '▤' },
          ]"
          :key="item.id"
          class="nav-item"
          :class="{ active: page === item.id }"
          @click="$emit('navigate', item.id as Page)"
        >
          <span class="nav-icon">{{ item.icon }}</span>{{ item.label }}
        </button>
      </div>
      <div class="nav-group">
        <div class="nav-group-title">顧客対応</div>
        <button
          class="nav-item"
          :class="{ active: page === 'messages' }"
          @click="$emit('navigate', 'messages')"
        >
          <span class="nav-icon">✉</span>連絡・メール管理
        </button>
      </div>
      <div class="nav-group">
        <div class="nav-group-title">店舗運営・設定</div>
        <button
          v-for="item in [
            { id: 'shops', label: '店舗・担当者管理', icon: '⌂' },
            { id: 'plans', label: 'プラン設定', icon: '▣' },
          ]"
          :key="item.id"
          class="nav-item"
          :class="{ active: page === item.id }"
          @click="$emit('navigate', item.id as Page)"
        >
          <span class="nav-icon">{{ item.icon }}</span>{{ item.label }}
        </button>
      </div>
    </template>
    <template v-else
      ><div class="nav-group">
        <div class="nav-group-title">お客様メニュー</div>
        <button
          class="nav-item"
          :class="{ active: page === 'mypage' }"
          @click="$emit('navigate', 'mypage')"
        >
          <span class="nav-icon">▣</span>マイページ
        </button>
      </div></template
    >
    <div class="sidebar-bottom">
      <div class="side-label">表示モード</div>
      <button class="mode-switch" @click="$emit('toggle-role')">
        <span>{{ role === 'staff' ? '店舗スタッフ' : '顧客マイページ' }}</span><span>切替 ↗</span>
      </button>
      <div class="user">
        <div class="avatar">店</div>
        <div>
          <b>{{ role === 'staff' ? '店舗管理者' : '山田 太郎' }}</b><small>{{
            role === 'staff' ? 'スタッフアカウント' : '顧客アカウント'
          }}</small>
        </div>
      </div>
    </div>
  </aside>
</template>
