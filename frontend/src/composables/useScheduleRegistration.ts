import { ref, watch, type ComputedRef, type Ref } from 'vue';
import { apiPost } from '../api';
import type { Bike, EventCheck, Work } from '../types';
import { dateSlash } from '../services/apiMappers';
import { isOverhaulSeason } from '../services/contractRules';
import type { ContractService, ScheduleCapacity } from './useContractUsage';
import { findReceptionist } from '../utils/staff';
import { nextId } from '../utils/id';

type StaffMember = { id: number; shopId: number; name: string; role: string };
type ApiAction = (action: () => Promise<unknown>, success: string) => Promise<boolean>;

interface ScheduleRegistrationContext {
  bikes: Ref<Bike[]>;
  staff: Ref<StaffMember[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  newOverhaulBike: Ref<number>;
  overhaulBikeSearch: Ref<string>;
  newOverhaulReceptionDate: Ref<string>;
  newOverhaulDate: Ref<string>;
  newOverhaulEndDate: Ref<string>;
  newOverhaulIsAdHoc: Ref<boolean>;
  newOverhaulTitle: Ref<string>;
  newOverhaulDetail: Ref<string>;
  newWorkBike: Ref<number>;
  maintenanceBikeSearch: Ref<string>;
  newWorkDate: Ref<string>;
  newWorkEndDate: Ref<string>;
  newWorkIsAdHoc: Ref<boolean>;
  newWorkTitle: Ref<string>;
  newWorkDetail: Ref<string>;
  newEventBike: Ref<number>;
  eventBikeSearch: Ref<string>;
  newEventName: Ref<string>;
  newEventDate: Ref<string>;
  newInspectionDate: Ref<string>;
  newInspectionEndDate: Ref<string>;
  newEventIsAdHoc: Ref<boolean>;
  notice: Ref<string>;
  showOverhaulModal: Ref<boolean>;
  showMaintenanceModal: Ref<boolean>;
  showEventModal: Ref<boolean>;
  dataSourceStatus: Ref<'mock' | 'api' | 'error'>;
  useMockData: boolean;
  bikeLabel: (id: number) => string;
  scheduleRemaining: (bike: Bike, kind: ContractService) => number;
  scheduleCapacityFor: (
    bikeId: Ref<number>,
    kind: ContractService
  ) => ComputedRef<ScheduleCapacity>;
  apiAction: ApiAction;
}

/** オーバーホール・メンテナンス・点検予定の登録フローをまとめます。 */
export function useScheduleRegistration(ctx: ScheduleRegistrationContext) {
  const overhaulScheduleCapacity = ctx.scheduleCapacityFor(ctx.newOverhaulBike, 'overhaul');
  const maintenanceScheduleCapacity = ctx.scheduleCapacityFor(ctx.newWorkBike, 'maintenance');
  const eventScheduleCapacity = ctx.scheduleCapacityFor(ctx.newEventBike, 'inspection');
  const overhaulError = ref('');
  const maintenanceError = ref('');
  const eventError = ref('');
  watch(ctx.showOverhaulModal, (isOpen) => {
    if (isOpen) overhaulError.value = '';
  });
  watch(ctx.showMaintenanceModal, (isOpen) => {
    if (isOpen) maintenanceError.value = '';
  });
  watch(ctx.showEventModal, (isOpen) => {
    if (isOpen) eventError.value = '';
  });

  function setError(error: Ref<string>, message: string) {
    error.value = message;
    ctx.notice.value = message;
  }

  function receptionistFor(bike: Bike, error: Ref<string>) {
    const receptionist = findReceptionist(ctx.staff.value, bike.shopId, bike.receptionist);
    if (!receptionist) setError(error, '受付担当者を確認してください。');
    return receptionist;
  }

  async function addOverhaul() {
    overhaulError.value = '';
    if (ctx.bikeLabel(ctx.newOverhaulBike.value) !== ctx.overhaulBikeSearch.value) {
      setError(overhaulError, '顧客・自転車は候補から選択してください。');
      return;
    }
    const b = ctx.bikes.value.find((x) => x.id === ctx.newOverhaulBike.value);
    if (!b) {
      setError(overhaulError, '登録対象の自転車を選択してください。');
      return;
    }
    if (ctx.scheduleRemaining(b, 'overhaul') <= 0) {
      setError(
        overhaulError,
        `${b.maker} ${b.model} のオーバーホールは、契約上限回数に達しています。既存の予定または実施履歴を確認してください。`
      );
      return;
    }
    const receptionDate = ctx.newOverhaulReceptionDate.value;
    const contractYear = Number(b.contractStart.slice(0, 4));
    if (!receptionDate || receptionDate > `${contractYear}-12-31`) {
      setError(overhaulError, 'オーバーホールの受付期限は契約開始年の12月31日です。');
      return;
    }
    if (
      !isOverhaulSeason(ctx.newOverhaulDate.value, contractYear) ||
      (ctx.newOverhaulEndDate.value &&
        !isOverhaulSeason(ctx.newOverhaulEndDate.value, contractYear))
    ) {
      setError(
        overhaulError,
        'オーバーホールの実施予定日は契約開始年の11月1日〜翌年2月末の期間で設定してください。'
      );
      return;
    }
    if (receptionDate > ctx.newOverhaulDate.value) {
      setError(
        overhaulError,
        '受付日は実施予定日以前の日付にしてください。同日での受付・実施は可能です。'
      );
      return;
    }
    if (ctx.newOverhaulEndDate.value && ctx.newOverhaulEndDate.value < ctx.newOverhaulDate.value) {
      setError(overhaulError, '完了予定日は開始予定日以降にしてください。');
      return;
    }
    const person = receptionistFor(b, overhaulError);
    if (!person) return;
    if (ctx.useMockData) {
      const item: Work = {
        id: nextId(ctx.overhauls.value),
        bikeId: b.id,
        date: dateSlash(ctx.newOverhaulDate.value),
        endDate: ctx.newOverhaulEndDate.value ? dateSlash(ctx.newOverhaulEndDate.value) : undefined,
        status: ctx.newOverhaulIsAdHoc.value ? '作業中' : '案内待ち',
        title: ctx.newOverhaulTitle.value,
        detail: ctx.newOverhaulDetail.value,
        shopId: b.shopId,
        receptionist: person.name,
        isAdHoc: ctx.newOverhaulIsAdHoc.value,
      };
      ctx.overhauls.value.unshift(item);
      ctx.showOverhaulModal.value = false;
      ctx.newOverhaulIsAdHoc.value = false;
      ctx.dataSourceStatus.value = 'mock';
      ctx.notice.value =
        'オーバーホール予定をVueモックデータに登録しました。画面を再読み込みすると初期データに戻ります。';
      return;
    }
    const ok = await ctx.apiAction(
      () =>
        apiPost('/overhauls', {
          bikeId: b.id,
          startDate: ctx.newOverhaulDate.value,
          endDate: ctx.newOverhaulEndDate.value || null,
          title: ctx.newOverhaulTitle.value,
          detail: ctx.newOverhaulDetail.value,
          shopId: b.shopId,
          receptionistId: person.id,
          isAdHoc: ctx.newOverhaulIsAdHoc.value,
        }),
      'オーバーホール予定を登録しました。実施完了までは契約回数を消費しません。'
    );
    if (ok) {
      ctx.showOverhaulModal.value = false;
      ctx.newOverhaulIsAdHoc.value = false;
    } else {
      overhaulError.value = ctx.notice.value;
    }
  }

  async function addMaintenance() {
    maintenanceError.value = '';
    if (ctx.bikeLabel(ctx.newWorkBike.value) !== ctx.maintenanceBikeSearch.value) {
      setError(maintenanceError, '顧客・自転車は候補から選択してください。');
      return;
    }
    const b = ctx.bikes.value.find((x) => x.id === ctx.newWorkBike.value);
    if (!b) {
      setError(maintenanceError, '登録対象の自転車を選択してください。');
      return;
    }
    if (ctx.scheduleRemaining(b, 'maintenance') <= 0) {
      setError(
        maintenanceError,
        `${b.maker} ${b.model} のメンテナンスは、契約上限回数に達しています。既存の予定または実施履歴を確認してください。`
      );
      return;
    }
    if (ctx.newWorkEndDate.value && ctx.newWorkEndDate.value < ctx.newWorkDate.value) {
      setError(maintenanceError, '完了予定日は開始予定日以降にしてください。');
      return;
    }
    const person = receptionistFor(b, maintenanceError);
    if (!person) return;
    if (ctx.useMockData) {
      ctx.maintenance.value.unshift({
        id: nextId(ctx.maintenance.value),
        bikeId: b.id,
        date: dateSlash(ctx.newWorkDate.value),
        endDate: ctx.newWorkEndDate.value ? dateSlash(ctx.newWorkEndDate.value) : undefined,
        status: ctx.newWorkIsAdHoc.value ? '作業中' : '案内待ち',
        title: ctx.newWorkTitle.value,
        detail: ctx.newWorkDetail.value,
        shopId: b.shopId,
        receptionist: person.name,
        isAdHoc: ctx.newWorkIsAdHoc.value,
      });
      ctx.showMaintenanceModal.value = false;
      ctx.newWorkIsAdHoc.value = false;
      ctx.notice.value = 'メンテナンス計画をVueモックデータに登録しました。';
      return;
    }
    const ok = await ctx.apiAction(
      () =>
        apiPost('/maintenance', {
          bikeId: b.id,
          startDate: ctx.newWorkDate.value,
          endDate: ctx.newWorkEndDate.value || null,
          title: ctx.newWorkTitle.value,
          detail: ctx.newWorkDetail.value,
          shopId: b.shopId,
          receptionistId: person.id,
          isAdHoc: ctx.newWorkIsAdHoc.value,
        }),
      'メンテナンス計画を追加しました。実施完了までは回数を消費しません。'
    );
    if (ok) {
      ctx.showMaintenanceModal.value = false;
      ctx.newWorkIsAdHoc.value = false;
    } else {
      maintenanceError.value = ctx.notice.value;
    }
  }

  async function addEvent() {
    eventError.value = '';
    if (ctx.bikeLabel(ctx.newEventBike.value) !== ctx.eventBikeSearch.value) {
      setError(eventError, '顧客・自転車は候補から選択してください。');
      return;
    }
    const b = ctx.bikes.value.find((x) => x.id === ctx.newEventBike.value);
    if (!b || !ctx.newEventName.value.trim()) {
      setError(eventError, 'イベント名を入力してください。');
      return;
    }
    if (ctx.scheduleRemaining(b, 'inspection') <= 0) {
      setError(
        eventError,
        `${b.maker} ${b.model} の点検サポートは、契約上限回数に達しています。既存の予定または実施履歴を確認してください。`
      );
      return;
    }
    if (
      ctx.newInspectionEndDate.value &&
      ctx.newInspectionEndDate.value < ctx.newInspectionDate.value
    ) {
      setError(eventError, '点検完了予定日は開始予定日以降にしてください。');
      return;
    }
    const person = receptionistFor(b, eventError);
    if (!person) return;
    if (ctx.useMockData) {
      ctx.eventChecks.value.unshift({
        id: nextId(ctx.eventChecks.value),
        bikeId: b.id,
        name: ctx.newEventName.value,
        eventDate: dateSlash(ctx.newEventDate.value),
        inspectionDate: dateSlash(ctx.newInspectionDate.value),
        endDate: ctx.newInspectionEndDate.value
          ? dateSlash(ctx.newInspectionEndDate.value)
          : undefined,
        status: ctx.newEventIsAdHoc.value ? '作業中' : '案内待ち',
        memo: '目標イベントに向けた点検サポート',
        shopId: b.shopId,
        receptionist: person.name,
        isAdHoc: ctx.newEventIsAdHoc.value,
      });
      ctx.newEventName.value = '';
      ctx.showEventModal.value = false;
      ctx.newEventIsAdHoc.value = false;
      ctx.notice.value = 'イベント・点検予定をVueモックデータに登録しました。';
      return;
    }
    const ok = await ctx.apiAction(
      () =>
        apiPost('/events', {
          bikeId: b.id,
          name: ctx.newEventName.value,
          eventDate: ctx.newEventDate.value,
          inspectionStartDate: ctx.newInspectionDate.value,
          inspectionEndDate: ctx.newInspectionEndDate.value || null,
          memo: '目標イベントに向けた点検サポート',
          shopId: b.shopId,
          receptionistId: person.id,
          isAdHoc: ctx.newEventIsAdHoc.value,
        }),
      '目標イベントと点検予定を登録しました。'
    );
    if (ok) {
      ctx.newEventName.value = '';
      ctx.showEventModal.value = false;
      ctx.newEventIsAdHoc.value = false;
    } else {
      eventError.value = ctx.notice.value;
    }
  }

  return {
    overhaulScheduleCapacity,
    maintenanceScheduleCapacity,
    eventScheduleCapacity,
    overhaulError,
    maintenanceError,
    eventError,
    addOverhaul,
    addMaintenance,
    addEvent,
  };
}
