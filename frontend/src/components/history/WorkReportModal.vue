<script setup lang="ts">
  import { computed } from 'vue';
  import type { Bike, History } from '../../types';

  type HistoryReport = History & { bike?: Bike };

  const props = defineProps<{ history: HistoryReport }>();

  defineEmits<{ close: [] }>();

  const report = computed(() => props.history.report);
  const generatedAt = new Intl.DateTimeFormat('ja-JP', { dateStyle: 'medium' }).format(new Date());

  function printReport() {
    window.print();
  }
</script>

<template>
  <div class="modal-backdrop work-report-backdrop" @click.self="$emit('close')">
    <article class="modal modal-wide work-report-modal">
      <header class="work-report-header">
        <div>
          <p class="eyebrow">SERVICE REPORT</p>
          <h1>作業報告書</h1>
        </div>
        <button class="close" @click="$emit('close')">×</button>
      </header>

      <p class="work-report-generated">発行日：{{ generatedAt }}</p>
      <section class="work-report-section work-report-summary">
        <div>
          <label>顧客名</label><strong>{{ history.bike?.customer || '未登録' }} 様</strong>
        </div>
        <div>
          <label>自転車</label>
          <strong>{{ history.bike?.maker }} {{ history.bike?.model }}</strong>
        </div>
        <div>
          <label>年式</label><strong>{{ history.bike?.year || '未登録' }}</strong>
        </div>
        <div>
          <label>車体番号</label><strong>{{ history.bike?.serial || '未登録' }}</strong>
        </div>
        <div>
          <label>作業区分</label><strong>{{ history.category }}</strong>
        </div>
        <div>
          <label>作業完了日</label><strong>{{ history.date }}</strong>
        </div>
        <div>
          <label>作業時間</label>
          <strong>{{ report?.workMinutes ?? '未記録' }}{{ report ? '分' : '' }}</strong>
        </div>
        <div>
          <label>店舗</label><strong>{{ report?.shopName || '未記録' }}</strong>
        </div>
        <div>
          <label>受付担当者</label><strong>{{ report?.staffName || '未記録' }}</strong>
        </div>
      </section>

      <section class="work-report-section">
        <h2>{{ history.title }}</h2>
        <p class="work-report-detail">{{ history.detail || '作業内容の詳細はありません。' }}</p>
      </section>

      <section class="work-report-section">
        <h2>作業チェック</h2>
        <div v-if="report?.checklist.length" class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>項目</th>
                <th>結果</th>
                <th>メモ</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(entry, index) in report.checklist" :key="index">
                <td>{{ entry.item }}</td>
                <td>{{ entry.result }}</td>
                <td>{{ entry.note || '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-else class="work-report-empty">チェック項目は記録されていません。</p>
      </section>

      <section class="work-report-section">
        <h2>使用部品</h2>
        <div v-if="report?.parts.length" class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>部品名</th>
                <th>数量</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(part, index) in report.parts" :key="index">
                <td>{{ part.name }}</td>
                <td>{{ part.quantity }} {{ part.unit }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-else class="work-report-empty">使用部品は記録されていません。</p>
      </section>

      <section class="work-report-section">
        <h2>作業メモ</h2>
        <p class="work-report-detail">{{ history.memo || '作業メモはありません。' }}</p>
      </section>

      <footer class="modal-actions work-report-actions">
        <button class="outline" @click="$emit('close')">閉じる</button>
        <button class="primary" @click="printReport">印刷・PDF保存</button>
      </footer>
    </article>
  </div>
</template>
