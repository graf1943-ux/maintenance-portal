import type { ScheduleTimeSlot } from '../types';

const maximumEstimatedMinutes = 10_080;
const maximumScheduleSlots = 100;
const timePattern = /^(?:[01]\d|2[0-3]):[0-5]\d$/;

/** 予定時間枠と作業見込み時間の入力を検証します。 */
export function validateScheduleTimePlan(
  startDate: string,
  endDate: string,
  estimatedWorkMinutes: number | null,
  scheduleSlots: ScheduleTimeSlot[]
) {
  if (
    estimatedWorkMinutes !== null &&
    (!Number.isInteger(estimatedWorkMinutes) ||
      estimatedWorkMinutes < 1 ||
      estimatedWorkMinutes > maximumEstimatedMinutes)
  ) {
    return '作業見込み時間は1〜10,080分で入力してください。';
  }

  if (scheduleSlots.length > maximumScheduleSlots) {
    return `時間枠は${maximumScheduleSlots}件以内で登録してください。`;
  }

  const lastDate = endDate || startDate;
  const slots = [...scheduleSlots].sort(
    (a, b) => a.date.localeCompare(b.date) || a.startTime.localeCompare(b.startTime)
  );
  for (let index = 0; index < slots.length; index += 1) {
    const slot = slots[index];
    if (!slot.date || slot.date < startDate || slot.date > lastDate) {
      return '時間枠の日付は作業開始日〜完了予定日の範囲で選択してください。';
    }
    if (!timePattern.test(slot.startTime) || !timePattern.test(slot.endTime)) {
      return '時間枠の開始・終了時刻を入力してください。';
    }
    if (slot.startTime >= slot.endTime) {
      return '終了時刻は開始時刻より後にしてください。';
    }
    const previous = slots[index - 1];
    if (previous?.date === slot.date && slot.startTime < previous.endTime) {
      return '同じ作業内の時間枠が重複しています。';
    }
  }

  return null;
}
