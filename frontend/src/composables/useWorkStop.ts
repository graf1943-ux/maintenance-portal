import { ref, type Ref } from 'vue';
import { apiPost } from '../api';
import type { EventCheck, History, Work } from '../types';
import { localDateInput } from '../utils/date';
import { nextId } from '../utils/id';

type WorkKind = 'maintenance' | 'event' | 'overhaul';
type WorkItem = Work | EventCheck;
type WorkTarget = { kind: WorkKind; id: number };
type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;

interface WorkStopContext {
  useMockData: boolean;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  histories: Ref<History[]>;
  notice: Ref<string>;
  apiAction: ApiAction;
}

/** 作業中止の入力状態、検証、API・モック更新を管理します。 */
export function useWorkStop(ctx: WorkStopContext) {
  const stopTarget = ref<WorkTarget | null>(null);
  const stopReason = ref('');
  const stopDate = ref('');
  const stopError = ref('');

  function startStop(kind: WorkKind, item: WorkItem) {
    if (item.status !== '作業中') return;
    stopTarget.value = { kind, id: item.id };
    stopDate.value = localDateInput();
    stopReason.value = '';
    stopError.value = '';
  }

  async function confirmStop() {
    if (!stopTarget.value || !stopDate.value) return;
    stopError.value = '';
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
      stopError.value = '中止日は作業開始日以降の日付を指定してください。';
      ctx.notice.value = stopError.value;
      return;
    }

    if (ctx.useMockData) {
      item.status = '作業中止';
      ctx.histories.value.unshift({
        id: nextId(ctx.histories.value),
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
      stopError.value = '';
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
    } else {
      stopError.value = ctx.notice.value;
    }
  }

  return { stopTarget, stopReason, stopDate, stopError, startStop, confirmStop };
}
