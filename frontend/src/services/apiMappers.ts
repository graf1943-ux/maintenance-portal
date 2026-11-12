import type { Bike, EventCheck, History, Plan, Status, Work } from '../types';

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

export type ApiWork = {
  id: number;
  bikeId: number;
  startDate: string;
  endDate?: string | null;
  status: Status;
  title: string;
  detail: string;
  shopId: number;
  receptionist?: string | null;
  receptionistId?: number | null;
};

export type ApiEvent = {
  id: number;
  bikeId: number;
  name: string;
  eventDate: string;
  inspectionStartDate: string;
  inspectionEndDate?: string | null;
  status: Status;
  memo: string;
  shopId: number;
  receptionistId?: number | null;
};

export type ApiCustomer = { id: number; name: string; email: string; phone?: string };
export type ApiShop = { id: number; name: string; address: string; phone: string };
export type ApiStaff = { id: number; shopId: number; name: string; role: string };
export type ApiHistory = {
  id: number;
  bikeId: number;
  date: string;
  startDate?: string | null;
  category: string;
  title: string;
  detail: string;
  memo?: string | null;
};

export const dateSlash = (v?: string | null) =>
  v ? String(v).slice(0, 10).replace(/-/g, '/') : '';
export const dateIso = (v?: string | null) => (v ? String(v).slice(0, 10).replace(/\//g, '-') : '');
export function mapBike(x: ApiBike, receptionistName: (id: number) => string): Bike {
  return {
    id: x.id,
    customerId: x.customerId,
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
export function mapWork(x: ApiWork, receptionistName: (id: number) => string): Work {
  return {
    id: x.id,
    bikeId: x.bikeId,
    date: dateSlash(x.startDate),
    endDate: dateSlash(x.endDate),
    status: x.status as Status,
    title: x.title,
    detail: x.detail,
    shopId: x.shopId,
    receptionist: x.receptionist ?? receptionistName(x.receptionistId ?? 0),
  };
}
export function mapEvent(x: ApiEvent, receptionistName: (id: number) => string): EventCheck {
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
    receptionist: receptionistName(x.receptionistId ?? 0),
  };
}

export function mapHistory(x: ApiHistory): History {
  return {
    id: x.id,
    bikeId: x.bikeId,
    date: dateSlash(x.date),
    startDate: dateSlash(x.startDate),
    category: x.category,
    title: x.title,
    detail: x.detail,
    memo: x.memo ?? '',
  };
}
