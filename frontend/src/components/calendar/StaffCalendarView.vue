<script setup lang="ts">
  import { computed, ref } from 'vue';
  import { localDateInput } from '../../utils/date';
  import type { StaffCalendarEntry } from '../../composables/useStaffCalendar';

  const props = defineProps<{ entries: StaffCalendarEntry[] }>();
  const emit = defineEmits<{ 'open-item': [entry: StaffCalendarEntry] }>();
  const visibleMonth = ref(monthStart(new Date()));
  const weekdayLabels = ['日', '月', '火', '水', '木', '金', '土'];

  const monthLabel = computed(() =>
    new Intl.DateTimeFormat('ja-JP', { year: 'numeric', month: 'long' }).format(visibleMonth.value)
  );
  const days = computed(() => {
    const first = monthStart(visibleMonth.value);
    const start = new Date(first.getFullYear(), first.getMonth(), 1 - first.getDay());
    return Array.from({ length: 42 }, (_, index) => {
      const date = new Date(start.getFullYear(), start.getMonth(), start.getDate() + index);
      const key = dateKey(date);
      return {
        date,
        key,
        isCurrentMonth: date.getMonth() === visibleMonth.value.getMonth(),
        isToday: key === localDateInput(),
        entries: props.entries.filter((entry) => key >= entry.date && key <= entry.endDate),
        targetEvents: props.entries.filter(
          (entry) => entry.kind === 'event' && entry.targetEventDate === key
        ),
      };
    });
  });

  function monthStart(date: Date) {
    return new Date(date.getFullYear(), date.getMonth(), 1);
  }

  function dateKey(date: Date) {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }

  function moveMonth(amount: number) {
    visibleMonth.value = new Date(
      visibleMonth.value.getFullYear(),
      visibleMonth.value.getMonth() + amount,
      1
    );
  }

  function showToday() {
    visibleMonth.value = monthStart(new Date());
  }
</script>

<template>
  <div class="page-heading">
    <div>
      <p class="eyebrow">STAFF SCHEDULE</p>
      <h1>作業カレンダー</h1>
      <p class="sub">店舗のオーバーホール、メンテナンス、点検サポート予定を確認できます。</p>
    </div>
  </div>

  <section class="panel calendar-panel">
    <div class="calendar-toolbar">
      <div class="month-control">
        <button class="secondary" aria-label="前の月" @click="moveMonth(-1)">‹</button>
        <h2>{{ monthLabel }}</h2>
        <button class="secondary" aria-label="次の月" @click="moveMonth(1)">›</button>
      </div>
      <button class="secondary" @click="showToday">今月</button>
    </div>
    <div class="calendar-scroll">
      <div class="calendar-grid">
        <div v-for="weekday in weekdayLabels" :key="weekday" class="weekday">{{ weekday }}</div>
        <section
          v-for="day in days"
          :key="day.key"
          class="calendar-day"
          :class="{ 'outside-month': !day.isCurrentMonth, 'today-cell': day.isToday }"
        >
          <div class="day-number" :class="{ today: day.isToday }">{{ day.date.getDate() }}</div>
          <button
            v-for="entry in day.entries"
            :key="entry.id"
            class="calendar-entry"
            :class="[`entry-${entry.kind}`, { 'ad-hoc': entry.isAdHoc }]"
            :title="`${entry.kindLabel} / ${entry.customer} / ${entry.bikeLabel} / ${entry.title} / ${entry.status}`"
            @click="emit('open-item', entry)"
          >
            <span class="entry-kind">{{ entry.isAdHoc ? '単発・' : '' }}{{ entry.kindLabel }}</span>
            <span class="entry-title">{{ entry.title }}</span>
            <span class="entry-customer">{{ entry.customer }} {{ entry.bikeLabel }}</span>
            <span class="entry-status">{{ entry.status }}</span>
          </button>
          <small v-for="entry in day.targetEvents" :key="`${entry.id}-goal`" class="target-event">
            目標イベント: {{ entry.title }}
          </small>
        </section>
      </div>
    </div>
    <p v-if="entries.length === 0" class="empty-state">登録済みの作業予定はありません。</p>
  </section>
</template>

<style scoped>
  .calendar-panel {
    padding: 20px;
  }
  .calendar-toolbar,
  .month-control {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .calendar-toolbar {
    justify-content: space-between;
    margin-bottom: 16px;
  }
  .month-control h2 {
    min-width: 150px;
    margin: 0;
    text-align: center;
  }
  .calendar-scroll {
    overflow-x: auto;
  }
  .calendar-grid {
    display: grid;
    grid-template-columns: repeat(7, minmax(125px, 1fr));
    min-width: 920px;
    border-top: 1px solid #dbe3ed;
    border-left: 1px solid #dbe3ed;
  }
  .weekday {
    padding: 9px;
    text-align: center;
    font-weight: 700;
    background: #f3f6fa;
    border-right: 1px solid #dbe3ed;
    border-bottom: 1px solid #dbe3ed;
  }
  .calendar-day {
    min-height: 150px;
    padding: 7px;
    border-right: 1px solid #dbe3ed;
    border-bottom: 1px solid #dbe3ed;
  }
  .outside-month {
    background: #f3f6fa;
    color: #69778a;
  }
  .today-cell {
    background: color-mix(in srgb, #2563eb 5%, white);
  }
  .day-number {
    margin-bottom: 6px;
    font-size: 13px;
    font-weight: 700;
  }
  .day-number.today {
    display: inline-grid;
    width: 24px;
    height: 24px;
    place-items: center;
    color: white;
    background: #2563eb;
    border-radius: 50%;
  }
  .calendar-entry {
    display: flex;
    width: 100%;
    flex-direction: column;
    gap: 2px;
    margin: 0 0 5px;
    padding: 6px;
    overflow: hidden;
    text-align: left;
    color: #243247;
    background: #f1f5f9;
    border: 0;
    border-left: 3px solid #64748b;
    border-radius: 4px;
    cursor: pointer;
  }
  .entry-overhaul {
    background: #fff7ed;
    border-left-color: #ea580c;
  }
  .entry-maintenance {
    background: #eff6ff;
    border-left-color: #2563eb;
  }
  .entry-event {
    background: #f5f3ff;
    border-left-color: #7c3aed;
  }
  .calendar-entry.ad-hoc {
    outline: 1px dashed currentColor;
  }
  .entry-kind,
  .entry-status {
    font-size: 10px;
  }
  .entry-title {
    overflow: hidden;
    font-size: 12px;
    font-weight: 700;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .entry-customer {
    overflow: hidden;
    font-size: 10px;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .target-event {
    display: block;
    margin: 2px 0 5px;
    color: #69778a;
    font-size: 10px;
  }
  @media (max-width: 700px) {
    .calendar-panel {
      padding: 12px;
    }
  }
</style>
