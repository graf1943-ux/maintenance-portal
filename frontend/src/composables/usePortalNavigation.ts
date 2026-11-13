import type { Ref } from 'vue';
import type { Page } from '../types';

interface PortalNavigationContext {
  page: Ref<Page>;
  role: Ref<'staff' | 'customer'>;
  notice: Ref<string>;
  searchHistory: Ref<string>;
  useMockData: boolean;
}

/** ページ遷移と、モック環境での表示ロール切り替えを管理します。 */
export function usePortalNavigation(ctx: PortalNavigationContext) {
  function setPage(page: Page) {
    ctx.page.value = page;
    ctx.notice.value = '';
    if (page === 'history') {
      ctx.searchHistory.value = '';
    }
  }

  function toggleRole() {
    if (!ctx.useMockData) return;
    ctx.role.value = ctx.role.value === 'staff' ? 'customer' : 'staff';
    ctx.page.value = ctx.role.value === 'customer' ? 'mypage' : 'dashboard';
  }

  return { setPage, toggleRole };
}
