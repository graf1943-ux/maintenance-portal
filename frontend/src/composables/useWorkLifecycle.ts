import { ref, type Ref } from 'vue';
import { apiPost, apiPut } from '../api';
import type {
  EventCheck,
  History,
  Status,
  Work,
  WorkChecklistItem,
  WorkReportPart,
} from '../types';
import { dateIso } from '../services/apiMappers';

type WorkKind = 'maintenance' | 'event' | 'overhaul';
type WorkItem = Work | EventCheck;
type StaffMember = { id: number; shopId: number; name: string; role: string };
type CompletionTarget = { kind: WorkKind; id: number };
type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;

interface WorkLifecycleContext {
  useMockData: boolean;
  staff: Ref<StaffMember[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  histories: Ref<History[]>;
  notice: Ref<string>;
  apiAction: ApiAction;
}

/** 作業開始・完了の状態遷移をまとめます。履歴と契約回数の更新は完了API側で行います。 */
export function useWorkLifecycle(ctx: WorkLifecycleContext) {
  const completionTarget = ref<CompletionTarget | null>(null);
  const completionMemo = ref('');
  const completionDate = ref('');
  const completionWorkMinutes = ref(0);
  const completionChecklist = ref<WorkChecklistItem[]>([]);
  const completionParts = ref<WorkReportPart[]>([]);
  const completionError = ref('');
  const stopTarget = ref<CompletionTarget | null>(null);
  const stopReason = ref('');
  const stopDate = ref('');

  function localDateInput() {
    const date = new Date();
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }

  async function updateStatus(kind: WorkKind, item: WorkItem, status: Status) {
    const list =
      kind === 'overhaul'
        ? ctx.overhauls
        : kind === 'maintenance'
          ? ctx.maintenance
          : ctx.eventChecks;
    const target = list.value.find((entry) => entry.id === item.id);
    if (!target) return;

    if (ctx.useMockData) {
      target.status = status;
      return;
    }

    const staffMember =
      ctx.staff.value.find((x) => x.name === item.receptionist && x.shopId === item.shopId) ??
      ctx.staff.value.find((x) => x.shopId === item.shopId);
    if (!staffMember) {
      ctx.notice.value = '受付担当者を確認してください。';
      return;
    }

    if (kind === 'event' && 'eventDate' in item) {
      await ctx.apiAction(
        () =>
          apiPut(`/events/${item.id}`, {
            id: item.id,
            bikeId: item.bikeId,
            name: item.name,
            eventDate: dateIso(item.eventDate),
            inspectionStartDate: dateIso(item.inspectionDate),
            inspectionEndDate: dateIso(item.endDate) || null,
            status,
            memo: item.memo,
            shopId: item.shopId,
            receptionistId: staffMember.id,
            isAdHoc: item.isAdHoc ?? false,
          }),
        '状態を更新しました。'
      );
      return;
    }

    if ('date' in item) {
      await ctx.apiAction(
        () =>
          apiPut(`/work/${item.id}`, {
            startDate: dateIso(item.date),
            endDate: dateIso(item.endDate) || null,
            status,
            title: item.title,
            detail: item.detail,
            receptionistId: staffMember.id,
            isAdHoc: item.isAdHoc ?? false,
          }),
        '状態を更新しました。'
      );
    }
  }

  async function startWork(kind: WorkKind, item: WorkItem) {
    if (item.status === '完了' || item.status === 'キャンセル' || item.status === '作業中止')
      return;

    const staffMember =
      ctx.staff.value.find((x) => x.name === item.receptionist && x.shopId === item.shopId) ??
      ctx.staff.value.find((x) => x.shopId === item.shopId);
    if (!staffMember) {
      ctx.notice.value = '受付担当者を確認してください。';
      return;
    }

    if (kind === 'event') {
      const event = item as EventCheck;
      await ctx.apiAction(
        () =>
          apiPut(`/events/${event.id}`, {
            id: event.id,
            bikeId: event.bikeId,
            name: event.name,
            eventDate: dateIso(event.eventDate),
            inspectionStartDate: dateIso(event.inspectionDate),
            inspectionEndDate: dateIso(event.endDate),
            status: '作業中',
            memo: event.memo,
            shopId: event.shopId,
            receptionistId: staffMember.id,
            isAdHoc: event.isAdHoc ?? false,
          }),
        '点検を作業中に変更しました。'
      );
      return;
    }

    const work = item as Work;
    await ctx.apiAction(
      () =>
        apiPut(`/work/${work.id}`, {
          startDate: dateIso(work.date),
          endDate: dateIso(work.endDate) || null,
          status: '作業中',
          title: work.title,
          detail: work.detail,
          receptionistId: staffMember.id,
          isAdHoc: work.isAdHoc ?? false,
        }),
      '作業中に変更しました。'
    );
  }

  function startCompletion(kind: WorkKind, item: WorkItem) {
    completionTarget.value = { kind, id: item.id };
    completionMemo.value = '';
    completionWorkMinutes.value = 0;
    completionChecklist.value = [
      { item: 'フレーム・締結部', result: '良好', note: '' },
      { item: 'ブレーキ', result: '良好', note: '' },
      { item: '変速', result: '良好', note: '' },
      { item: 'ホイール・タイヤ', result: '良好', note: '' },
      { item: '駆動系', result: '良好', note: '' },
      { item: '全体確認', result: '良好', note: '' },
    ];
    completionParts.value = [];
    completionError.value = '';
    const suggested =
      item.endDate ||
      (kind === 'event' ? (item as EventCheck).inspectionDate : (item as Work).date);
    completionDate.value = suggested.replace(/\//g, '-');
  }

  async function confirmCompletion() {
    if (!completionTarget.value) return;

    const { kind, id } = completionTarget.value;
    const list: WorkItem[] =
      kind === 'maintenance'
        ? ctx.maintenance.value
        : kind === 'event'
          ? ctx.eventChecks.value
          : ctx.overhauls.value;
    const item = list.find((x) => x.id === id);
    if (!item || item.status === '完了') return;

    completionError.value = '';
    const startDate = kind === 'event' ? (item as EventCheck).inspectionDate : (item as Work).date;
    const endDate = completionDate.value;
    if (!endDate || endDate < startDate.replace(/\//g, '-')) {
      ctx.notice.value = '完了日は開始日以降の日付を指定してください。';
      return;
    }

    if (
      !Number.isInteger(completionWorkMinutes.value) ||
      completionWorkMinutes.value < 1 ||
      completionWorkMinutes.value > 1440
    ) {
      completionError.value = '作業時間は1〜1440分で入力してください。';
      return;
    }

    const checklist = completionChecklist.value.map((entry) => ({
      item: entry.item.trim(),
      result: entry.result,
      note: entry.note.trim(),
    }));
    if (
      checklist.length > 50 ||
      checklist.some((entry) => !entry.item || entry.item.length > 100 || entry.note.length > 500)
    ) {
      completionError.value =
        '点検項目名は必須（100文字以内）、メモは500文字以内で入力してください。';
      return;
    }

    const parts = completionParts.value.map((part) => ({
      name: part.name.trim(),
      quantity: part.quantity,
      unit: part.unit.trim(),
    }));
    if (
      parts.length > 50 ||
      parts.some(
        (part) =>
          !part.name ||
          part.name.length > 100 ||
          !Number.isFinite(part.quantity) ||
          part.quantity <= 0 ||
          part.quantity > 100000 ||
          !part.unit ||
          part.unit.length > 20
      )
    ) {
      completionError.value = '使用部品は名称・数量・単位を確認してください（各50件以内）。';
      return;
    }

    const reportRequest = {
      workMinutes: completionWorkMinutes.value,
      checklist,
      parts,
    };
    const endpoint = kind === 'event' ? `/events/${id}/complete` : `/work/${id}/complete`;
    const ok = await ctx.apiAction(
      () =>
        apiPost(endpoint, {
          completedDate: endDate,
          memo: completionMemo.value,
          report: reportRequest,
        }),
      kind === 'event'
        ? '点検サポートを完了し、点検回数を1回消費しました。'
        : kind === 'maintenance'
          ? 'メンテナンスを完了し、メンテナンス回数を1回消費しました。'
          : 'オーバーホールを完了しました。'
    );

    if (ok) {
      completionTarget.value = null;
      completionMemo.value = '';
      completionError.value = '';
    }
  }

  function startStop(kind: WorkKind, item: WorkItem) {
    if (item.status !== '作業中') return;
    stopTarget.value = { kind, id: item.id };
    stopDate.value = localDateInput();
    stopReason.value = '';
  }

  async function confirmStop() {
    if (!stopTarget.value || !stopDate.value) return;
    const { kind, id } = stopTarget.value;
    const list: WorkItem[] =
      kind === 'maintenance'
        ? ctx.maintenance.value
        : kind === 'event'
          ? ctx.eventChecks.value
          : ctx.overhauls.value;
    const item = list.find((entry) => entry.id === id);
    if (!item || item.status !== '作業中') return;
    const startDate = kind === 'event' ? (item as EventCheck).inspectionDate : (item as Work).date;
    if (stopDate.value < startDate.replace(/\//g, '-')) {
      ctx.notice.value = '中止日は作業開始日以降の日付を指定してください。';
      return;
    }

    if (ctx.useMockData) {
      item.status = '作業中止';
      ctx.histories.value.unshift({
        id: Math.max(0, ...ctx.histories.value.map((entry) => entry.id)) + 1,
        bikeId: item.bikeId,
        date: stopDate.value.replace(/-/g, '/'),
        startDate,
        category: '作業中止',
        title: 'name' in item ? item.name : item.title,
        detail: 'name' in item ? item.memo : item.detail,
        memo: stopReason.value,
        isAdHoc: item.isAdHoc ?? false,
      });
      ctx.notice.value =
        '作業中止を記録しました。契約回数は消費していません。再開時は新規作業として登録してください。';
      stopTarget.value = null;
      return;
    }

    const endpoint = kind === 'event' ? `/events/${id}/stop` : `/work/${id}/stop`;
    const ok = await ctx.apiAction(
      () => apiPost(endpoint, { stoppedDate: stopDate.value, reason: stopReason.value || null }),
      '作業中止を記録しました。契約回数は消費していません。再開時は新規作業として登録してください。'
    );
    if (ok) {
      stopTarget.value = null;
      stopReason.value = '';
    }
  }

  return {
    completionTarget,
    completionMemo,
    completionDate,
    completionWorkMinutes,
    completionChecklist,
    completionParts,
    completionError,
    stopTarget,
    stopReason,
    stopDate,
    updateStatus,
    startWork,
    startCompletion,
    confirmCompletion,
    startStop,
    confirmStop,
  };
}
