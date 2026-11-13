import { ref, type Ref } from 'vue';
import { apiPost } from '../api';
import type { EventCheck, Work, WorkChecklistItem, WorkReportPart } from '../types';

type WorkKind = 'maintenance' | 'event' | 'overhaul';
type WorkItem = Work | EventCheck;
type CompletionTarget = { kind: WorkKind; id: number };
type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;

interface WorkCompletionContext {
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  notice: Ref<string>;
  apiAction: ApiAction;
}

/** 完了登録フォーム、作業報告の検証、完了API呼び出しを管理します。 */
export function useWorkCompletion(ctx: WorkCompletionContext) {
  const completionTarget = ref<CompletionTarget | null>(null);
  const completionMemo = ref('');
  const completionDate = ref('');
  const completionWorkMinutes = ref(0);
  const completionChecklist = ref<WorkChecklistItem[]>([]);
  const completionParts = ref<WorkReportPart[]>([]);
  const completionError = ref('');

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
    const item = list.find((entry) => entry.id === id);
    if (!item || item.status === '完了') return;

    completionError.value = '';
    const startDate = kind === 'event' ? (item as EventCheck).inspectionDate : (item as Work).date;
    const endDate = completionDate.value;
    if (!endDate || endDate < startDate.replace(/\//g, '-')) {
      completionError.value = '完了日は開始日以降の日付を指定してください。';
      ctx.notice.value = completionError.value;
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
      unitPrice: part.unitPrice ?? 0,
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
          !Number.isFinite(part.unitPrice) ||
          !Number.isInteger(part.unitPrice) ||
          (part.unitPrice ?? -1) < 0 ||
          (part.unitPrice ?? 0) > 100000000 ||
          !part.unit ||
          part.unit.length > 20
      )
    ) {
      completionError.value =
        '使用部品は名称・数量・単位を確認してください。単価は0円以上の整数で入力してください（各50件以内）。';
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
    } else {
      completionError.value = ctx.notice.value;
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
    startCompletion,
    confirmCompletion,
  };
}
