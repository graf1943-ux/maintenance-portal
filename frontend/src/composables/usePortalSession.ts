import { onMounted, type Ref } from 'vue';
import { authState, restoreAuthentication, signIn, signOut, type PortalRole } from '../auth';
import { apiGet } from '../api';
import type { Page, Bike, EventCheck, History, Plan, Work } from '../types';
import type { ApiCustomer, ApiShop, ApiStaff } from '../services/apiMappers';

export type PortalProfile = {
  role: PortalRole;
  customerId?: number;
  name: string;
  email?: string;
  isStaffAdmin?: boolean;
};

type DataSourceStatus = 'mock' | 'api' | 'error';

interface PortalSessionContext {
  useMockData: boolean;
  portalProfile: Ref<PortalProfile | null>;
  authError: Ref<string>;
  role: Ref<PortalRole>;
  page: Ref<Page>;
  dataSourceStatus: Ref<DataSourceStatus>;
  notice: Ref<string>;
  customers: Ref<ApiCustomer[]>;
  shops: Ref<ApiShop[]>;
  staff: Ref<ApiStaff[]>;
  plans: Ref<Plan[]>;
  bikes: Ref<Bike[]>;
  overhauls: Ref<Work[]>;
  maintenance: Ref<Work[]>;
  eventChecks: Ref<EventCheck[]>;
  histories: Ref<History[]>;
  reloadData: () => Promise<void>;
}

/** 認証復元とポータルのサインイン・サインアウト状態を管理します。 */
export function usePortalSession(ctx: PortalSessionContext) {
  async function connectPortal() {
    const profile = await apiGet<PortalProfile>('/me');
    if (profile.role !== authState.role)
      throw new Error('アカウントの権限を確認できませんでした。');
    ctx.portalProfile.value = profile;
    ctx.role.value = profile.role;
    ctx.page.value = profile.role === 'customer' ? 'mypage' : 'dashboard';
    await ctx.reloadData();
    ctx.dataSourceStatus.value = 'api';
    ctx.notice.value = 'バックエンドAPIに接続しました。';
  }

  async function handleSignIn(selectedRole: PortalRole) {
    ctx.authError.value = '';
    try {
      await signIn(selectedRole);
    } catch (error) {
      ctx.portalProfile.value = null;
      ctx.authError.value = error instanceof Error ? error.message : 'サインインできませんでした。';
    }
  }

  async function handleSignOut() {
    ctx.portalProfile.value = null;
    ctx.authError.value = '';
    try {
      await signOut();
    } catch (error) {
      ctx.authError.value =
        error instanceof Error ? error.message : 'サインアウトできませんでした。';
    }
    ctx.customers.value = [];
    ctx.shops.value = [];
    ctx.staff.value = [];
    ctx.plans.value = [];
    ctx.bikes.value = [];
    ctx.overhauls.value = [];
    ctx.maintenance.value = [];
    ctx.eventChecks.value = [];
    ctx.histories.value = [];
    ctx.role.value = 'staff';
    ctx.page.value = 'dashboard';
  }

  onMounted(async () => {
    if (ctx.useMockData) {
      ctx.notice.value = 'Vue内のモックデータを表示しています。APIへの通信は行いません。';
      return;
    }

    ctx.customers.value = [];
    ctx.shops.value = [];
    ctx.staff.value = [];
    ctx.plans.value = [];
    ctx.bikes.value = [];
    ctx.overhauls.value = [];
    ctx.maintenance.value = [];
    ctx.eventChecks.value = [];
    ctx.histories.value = [];

    try {
      if (!(await restoreAuthentication())) return;
      await connectPortal();
    } catch (error) {
      ctx.authError.value =
        error instanceof Error ? error.message : 'サインイン情報を確認できませんでした。';
      ctx.dataSourceStatus.value = 'error';
    }
  });

  return { connectPortal, handleSignIn, handleSignOut };
}
