<script setup lang="ts">
  import { computed, ref } from 'vue';
  import type { Bike, History } from '../../types';

  type HistoryReport = History & { bike?: Bike };

  const props = defineProps<{ history: HistoryReport }>();

  defineEmits<{ close: [] }>();

  const report = computed(() => props.history.report);
  const showQuote = ref(false);
  const hasQuotePrices = computed(() => {
    const parts = report.value?.parts;
    return Boolean(parts?.length && parts.every((part) => part.unitPrice != null));
  });
  const quoteTotal = computed(() =>
    (report.value?.parts ?? []).reduce(
      (total, part) => total + Math.round(part.quantity * (part.unitPrice ?? 0)),
      0
    )
  );
  const quoteTax = computed(() => Math.round(quoteTotal.value * 0.1));
  const quoteTotalWithTax = computed(() => quoteTotal.value + quoteTax.value);
  const yen = new Intl.NumberFormat('ja-JP', { maximumFractionDigits: 0 });
  const generatedAt = new Intl.DateTimeFormat('ja-JP', { dateStyle: 'medium' }).format(new Date());

  function printReport() {
    window.print();
  }
</script>

<template>
  <div class="modal-backdrop work-report-backdrop" @click.self="$emit('close')">
    <article v-if="showQuote" class="modal modal-wide work-report-modal">
      <header class="work-report-header">
        <div>
          <p class="eyebrow">PARTS ESTIMATE</p>
          <h1>部品代お見積書</h1>
        </div>
        <button class="close" @click="$emit('close')">×</button>
      </header>
      <p class="work-report-generated">発行日：{{ generatedAt }}</p>
      <section class="work-report-section work-report-summary">
        <div>
          <label>お客様</label><strong>{{ history.bike?.customer || '未登録' }} 様</strong>
        </div>
        <div>
          <label>自転車</label><strong>{{ history.bike?.maker }} {{ history.bike?.model }}</strong>
        </div>
        <div>
          <label>車体番号</label><strong>{{ history.bike?.serial || '未登録' }}</strong>
        </div>
        <div>
          <label>作業区分</label><strong>{{ history.category }}</strong>
        </div>
        <div>
          <label>対象作業</label><strong>{{ history.title }}</strong>
        </div>
        <div>
          <label>作業日</label><strong>{{ history.date }}</strong>
        </div>
      </section>
      <section class="work-report-section">
        <h2>お見積り内容（部品代）</h2>
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>部品名</th>
                <th>数量</th>
                <th>単価</th>
                <th>金額</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(part, index) in report?.parts ?? []" :key="index">
                <td>{{ part.name }}</td>
                <td>{{ part.quantity }} {{ part.unit }}</td>
                <td>¥{{ yen.format(part.unitPrice ?? 0) }}</td>
                <td>¥{{ yen.format(Math.round(part.quantity * (part.unitPrice ?? 0))) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <dl class="work-report-quote-totals">
          <div>
            <dt>部品代小計（税別）</dt>
            <dd>¥{{ yen.format(quoteTotal) }}</dd>
          </div>
          <div>
            <dt>消費税（10%）</dt>
            <dd>¥{{ yen.format(quoteTax) }}</dd>
          </div>
          <div class="grand-total">
            <dt>お見積り合計（税込）</dt>
            <dd>¥{{ yen.format(quoteTotalWithTax) }}</dd>
          </div>
        </dl>
        <p class="work-report-detail">
          本見積りは部品代のみです。作業工賃はご契約プランに含まれています。
        </p>
      </section>
      <footer class="modal-actions work-report-actions">
        <button class="outline" @click="showQuote = false">作業報告書に戻る</button>
        <button class="primary" @click="printReport">見積書を印刷・PDF保存</button>
      </footer>
    </article>
    <article v-else class="modal modal-wide work-report-modal">
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
        <button v-if="hasQuotePrices" class="outline" @click="showQuote = true">
          部品代見積書
        </button>
        <button class="primary" @click="printReport">印刷・PDF保存</button>
      </footer>
    </article>
  </div>
</template>
