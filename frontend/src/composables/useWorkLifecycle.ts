import type { Ref } from 'vue';
import { apiPut } from '../api';
import type { EventCheck, Status, Work } from '../types';
import { dateIso } from '../services/apiMappers';
import { findReceptionist } from '../utils/staff';

type WorkKind = 'maintenance' | 'event' | 'overhaul';
type WorkItem = Work | EventCheck;
type StaffMember = { id: number; shopId: number; name: string; role: string };
type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;

interface WorkLifecycleContext {
  useMockData: boolean;
  staff: Ref<StaffMember[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  notice: Ref<string>;
  apiAction: ApiAction;
}

/** 作業開始・完了の状態遷移をまとめます。履歴と契約回数の更新は完了API側で行います。 */
export function useWorkLifecycle(ctx: WorkLifecycleContext) {
  function receptionistFor(item: WorkItem) {
    const receptionist = findReceptionist(ctx.staff.value, item.shopId, item.receptionist);
    if (!receptionist) ctx.notice.value = '受付担当者を確認してください。';
    return receptionist;
  }

  function workUpdatePayload(work: Work, status: Status, receptionistId: number) {
    return {
      startDate: dateIso(work.date),
      endDate: dateIso(work.endDate) || null,
      status,
      title: work.title,
      detail: work.detail,
      receptionistId,
      isAdHoc: work.isAdHoc ?? false,
    };
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

    const staffMember = receptionistFor(item);
    if (!staffMember) return;

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
        () => apiPut(`/work/${item.id}`, workUpdatePayload(item, status, staffMember.id)),
        '状態を更新しました。'
      );
    }
  }

  async function startWork(kind: WorkKind, item: WorkItem) {
    if (item.status === '完了' || item.status === 'キャンセル' || item.status === '作業中止')
      return;

    const staffMember = receptionistFor(item);
    if (!staffMember) return;

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
      () => apiPut(`/work/${work.id}`, workUpdatePayload(work, '作業中', staffMember.id)),
      '作業中に変更しました。'
    );
  }

  return {
    updateStatus,
    startWork,
  };
}
