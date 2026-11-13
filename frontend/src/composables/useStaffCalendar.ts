import { computed, type Ref } from 'vue';
import type { Bike, EventCheck, Work } from '../types';
import { dateIso } from '../services/apiMappers';

export type StaffCalendarEntry = {
  id: string;
  kind: 'overhaul' | 'maintenance' | 'event';
  kindLabel: string;
  date: string;
  endDate: string;
  targetEventDate?: string;
  title: string;
  customer: string;
  bikeLabel: string;
  status: string;
  isAdHoc: boolean;
  item: Work | EventCheck;
};

interface StaffCalendarContext {
  bikes: Ref<Bike[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
}

/** スタッフの作業カレンダーに表示する予定データを組み立てます。 */
export function useStaffCalendar(ctx: StaffCalendarContext) {
  const entries = computed<StaffCalendarEntry[]>(() => {
    const bikesById = new Map(ctx.bikes.value.map((bike) => [bike.id, bike]));
    const toEntry = (
      kind: StaffCalendarEntry['kind'],
      kindLabel: string,
      item: Work | EventCheck,
      date: string,
      title: string,
      targetEventDate?: string
    ): StaffCalendarEntry => {
      const bike = bikesById.get(item.bikeId);
      const normalizedDate = dateIso(date);
      return {
        id: `${kind}-${item.id}`,
        kind,
        kindLabel,
        date: normalizedDate,
        endDate: dateIso(item.endDate) || normalizedDate,
        targetEventDate: targetEventDate ? dateIso(targetEventDate) : undefined,
        title,
        customer: bike?.customer ?? '',
        bikeLabel: bike ? `${bike.maker} ${bike.model}` : '',
        status: item.status,
        isAdHoc: item.isAdHoc ?? false,
        item,
      };
    };

    return [
      ...ctx.overhauls.value.map((item) =>
        toEntry('overhaul', 'オーバーホール', item, item.date, item.title)
      ),
      ...ctx.maintenance.value.map((item) =>
        toEntry('maintenance', 'メンテナンス', item, item.date, item.title)
      ),
      ...ctx.eventChecks.value.map((item) =>
        toEntry('event', '点検サポート', item, item.inspectionDate, item.name, item.eventDate)
      ),
    ].sort((a, b) => a.date.localeCompare(b.date) || a.title.localeCompare(b.title));
  });

  return { entries };
}
