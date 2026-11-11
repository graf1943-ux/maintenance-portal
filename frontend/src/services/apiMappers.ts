import type { Bike, EventCheck, Plan, Status, Work } from '../types';

export type ApiBike = {
  id: number;
  customerId: number;
  customer: string;
  maker: string;
  model: string;
  year: number;
  serial: string;
  contractStart?: string | null;
  contractEnd?: string | null;
  overhaulUsed: number;
  maintenanceUsed: number;
  inspectionUsed: number;
  plan?: Plan | null;
  shopId?: number | null;
  receptionistId?: number | null;
};
export const dateSlash = (v?: string | null) =>
  v ? String(v).slice(0, 10).replace(/-/g, '/') : '';
export const dateIso = (v?: string | null) => (v ? String(v).slice(0, 10).replace(/\//g, '-') : '');
export function mapBike(x: ApiBike, receptionistName: (id: number) => string): Bike {
  return {
    id: x.id,
    customer: x.customer,
    maker: x.maker,
    model: x.model,
    year: x.year,
    serial: x.serial,
    planId: x.plan?.id ?? 1,
    contractStart: dateSlash(x.contractStart),
    contractEnd: dateSlash(x.contractEnd),
    overhaulUsed: x.overhaulUsed,
    maintenanceUsed: x.maintenanceUsed,
    inspectionUsed: x.inspectionUsed,
    overhaulCompleted: '',
    shopId: x.shopId ?? 1,
    receptionist: receptionistName(x.receptionistId ?? 0),
  };
}
export function mapWork(x: any, receptionistName: (id: number) => string): Work {
  return {
    id: x.id,
    bikeId: x.bikeId,
    date: dateSlash(x.startDate),
    endDate: dateSlash(x.endDate),
    status: x.status as Status,
    title: x.title,
    detail: x.detail,
    shopId: x.shopId,
    receptionist: x.receptionist ?? receptionistName(x.receptionistId),
  };
}
export function mapEvent(x: any, receptionistName: (id: number) => string): EventCheck {
  return {
    id: x.id,
    bikeId: x.bikeId,
    name: x.name,
    eventDate: dateSlash(x.eventDate),
    inspectionDate: dateSlash(x.inspectionStartDate),
    endDate: dateSlash(x.inspectionEndDate),
    status: x.status,
    memo: x.memo,
    shopId: x.shopId,
    receptionist: receptionistName(x.receptionistId),
  };
}
