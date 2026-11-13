import { ref, type Ref } from 'vue';
import { apiPut } from '../api';
import type { Bike, EventCheck, Work } from '../types';
import { dateIso } from '../services/apiMappers';
import { isOverhaulSeason } from '../services/contractRules';
import { findReceptionist } from '../utils/staff';

export type EditableWorkKind = 'maintenance' | 'event' | 'overhaul';
export type EditTarget = { kind: EditableWorkKind; id: number };

type StaffMember = { id: number; shopId: number; name: string; role: string };
type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;

interface WorkEditingContext {
  bikes: Ref<Bike[]>;
  staff: Ref<StaffMember[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  notice: Ref<string>;
  apiAction: ApiAction;
}

/** 予定・点検の編集フォーム初期化、検証、保存をまとめます。 */
export function useWorkEditing(ctx: WorkEditingContext) {
  const editTarget = ref<EditTarget | null>(null);
  const editDate = ref('');
  const editEndDate = ref('');
  const editTitle = ref('');
  const editDetail = ref('');
  const editReceptionist = ref('');
  const editError = ref('');

  function openEdit(kind: EditableWorkKind, item: Work | EventCheck) {
    editError.value = '';
    editTarget.value = { kind, id: item.id };
    const startDate = 'inspectionDate' in item ? item.inspectionDate : item.date;
    editDate.value = startDate.replace(/\//g, '-');
    editEndDate.value = (item.endDate || '').replace(/\//g, '-');
    editTitle.value = 'name' in item ? item.name : item.title;
    editDetail.value = 'name' in item ? item.memo : item.detail;
    editReceptionist.value = item.receptionist || '田中';
  }

  async function saveEdit() {
    const target = editTarget.value;
    if (!target) return;
    const { kind, id } = target;

    if (kind === 'overhaul') {
      const work = ctx.overhauls.value.find((item) => item.id === id);
      const bike = ctx.bikes.value.find((item) => item.id === work?.bikeId);
      const contractYear = Number(bike?.contractStart.slice(0, 4));
      if (
        !bike ||
        !isOverhaulSeason(editDate.value, contractYear) ||
        (editEndDate.value && !isOverhaulSeason(editEndDate.value, contractYear))
      ) {
        editError.value =
          'オーバーホールの実施予定日は契約開始年の11月1日〜翌年2月末の期間で設定してください。';
        ctx.notice.value = editError.value;
        return;
      }
    }

    const list: (Work | EventCheck)[] =
      kind === 'maintenance'
        ? ctx.maintenance.value
        : kind === 'event'
          ? ctx.eventChecks.value
          : ctx.overhauls.value;
    const item = list.find((candidate) => candidate.id === id);
    if (!item) return;

    const staffMember = findReceptionist(ctx.staff.value, item.shopId, editReceptionist.value);
    if (!staffMember) {
      editError.value = '受付担当者を確認してください。';
      ctx.notice.value = editError.value;
      return;
    }
    if (editEndDate.value && editEndDate.value < editDate.value) {
      editError.value = '完了日は開始日以降の日付にしてください。';
      ctx.notice.value = editError.value;
      return;
    }

    if (kind === 'event') {
      if (!('eventDate' in item)) return;
      const ok = await ctx.apiAction(
        () =>
          apiPut(`/events/${id}`, {
            id,
            bikeId: item.bikeId,
            name: editTitle.value,
            eventDate: dateIso(item.eventDate),
            inspectionStartDate: editDate.value,
            inspectionEndDate: editEndDate.value || null,
            status: item.status,
            memo: editDetail.value,
            shopId: item.shopId,
            receptionistId: staffMember.id,
            isAdHoc: item.isAdHoc ?? false,
          }),
        '予定内容を更新しました。契約回数は変更していません。'
      );
      if (ok) {
        editTarget.value = null;
      } else {
        editError.value = ctx.notice.value;
      }
      return;
    }

    if ('eventDate' in item) return;
    const ok = await ctx.apiAction(
      () =>
        apiPut(`/work/${id}`, {
          startDate: editDate.value,
          endDate: editEndDate.value || null,
          status: item.status,
          title: editTitle.value,
          detail: editDetail.value,
          receptionistId: staffMember.id,
          isAdHoc: item.isAdHoc ?? false,
        }),
      '予定内容を更新しました。契約回数は変更していません。'
    );
    if (ok) {
      editTarget.value = null;
    } else {
      editError.value = ctx.notice.value;
    }
  }

  return {
    editTarget,
    editDate,
    editEndDate,
    editTitle,
    editDetail,
    editReceptionist,
    editError,
    openEdit,
    saveEdit,
  };
}
