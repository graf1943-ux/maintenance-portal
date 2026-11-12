import type { Ref } from 'vue';
import { apiPost, apiPut } from '../api';
import type { EventCheck, Work } from '../types';
import { dateIso } from '../services/apiMappers';

type WorkKind = 'maintenance' | 'event' | 'overhaul';
type WorkItem = Work | EventCheck;
type StaffMember = { id: number; shopId: number; name: string; role: string };
type CompletionTarget = { kind: WorkKind; id: number };
type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;

interface WorkLifecycleContext {
  staff: Ref<StaffMember[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  notice: Ref<string>;
  completionTarget: Ref<CompletionTarget | null>;
  completionMemo: Ref<string>;
  completionDate: Ref<string>;
  apiAction: ApiAction;
}

/** 作業開始・完了の状態遷移をまとめます。履歴と契約回数の更新は完了API側で行います。 */
export function useWorkLifecycle(ctx: WorkLifecycleContext) {
  async function startWork(kind: WorkKind, item: WorkItem) {
    if (item.status === '完了' || item.status === 'キャンセル') return;

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
        }),
      '作業中に変更しました。'
    );
  }

  function startCompletion(kind: WorkKind, item: WorkItem) {
    ctx.completionTarget.value = { kind, id: item.id };
    ctx.completionMemo.value = '';
    const suggested =
      item.endDate ||
      (kind === 'event' ? (item as EventCheck).inspectionDate : (item as Work).date);
    ctx.completionDate.value = suggested.replace(/\//g, '-');
  }

  async function confirmCompletion() {
    if (!ctx.completionTarget.value) return;

    const { kind, id } = ctx.completionTarget.value;
    const list: WorkItem[] =
      kind === 'maintenance'
        ? ctx.maintenance.value
        : kind === 'event'
          ? ctx.eventChecks.value
          : ctx.overhauls.value;
    const item = list.find((x) => x.id === id);
    if (!item || item.status === '完了') return;

    const startDate = kind === 'event' ? (item as EventCheck).inspectionDate : (item as Work).date;
    const endDate = ctx.completionDate.value;
    if (!endDate || endDate < startDate.replace(/\//g, '-')) {
      ctx.notice.value = '完了日は開始日以降の日付を指定してください。';
      return;
    }

    const endpoint = kind === 'event' ? `/events/${id}/complete` : `/work/${id}/complete`;
    const ok = await ctx.apiAction(
      () => apiPost(endpoint, { completedDate: endDate, memo: ctx.completionMemo.value }),
      kind === 'event'
        ? '点検サポートを完了し、点検回数を1回消費しました。'
        : kind === 'maintenance'
          ? 'メンテナンスを完了し、メンテナンス回数を1回消費しました。'
          : 'オーバーホールを完了しました。'
    );

    if (ok) {
      ctx.completionTarget.value = null;
      ctx.completionMemo.value = '';
    }
  }

  return { startWork, startCompletion, confirmCompletion };
}
