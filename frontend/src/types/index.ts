export type Page =
  | 'dashboard'
  | 'bikes'
  | 'shops'
  | 'plans'
  | 'overhaul'
  | 'maintenance'
  | 'events'
  | 'history'
  | 'messages'
  | 'mypage';
export type Status =
  '案内待ち' | '案内済み' | '予約済み' | '作業中' | '完了' | 'キャンセル' | '作業中止';
export type Plan = {
  id: number;
  name: string;
  overhaulCount: number;
  maintenanceCount: number;
  inspectionCount: number;
};
export type Bike = {
  id: number;
  customerId?: number;
  customer: string;
  maker: string;
  model: string;
  year: number;
  serial: string;
  purchaseDate?: string;
  planId: number;
  contractStart: string;
  contractEnd: string;
  overhaulUsed: number;
  maintenanceUsed: number;
  inspectionUsed: number;
  overhaulCompleted: string;
  shopId: number;
  receptionist: string;
  isAdHoc?: boolean;
};
export type Work = {
  id: number;
  bikeId: number;
  date: string;
  endDate?: string;
  status: Status;
  title: string;
  detail: string;
  shopId: number;
  receptionist: string;
  isAdHoc?: boolean;
};
export type EventCheck = {
  id: number;
  bikeId: number;
  name: string;
  eventDate: string;
  inspectionDate: string;
  endDate?: string;
  status: Status;
  memo: string;
  shopId: number;
  receptionist: string;
  isAdHoc?: boolean;
};
export type WorkChecklistResult = '良好' | '調整' | '交換' | '対象外';
export type WorkChecklistItem = {
  item: string;
  result: WorkChecklistResult;
  note: string;
};
export type WorkReportPart = {
  name: string;
  quantity: number;
  unit: string;
};
export type WorkReport = {
  workMinutes: number;
  checklist: WorkChecklistItem[];
  parts: WorkReportPart[];
  shopName?: string;
  staffName?: string;
};
export type History = {
  id: number;
  bikeId: number;
  date: string;
  startDate?: string;
  category: string;
  title: string;
  detail: string;
  memo?: string;
  isAdHoc?: boolean;
  report?: WorkReport;
};
