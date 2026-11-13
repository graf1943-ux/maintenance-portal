import { watch, type Ref } from 'vue';
import { localDateInput } from '../utils/date';

export interface AdHocDateGroup {
  enabled: Ref<boolean>;
  dates: Ref<string>[];
}

/** 単発作業の有効化時に日付を今日へ揃え、解除時に元の日付へ戻します。 */
export function useAdHocScheduleDates(groups: AdHocDateGroup[]) {
  for (const { enabled, dates } of groups) {
    let previousDates: string[] | null = null;
    watch(enabled, (isEnabled) => {
      if (isEnabled) {
        previousDates = dates.map((date) => date.value);
        const today = localDateInput();
        dates.forEach((date) => (date.value = today));
      } else if (previousDates) {
        dates.forEach((date, index) => (date.value = previousDates?.[index] ?? date.value));
        previousDates = null;
      }
    });
  }
}
