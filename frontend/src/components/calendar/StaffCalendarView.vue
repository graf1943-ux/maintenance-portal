<script setup lang="ts">
  import { computed, ref } from 'vue';
  import type { ApiShop, ApiStaff } from '../../services/apiMappers';
  import { localDateInput } from '../../utils/date';
  import type { StaffCalendarEntry } from '../../composables/useStaffCalendar';

  const props = defineProps<{
    entries: StaffCalendarEntry[];
    shops: ApiShop[];
    staff: ApiStaff[];
    selectedShopId: number | null;
    selectedStaffId: number | null;
  }>();
  const emit = defineEmits<{ 'open-item': [entry: StaffCalendarEntry] }>();
  const visibleMonth = ref(monthStart(new Date()));
  const expandedDayKey = ref<string | null>(null);
  const weekdayLabels = ['日', '月', '火', '水', '木', '金', '土'];
  const visibleMonthRange = computed(() => {
    const first = monthStart(visibleMonth.value);
    const last = new Date(first.getFullYear(), first.getMonth() + 1, 0);
    return { firstKey: dateKey(first), lastKey: dateKey(last) };
  });
  const visibleMonthEntries = computed(() => {
    const { firstKey, lastKey } = visibleMonthRange.value;
    return props.entries.filter((entry) => entry.date <= lastKey && entry.endDate >= firstKey);
  });
  const activeMonthEntries = computed(() =>
    visibleMonthEntries.value.filter(
      (entry) =>
        entry.status !== '完了' && entry.status !== 'キャンセル' && entry.status !== '作業中止'
    )
  );
  const workloadConflicts = computed(() => {
    const { firstKey, lastKey } = visibleMonthRange.value;
    const conflicts: {
      first: StaffCalendarEntry;
      second: StaffCalendarEntry;
      date: string;
      startTime: string;
      endTime: string;
    }[] = [];

    for (let firstIndex = 0; firstIndex < activeMonthEntries.value.length; firstIndex += 1) {
      const first = activeMonthEntries.value[firstIndex];
      const firstStaffKey = staffKey(first);
      if (!firstStaffKey) continue;

      for (
        let secondIndex = firstIndex + 1;
        secondIndex < activeMonthEntries.value.length;
        secondIndex += 1
      ) {
        const second = activeMonthEntries.value[secondIndex];
        if (staffKey(second) !== firstStaffKey) continue;

        for (const firstSlot of first.scheduleSlots) {
          if (firstSlot.date < firstKey || firstSlot.date > lastKey) continue;
          for (const secondSlot of second.scheduleSlots) {
            if (firstSlot.date !== secondSlot.date) continue;
            if (
              firstSlot.startTime < secondSlot.endTime &&
              secondSlot.startTime < firstSlot.endTime
            ) {
              conflicts.push({
                first,
                second,
                date: firstSlot.date,
                startTime:
                  firstSlot.startTime > secondSlot.startTime
                    ? firstSlot.startTime
                    : secondSlot.startTime,
                endTime:
                  firstSlot.endTime < secondSlot.endTime ? firstSlot.endTime : secondSlot.endTime,
              });
            }
          }
        }
      }
    }

    return conflicts;
  });
  const overlappingEntryIds = computed(
    () =>
      new Set(
        workloadConflicts.value.flatMap((conflict) => [conflict.first.id, conflict.second.id])
      )
  );
  const workloadRows = computed(() =>
    props.staff
      .filter(
        (member) =>
          (props.selectedShopId === null || member.shopId === props.selectedShopId) &&
          (props.selectedStaffId === null || member.id === props.selectedStaffId)
      )
      .map((member) => {
        const memberEntries = activeMonthEntries.value.filter((entry) =>
          isAssignedToStaff(entry, member)
        );
        return {
          ...member,
          shopName: props.shops.find((shop) => shop.id === member.shopId)?.name ?? '',
          activeCount: memberEntries.length,
          unconfirmedCount: memberEntries.filter((entry) => entry.scheduleSlots.length === 0)
            .length,
          reservedMinutes: memberEntries.reduce(
            (total, entry) =>
              total +
              entry.scheduleSlots
                .filter(
                  (slot) =>
                    slot.date >= visibleMonthRange.value.firstKey &&
                    slot.date <= visibleMonthRange.value.lastKey
                )
                .reduce((slotTotal, slot) => slotTotal + slotDurationMinutes(slot), 0),
            0
          ),
          conflictCount: workloadConflicts.value.filter(
            (conflict) => staffKey(conflict.first) === `staff:${member.id}`
          ).length,
        };
      })
      .sort((a, b) => a.name.localeCompare(b.name))
  );
  const unconfirmedEntries = computed(() =>
    activeMonthEntries.value.filter((entry) => entry.scheduleSlots.length === 0)
  );
  const visibleUnconfirmedEntries = computed(() => unconfirmedEntries.value.slice(0, 4));
  const monthCounts = computed(() => ({
    overhaul: visibleMonthEntries.value.filter((entry) => entry.kind === 'overhaul').length,
    maintenance: visibleMonthEntries.value.filter((entry) => entry.kind === 'maintenance').length,
    event: visibleMonthEntries.value.filter((entry) => entry.kind === 'event').length,
  }));

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
        isSunday: date.getDay() === 0,
        isSaturday: date.getDay() === 6,
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

  function staffKey(entry: StaffCalendarEntry) {
    if (entry.assignedStaffId !== undefined) return `staff:${entry.assignedStaffId}`;
    return entry.receptionist ? `shop:${entry.shopId}:${entry.receptionist}` : null;
  }

  function isAssignedToStaff(entry: StaffCalendarEntry, member: ApiStaff) {
    return entry.assignedStaffId !== undefined
      ? entry.assignedStaffId === member.id
      : entry.shopId === member.shopId && entry.receptionist === member.name;
  }

  function slotDurationMinutes(slot: { startTime: string; endTime: string }) {
    const [startHour, startMinute] = slot.startTime.split(':').map(Number);
    const [endHour, endMinute] = slot.endTime.split(':').map(Number);
    return endHour * 60 + endMinute - (startHour * 60 + startMinute);
  }

  function formatMinutes(minutes: number) {
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    if (hours === 0) return `${remainingMinutes}分`;
    return remainingMinutes === 0 ? `${hours}時間` : `${hours}時間${remainingMinutes}分`;
  }

  function scheduleLabel(entry: StaffCalendarEntry, dayKey: string) {
    if (entry.scheduleSlots.length === 0) return '時間未設定';
    const slots = entry.scheduleSlots.filter((slot) => slot.date === dayKey);
    return slots.length
      ? slots.map((slot) => `${slot.startTime}〜${slot.endTime}`).join(' / ')
      : '時間枠なし';
  }

  function moveMonth(amount: number) {
    visibleMonth.value = new Date(
      visibleMonth.value.getFullYear(),
      visibleMonth.value.getMonth() + amount,
      1
    );
    expandedDayKey.value = null;
  }

  function showToday() {
    visibleMonth.value = monthStart(new Date());
    expandedDayKey.value = null;
  }

  function toggleDay(dayKey: string) {
    expandedDayKey.value = expandedDayKey.value === dayKey ? null : dayKey;
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
        <button class="calendar-nav" aria-label="前の月" @click="moveMonth(-1)">‹</button>
        <h2 aria-live="polite">{{ monthLabel }}</h2>
        <button class="calendar-nav" aria-label="次の月" @click="moveMonth(1)">›</button>
      </div>
      <button class="secondary today-button" @click="showToday">今月に戻る</button>
    </div>
    <div class="calendar-overview">
      <strong class="month-total">{{ visibleMonthEntries.length }}<span>件の予定</span></strong>
      <div class="calendar-legend" aria-label="予定の区分">
        <span class="legend-item"
          ><i class="legend-dot overhaul"></i>オーバーホール <b>{{ monthCounts.overhaul }}</b></span
        >
        <span class="legend-item"
          ><i class="legend-dot maintenance"></i>メンテナンス
          <b>{{ monthCounts.maintenance }}</b></span
        >
        <span class="legend-item"
          ><i class="legend-dot event"></i>点検サポート <b>{{ monthCounts.event }}</b></span
        >
      </div>
    </div>
    <section class="workload-panel" aria-label="担当者別の作業負荷">
      <div class="workload-heading">
        <div>
          <h3>担当者別の予定負荷</h3>
          <p>{{ monthLabel }}の未完了予定数と登録済み時間枠です。</p>
        </div>
        <span
          class="workload-conflict-total"
          :class="{ 'has-conflicts': workloadConflicts.length }"
        >
          時間重複 {{ workloadConflicts.length }}件
        </span>
      </div>
      <div v-if="workloadRows.length" class="workload-list">
        <div v-for="row in workloadRows" :key="row.id" class="workload-row">
          <div class="workload-person">
            <strong>{{ row.name }}</strong>
            <small>{{ row.shopName }}</small>
          </div>
          <span class="workload-count"
            >未完了 <b>{{ row.activeCount }}</b> 件</span
          >
          <span class="workload-time-count"
            >時間未設定 <b>{{ row.unconfirmedCount }}</b> 件</span
          >
          <span class="workload-time-count"
            >予定枠 <b>{{ formatMinutes(row.reservedMinutes) }}</b></span
          >
          <span class="workload-conflict-count" :class="{ 'has-conflicts': row.conflictCount }">
            時間重複 <b>{{ row.conflictCount }}</b> 件
          </span>
        </div>
      </div>
      <p v-else class="workload-empty">選択した条件に該当する担当者はいません。</p>
      <ul v-if="workloadConflicts.length" class="workload-conflicts">
        <li
          v-for="conflict in workloadConflicts"
          :key="`${conflict.first.id}-${conflict.second.id}-${conflict.date}-${conflict.startTime}`"
        >
          <div class="conflict-summary">
            <strong>{{ conflict.first.receptionist }}</strong>
            <span>{{ conflict.date }} {{ conflict.startTime }}〜{{ conflict.endTime }}</span>
          </div>
          <div class="conflict-works">
            <button type="button" @click="emit('open-item', conflict.first)">
              {{ conflict.first.kindLabel }}：{{ conflict.first.title }}
            </button>
            <span aria-hidden="true">↔</span>
            <button type="button" @click="emit('open-item', conflict.second)">
              {{ conflict.second.kindLabel }}：{{ conflict.second.title }}
            </button>
          </div>
        </li>
      </ul>
      <p v-else class="no-workload-conflicts">現在、同じ担当者の時間枠重複はありません。</p>
      <div v-if="unconfirmedEntries.length" class="time-unconfirmed">
        <strong>時間要確認 {{ unconfirmedEntries.length }}件</strong>
        <div>
          <button
            v-for="entry in visibleUnconfirmedEntries"
            :key="entry.id"
            type="button"
            @click="emit('open-item', entry)"
          >
            {{ entry.kindLabel }}：{{ entry.title }}
          </button>
          <span v-if="unconfirmedEntries.length > visibleUnconfirmedEntries.length">
            ほか{{ unconfirmedEntries.length - visibleUnconfirmedEntries.length }}件
          </span>
        </div>
      </div>
    </section>
    <p class="calendar-scroll-hint">
      複数日にまたがる作業は矢印でつながります。予定を選択すると詳細を開けます。
    </p>
    <div class="calendar-scroll">
      <div class="calendar-grid">
        <div
          v-for="(weekday, index) in weekdayLabels"
          :key="weekday"
          class="weekday"
          :class="{ 'sunday-label': index === 0, 'saturday-label': index === 6 }"
        >
          {{ weekday }}
        </div>
        <section
          v-for="day in days"
          :key="day.key"
          class="calendar-day"
          :class="{
            'outside-month': !day.isCurrentMonth,
            'today-cell': day.isToday,
            'sunday-cell': day.isSunday,
            'saturday-cell': day.isSaturday,
          }"
        >
          <div class="day-number-row">
            <span class="day-number" :class="{ today: day.isToday }">{{ day.date.getDate() }}</span>
            <span v-if="day.isToday" class="today-label">今日</span>
          </div>
          <button
            v-for="entry in expandedDayKey === day.key ? day.entries : day.entries.slice(0, 2)"
            :key="entry.id"
            type="button"
            class="calendar-entry"
            :class="[
              `entry-${entry.kind}`,
              {
                'ad-hoc': entry.isAdHoc,
                'entry-overlap': overlappingEntryIds.has(entry.id),
                'entry-multiday': entry.date !== entry.endDate,
                'entry-range-start': day.key === entry.date,
                'entry-range-middle': day.key > entry.date && day.key < entry.endDate,
                'entry-range-end': day.key === entry.endDate,
              },
            ]"
            :title="`${entry.kindLabel} / ${entry.customer} / ${entry.bikeLabel} / ${entry.title} / ${entry.status}`"
            @click="emit('open-item', entry)"
          >
            <span class="entry-heading">
              <span class="entry-kind"
                >{{ entry.isAdHoc ? '単発・' : '' }}{{ entry.kindLabel }}</span
              >
              <span class="entry-status">{{ entry.status }}</span>
              <span
                v-if="overlappingEntryIds.has(entry.id)"
                class="entry-conflict-icon"
                title="同じ担当者の時間枠が重複"
                aria-label="同じ担当者の時間枠が重複"
              >
                ⚠
              </span>
            </span>
            <span v-if="entry.date !== entry.endDate" class="entry-range-cue">
              {{
                day.key === entry.date
                  ? '開始 →'
                  : day.key === entry.endDate
                    ? '← 最終日'
                    : '← 継続中 →'
              }}
            </span>
            <span class="entry-title">{{ entry.title }}</span>
            <span class="entry-schedule-time">{{ scheduleLabel(entry, day.key) }}</span>
            <span v-if="entry.estimatedWorkMinutes" class="entry-estimate">
              見込み {{ formatMinutes(entry.estimatedWorkMinutes) }}
            </span>
            <span v-if="entry.customer || entry.bikeLabel" class="entry-customer">
              {{ entry.customer }}<span v-if="entry.customer && entry.bikeLabel"> · </span
              >{{ entry.bikeLabel }}
            </span>
          </button>
          <button
            v-if="day.entries.length > 2"
            type="button"
            class="more-entries"
            :aria-expanded="expandedDayKey === day.key"
            @click="toggleDay(day.key)"
          >
            {{ expandedDayKey === day.key ? '閉じる' : `他 ${day.entries.length - 2} 件を表示` }}
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
    padding: 22px;
  }
  .calendar-toolbar,
  .month-control {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .calendar-toolbar {
    justify-content: space-between;
    margin-bottom: 14px;
  }
  .month-control h2 {
    min-width: 180px;
    margin: 0;
    color: #1e2d41;
    font-size: 19px;
    text-align: center;
  }
  .calendar-nav {
    display: grid;
    width: 36px;
    height: 36px;
    place-items: center;
    color: #49627e;
    background: #fff;
    border: 1px solid #dbe3ed;
    border-radius: 8px;
    font: inherit;
    font-size: 23px;
    line-height: 1;
    cursor: pointer;
  }
  .calendar-nav:hover,
  .calendar-nav:focus-visible {
    color: #245c9e;
    background: #f3f7fc;
    border-color: #9fb8d5;
  }
  .today-button {
    min-height: 36px;
  }
  .calendar-overview {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 12px 24px;
    padding: 12px 14px;
    background: #f7f9fc;
    border: 1px solid #e8edf3;
    border-radius: 8px;
  }
  .month-total {
    color: #263a54;
    font-size: 16px;
    white-space: nowrap;
  }
  .month-total span {
    margin-left: 5px;
    color: #758398;
    font-size: 11px;
    font-weight: 500;
  }
  .calendar-legend {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 8px 18px;
  }
  .legend-item {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    color: #596a80;
    font-size: 11px;
    white-space: nowrap;
  }
  .legend-item b {
    color: #31455e;
    font-size: 10px;
  }
  .legend-dot {
    width: 9px;
    height: 9px;
    border-radius: 50%;
  }
  .legend-dot.overhaul {
    background: #ea580c;
  }
  .legend-dot.maintenance {
    background: #2563eb;
  }
  .legend-dot.event {
    background: #7c3aed;
  }
  .workload-panel {
    margin-top: 12px;
    padding: 14px;
    background: #fbfcfe;
    border: 1px solid #e8edf3;
    border-radius: 8px;
  }
  .workload-heading {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
    margin-bottom: 10px;
  }
  .workload-heading h3 {
    margin: 0;
    color: #293b52;
    font-size: 12px;
  }
  .workload-heading p {
    margin: 4px 0 0;
    color: #8190a3;
    font-size: 10px;
  }
  .workload-conflict-total,
  .workload-conflict-count {
    color: #627187;
    font-size: 10px;
    white-space: nowrap;
  }
  .workload-conflict-total.has-conflicts,
  .workload-conflict-count.has-conflicts {
    color: #b45309;
    font-weight: 700;
  }
  .workload-list {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 7px;
  }
  .workload-row {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px 12px;
    padding: 8px 10px;
    background: #fff;
    border: 1px solid #edf0f4;
    border-radius: 6px;
  }
  .workload-person {
    display: flex;
    min-width: 0;
    flex-direction: column;
    gap: 2px;
  }
  .workload-person strong {
    overflow: hidden;
    color: #31455e;
    font-size: 11px;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .workload-person small {
    overflow: hidden;
    color: #8996a7;
    font-size: 9px;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .workload-count,
  .workload-time-count,
  .workload-conflict-count {
    color: #627187;
    font-size: 10px;
    white-space: nowrap;
  }
  .workload-count b,
  .workload-time-count b,
  .workload-conflict-count b {
    color: #31455e;
    font-size: 13px;
  }
  .workload-conflict-count.has-conflicts b {
    color: #b45309;
  }
  .workload-empty,
  .no-workload-conflicts {
    margin: 8px 0 0;
    color: #8190a3;
    font-size: 10px;
  }
  .workload-conflicts {
    margin: 12px 0 0;
    padding: 0 12px;
    background: #fff9f0;
    border: 1px solid #f4dfbf;
    border-radius: 6px;
    list-style: none;
  }
  .workload-conflicts li {
    padding: 9px 0;
  }
  .workload-conflicts li + li {
    border-top: 1px solid #f2e7d6;
  }
  .conflict-summary {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 5px 12px;
    margin-bottom: 5px;
    font-size: 10px;
  }
  .conflict-summary strong {
    color: #9a5a16;
  }
  .conflict-summary span {
    color: #7f6a4d;
  }
  .conflict-works {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 4px 8px;
    color: #9a5a16;
    font-size: 10px;
  }
  .conflict-works button {
    max-width: 100%;
    padding: 2px 0;
    overflow: hidden;
    color: #356fae;
    background: transparent;
    border: 0;
    font: inherit;
    text-align: left;
    text-overflow: ellipsis;
    white-space: nowrap;
    cursor: pointer;
  }
  .conflict-works button:hover,
  .conflict-works button:focus-visible {
    color: #245c9e;
    text-decoration: underline;
  }
  .calendar-scroll-hint {
    margin: 12px 0 8px;
    color: #8190a3;
    font-size: 10px;
  }
  .calendar-scroll {
    overflow-x: auto;
    border-radius: 4px;
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
    color: #5f6e81;
    background: #f3f6fa;
    border-right: 1px solid #dbe3ed;
    border-bottom: 1px solid #dbe3ed;
  }
  .sunday-label {
    color: #bd4d4d;
  }
  .saturday-label {
    color: #3976b6;
  }
  .calendar-day {
    min-height: 152px;
    padding: 8px;
    background: #fff;
    border-right: 1px solid #dbe3ed;
    border-bottom: 1px solid #dbe3ed;
  }
  .outside-month {
    background: #f8fafc;
    color: #8290a2;
  }
  .today-cell {
    background: #f2f7ff;
  }
  .sunday-cell:not(.outside-month) .day-number:not(.today),
  .sunday-cell.outside-month .day-number:not(.today) {
    color: #bd4d4d;
  }
  .saturday-cell:not(.outside-month) .day-number:not(.today),
  .saturday-cell.outside-month .day-number:not(.today) {
    color: #3976b6;
  }
  .day-number-row {
    display: flex;
    min-height: 24px;
    align-items: center;
    gap: 6px;
    margin-bottom: 6px;
  }
  .day-number {
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
  .today-label {
    color: #2563eb;
    font-size: 9px;
    font-weight: 700;
  }
  .calendar-entry {
    display: flex;
    width: 100%;
    flex-direction: column;
    gap: 3px;
    margin: 0 0 5px;
    padding: 7px;
    overflow: hidden;
    text-align: left;
    color: #243247;
    background: #f1f5f9;
    border: 0;
    border-left: 3px solid #64748b;
    border-radius: 4px;
    cursor: pointer;
  }
  .entry-multiday {
    position: relative;
    z-index: 1;
    border-radius: 0;
  }
  .entry-range-start {
    margin-right: -8px;
    border-top-left-radius: 4px;
    border-bottom-left-radius: 4px;
  }
  .entry-range-middle {
    margin-right: -8px;
    margin-left: -8px;
    border-left-color: transparent;
  }
  .entry-range-end {
    margin-left: -8px;
    border-left-color: transparent;
    border-top-right-radius: 4px;
    border-bottom-right-radius: 4px;
  }
  .entry-range-cue {
    color: #53647a;
    font-size: 9px;
    font-weight: 700;
    line-height: 1.2;
  }
  .calendar-entry:hover,
  .calendar-entry:focus-visible {
    filter: brightness(0.97);
    box-shadow: 0 0 0 2px #527eac44;
    outline: none;
  }
  .time-unconfirmed {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 6px 12px;
    margin-top: 10px;
    padding: 8px 10px;
    color: #8a5a16;
    background: #fffaf0;
    border: 1px solid #f1e2c5;
    border-radius: 6px;
    font-size: 10px;
  }
  .time-unconfirmed > div {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 4px 10px;
  }
  .time-unconfirmed button {
    padding: 0;
    color: #356fae;
    background: transparent;
    border: 0;
    font: inherit;
    cursor: pointer;
  }
  .entry-schedule-time {
    color: #415875;
    font-size: 10px;
    font-weight: 600;
  }
  .entry-estimate {
    color: #718198;
    font-size: 9px;
  }
  .calendar-entry.entry-overlap {
    box-shadow: inset 0 0 0 1px #d97706;
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
  .entry-heading {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 4px;
  }
  .entry-conflict-icon {
    flex: none;
    color: #b45309;
    font-size: 10px;
    font-weight: 700;
  }
  .entry-kind,
  .entry-status {
    font-size: 10px;
  }
  .entry-kind {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .entry-status {
    flex: none;
    padding: 1px 5px;
    color: #53647a;
    background: #ffffffa8;
    border-radius: 10px;
    white-space: nowrap;
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
  .more-entries {
    margin: 0 0 5px;
    padding: 3px 4px;
    color: #356fae;
    background: transparent;
    border: 0;
    border-radius: 4px;
    font: inherit;
    font-size: 10px;
    font-weight: 600;
    cursor: pointer;
  }
  .more-entries:hover,
  .more-entries:focus-visible {
    color: #245c9e;
    background: #eaf2fb;
    outline: none;
  }
  @media (max-width: 700px) {
    .calendar-panel {
      padding: 14px 12px;
    }
    .month-control {
      gap: 7px;
    }
    .month-control h2 {
      min-width: 132px;
      font-size: 16px;
    }
    .calendar-nav {
      width: 32px;
      height: 32px;
    }
    .today-button {
      min-height: 32px;
      padding: 7px 9px;
      font-size: 10px;
    }
    .calendar-overview {
      align-items: flex-start;
      flex-direction: column;
      gap: 9px;
      padding: 10px;
    }
    .calendar-legend {
      gap: 7px 12px;
    }
    .calendar-grid {
      grid-template-columns: repeat(7, minmax(112px, 1fr));
      min-width: 820px;
    }
    .calendar-day {
      min-height: 138px;
      padding: 6px;
    }
  }
</style>
