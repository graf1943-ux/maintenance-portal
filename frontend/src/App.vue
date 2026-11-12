<script setup lang="ts">
  import { ref } from 'vue';
  import AppSidebar from './components/layout/AppSidebar.vue';
  import AppHeader from './components/layout/AppHeader.vue';
  import DashboardView from './components/dashboard/DashboardView.vue';
  import BikeContractsView from './components/bikes/BikeContractsView.vue';
  import BikeDetailModal from './components/bikes/BikeDetailModal.vue';
  import CustomerDetailModal from './components/customers/CustomerDetailModal.vue';
  import ContractRegistrationModal from './components/contracts/ContractRegistrationModal.vue';
  import MaintenanceRegistrationModal from './components/maintenance/MaintenanceRegistrationModal.vue';
  import OverhaulRegistrationModal from './components/overhaul/OverhaulRegistrationModal.vue';
  import EventInspectionRegistrationModal from './components/events/EventInspectionRegistrationModal.vue';
  import WorkCompletionModal from './components/work/WorkCompletionModal.vue';
  import WorkEditModal from './components/work/WorkEditModal.vue';
  import MasterDataModals from './components/layout/MasterDataModals.vue';
  import OverhaulView from './components/overhaul/OverhaulView.vue';
  import MaintenanceView from './components/maintenance/MaintenanceView.vue';
  import EventInspectionView from './components/events/EventInspectionView.vue';
  import HistoryView from './components/history/HistoryView.vue';
  import MessagesView from './components/messages/MessagesView.vue';
  import EmailPreviewModal from './components/messages/EmailPreviewModal.vue';
  import MypageView from './components/mypage/MypageView.vue';
  import ShopsView from './components/shops/ShopsView.vue';
  import PlansView from './components/plans/PlansView.vue';
  import LoginView from './components/auth/LoginView.vue';
  import { authState, isAuthConfigured } from './auth';
  import type { Page, Status, Plan, Bike, Work, EventCheck, History } from './types';
  import { pageTitles } from './constants/pages';
  import { useListSort } from './utils/list';
  import type { ApiCustomer, ApiShop, ApiStaff } from './services/apiMappers';
  import { useScheduleRegistration } from './composables/useScheduleRegistration';
  import { useWorkLifecycle } from './composables/useWorkLifecycle';
  import { useContractUsage } from './composables/useContractUsage';
  import { useCustomerPortal } from './composables/useCustomerPortal';
  import { useBikeListFilters } from './composables/useBikeListFilters';
  import { useDashboardData } from './composables/useDashboardData';
  import { useCustomerDetails } from './composables/useCustomerDetails';
  import { useActivityLists } from './composables/useActivityLists';
  import { useContractRegistration } from './composables/useContractRegistration';
  import { useMasterDataRegistration } from './composables/useMasterDataRegistration';
  import { useWorkEditing } from './composables/useWorkEditing';
  import { useCustomerIdentityLink } from './composables/useCustomerIdentityLink';
  import { usePortalApiData } from './composables/usePortalApiData';
  import { usePortalSession, type PortalProfile } from './composables/usePortalSession';
  import { createMockData } from './fixtures/mockData';
  import { useStaffLists } from './composables/useStaffLists';
  import { useCustomerNavigation } from './composables/useCustomerNavigation';
  import { useBikeOptions } from './composables/useBikeOptions';
  import { useBikeCatalog } from './composables/useBikeCatalog';
  import { useMailPreview } from './composables/useMailPreview';
  import { usePortalNavigation } from './composables/usePortalNavigation';
  import { useDetailSelection } from './composables/useDetailSelection';
  import { useBikeDetails } from './composables/useBikeDetails';
  const USE_MOCK_DATA = import.meta.env.VITE_USE_MOCK_DATA === 'true';
  const mockData = createMockData();
  const dataSourceStatus = ref<'mock' | 'api' | 'error'>(USE_MOCK_DATA ? 'mock' : 'api');
  const portalProfile = ref<PortalProfile | null>(null);
  const authError = ref('');
  const customerAuthEnabled = isAuthConfigured('customer');
  const staffAuthEnabled = isAuthConfigured('staff');
  const page = ref<Page>('dashboard'),
    role = ref<'staff' | 'customer'>('staff'),
    selectedBikeId = ref(1),
    notice = ref('');
  const showOverhaulModal = ref(false),
    showMaintenanceModal = ref(false),
    showEventModal = ref(false);
  const showCustomerDetail = ref(false),
    showBikeDetail = ref(false),
    selectedCustomerName = ref(''),
    selectedCustomerId = ref<number | null>(null);
  const today = '2026年11月10日（火）';
  // 一覧ごとのキーワード検索。顧客名・自転車・作業名・店舗・担当者などを横断して検索します。
  const searchShops = ref(''),
    searchBikes = ref(''),
    searchOverhauls = ref(''),
    searchMaintenance = ref(''),
    searchEvents = ref(''),
    searchHistory = ref(''),
    searchMessages = ref(''),
    searchMyPage = ref('');
  const { toggleListSort, sortByDate, sortLabel } = useListSort();
  const shops = ref<ApiShop[]>(mockData.shops);
  const staff = ref<ApiStaff[]>(mockData.staff);
  const customers = ref<ApiCustomer[]>(mockData.customers);
  const plans = ref<Plan[]>(mockData.plans);

  const bikes = ref<Bike[]>(mockData.bikes);
  const {
    selectedBikeFilter,
    customerFilterText,
    bikeFilterText,
    matchesBikeFilter,
    clearBikeNameFilters,
  } = useBikeListFilters(bikes);
  const { planFor, selectedBike } = useBikeCatalog(bikes, plans, selectedBikeId);
  const overhauls = ref<Work[]>(mockData.overhauls);
  const maintenance = ref<Work[]>(mockData.maintenance);
  const eventChecks = ref<EventCheck[]>(mockData.eventChecks);
  const histories = ref<History[]>(mockData.histories);
  const { dataSourceTitle, dataSourceMessage, reloadData, apiAction } = usePortalApiData({
    useMockData: USE_MOCK_DATA,
    dataSourceStatus,
    role,
    notice,
    customers,
    shops,
    staff,
    plans,
    bikes,
    overhauls,
    maintenance,
    eventChecks,
    histories,
  });
  const { handleSignIn, handleSignOut } = usePortalSession({
    useMockData: USE_MOCK_DATA,
    portalProfile,
    authError,
    role,
    page,
    dataSourceStatus,
    notice,
    customers,
    shops,
    staff,
    plans,
    bikes,
    overhauls,
    maintenance,
    eventChecks,
    histories,
    reloadData,
  });
  const { remaining, scheduleRemaining, scheduleCapacityFor } = useContractUsage({
    bikes,
    overhauls,
    maintenance,
    eventChecks,
    planFor,
  });
  const { bikeDetails } = useBikeDetails({ selectedBike, shops, planFor, remaining });
  const newOverhaulBike = ref(1),
    newOverhaulReceptionDate = ref('2026-12-15'),
    newOverhaulDate = ref('2027-01-15'),
    newOverhaulEndDate = ref('2027-01-16'),
    newOverhaulTitle = ref('年間オーバーホール'),
    newOverhaulDetail = ref('全体点検・消耗品確認・駆動系整備');
  const newWorkBike = ref(1),
    newWorkDate = ref('2027-07-01'),
    newWorkEndDate = ref('2027-07-01'),
    newWorkTitle = ref('メインイベントに向けたメンテナンス'),
    newWorkDetail = ref('イベントに向けた状態確認・調整');
  const newEventBike = ref(1),
    newEventName = ref(''),
    newEventDate = ref('2027-08-01'),
    newInspectionDate = ref('2027-07-28'),
    newInspectionEndDate = ref('2027-07-28');
  const { bikeLabel, setBikeFromLabel } = useBikeOptions({
    bikes,
    overhaulBikeId: newOverhaulBike,
    maintenanceBikeId: newWorkBike,
    eventBikeId: newEventBike,
  });
  const overhaulBikeSearch = ref(bikeLabel(newOverhaulBike.value)),
    maintenanceBikeSearch = ref(bikeLabel(newWorkBike.value)),
    eventBikeSearch = ref(bikeLabel(newEventBike.value));
  const { historyRows, selectedBikeHistoryCount, messageRows } = useActivityLists({
    bikes,
    histories,
    maintenance,
    eventChecks,
    selectedBikeId,
    searchHistory,
    searchMessages,
    sortRows: sortByDate,
  });
  const {
    customer: selectedCustomer,
    customerBikes: selectedCustomerBikes,
    customerBikeCards: selectedCustomerBikeCards,
    customerWorks: selectedCustomerWorks,
    customerHistories: selectedCustomerHistories,
  } = useCustomerDetails({
    selectedCustomerName,
    selectedCustomerId,
    customers,
    bikes,
    shops,
    overhauls,
    maintenance,
    eventChecks,
    histories,
    planFor,
    remaining,
    scheduleRemaining,
  });
  const {
    tenantId: customerIdentityTenantId,
    objectId: customerIdentityObjectId,
    error: customerIdentityError,
    message: customerIdentityMessage,
    saving: customerIdentitySaving,
    reset: resetCustomerIdentity,
    link: linkSelectedCustomerIdentity,
  } = useCustomerIdentityLink(selectedCustomer);
  const { showMail, mailTarget, mailKind, composeMail, sendMail } = useMailPreview({ notice });
  const { openCustomerDetail, openBikeDetail } = useDetailSelection({
    selectedCustomerName,
    selectedCustomerId,
    selectedBikeId,
    showCustomerDetail,
    showBikeDetail,
    resetCustomerIdentity,
  });
  const { openCustomerBikeRegistration, openCustomerBikePage, openCustomerWork } =
    useCustomerNavigation({
      bikes,
      selectedBikeId,
      selectedBikeFilter,
      customerFilterText,
      bikeFilterText,
      searchOverhauls,
      searchMaintenance,
      searchEvents,
      searchHistory,
      page,
      showCustomerDetail,
      newOverhaulBike,
      newOverhaulReceptionDate,
      newOverhaulDate,
      newOverhaulEndDate,
      overhaulBikeSearch,
      newWorkBike,
      newWorkDate,
      newWorkEndDate,
      maintenanceBikeSearch,
      newEventBike,
      newEventName,
      newEventDate,
      newInspectionDate,
      newInspectionEndDate,
      eventBikeSearch,
      showOverhaulModal,
      showMaintenanceModal,
      showEventModal,
      notice,
      bikeLabel,
    });
  const { completedCount, upcomingCount, pendingCount, dashboardRows } = useDashboardData({
    bikes,
    overhauls,
    maintenance,
    eventChecks,
    histories,
    sortRows: sortByDate,
  });
  const {
    customerName: customerPortalName,
    bikeCards: mypageBikeCards,
    scheduleRows: mypageScheduleRows,
  } = useCustomerPortal({
    useMockData: USE_MOCK_DATA,
    portalProfile,
    bikes,
    maintenance,
    eventChecks,
    searchText: searchMyPage,
    planFor,
    remaining,
    sortRows: sortByDate,
  });
  const {
    bikeContractRows,
    overhaulRows,
    overhaulBikeFilterCount,
    maintenanceRows,
    maintenanceBikeFilterCount,
    eventInspectionRows,
    eventInspectionCount,
  } = useStaffLists({
    bikes,
    shops,
    overhauls,
    maintenance,
    eventChecks,
    searchBikes,
    searchOverhauls,
    searchMaintenance,
    searchEvents,
    matchesBikeFilter,
    planFor,
    remaining,
    scheduleRemaining,
    sortRows: sortByDate,
  });
  const { setPage, toggleRole } = usePortalNavigation({
    page,
    role,
    notice,
    useMockData: USE_MOCK_DATA,
  });
  const {
    completionTarget,
    completionMemo,
    completionDate,
    updateMaintenanceStatus,
    startWork,
    startCompletion,
    confirmCompletion,
  } = useWorkLifecycle({
    staff,
    overhauls,
    maintenance,
    eventChecks,
    notice,
    apiAction: (action, success) => apiAction(action, success),
  });
  const { contractForm, showContractModal, addContract } = useContractRegistration({
    useMockData: USE_MOCK_DATA,
    customers,
    staff,
    notice,
    selectedBikeId,
    newOverhaulBike,
    overhaulBikeSearch,
    newOverhaulReceptionDate,
    page,
    showOverhaulModal,
    reloadData,
    bikeLabel,
  });
  const {
    newPlan,
    newShopName,
    newShopAddress,
    newShopPhone,
    newStaffName,
    newStaffShop,
    showPlanModal,
    showShopModal,
    showStaffModal,
    addPlan,
    addShop,
    addStaff,
  } = useMasterDataRegistration({
    notice,
    apiAction: (action, success) => apiAction(action, success),
  });

  const {
    editTarget,
    editDate,
    editEndDate,
    editTitle,
    editDetail,
    editReceptionist,
    openEdit,
    saveEdit,
  } = useWorkEditing({
    bikes,
    staff,
    overhauls,
    maintenance,
    eventChecks,
    notice,
    apiAction: (action, success) => apiAction(action, success),
  });

  const {
    overhaulScheduleCapacity,
    maintenanceScheduleCapacity,
    eventScheduleCapacity,
    addOverhaul,
    addMaintenance,
    addEvent,
  } = useScheduleRegistration({
    bikes,
    staff,
    overhauls,
    maintenance,
    eventChecks,
    newOverhaulBike,
    overhaulBikeSearch,
    newOverhaulReceptionDate,
    newOverhaulDate,
    newOverhaulEndDate,
    newOverhaulTitle,
    newOverhaulDetail,
    newWorkBike,
    maintenanceBikeSearch,
    newWorkDate,
    newWorkEndDate,
    newWorkTitle,
    newWorkDetail,
    newEventBike,
    eventBikeSearch,
    newEventName,
    newEventDate,
    newInspectionDate,
    newInspectionEndDate,
    notice,
    showOverhaulModal,
    showMaintenanceModal,
    showEventModal,
    dataSourceStatus,
    useMockData: USE_MOCK_DATA,
    bikeLabel,
    scheduleRemaining,
    scheduleCapacityFor,
    apiAction: (action, success) => apiAction(action, success),
  });
</script>
<template>
  <LoginView
    v-if="!USE_MOCK_DATA && !portalProfile"
    :loading="authState.loading"
    :error="authError"
    :customer-enabled="customerAuthEnabled"
    :staff-enabled="staffAuthEnabled"
    :authenticated="Boolean(authState.account)"
    @sign-in="handleSignIn"
    @sign-out="handleSignOut"
  />
  <template v-else>
    <div class="app-shell">
      <AppSidebar
        :page="page"
        :role="role"
        :user-name="portalProfile?.name"
        :role-toggle-enabled="USE_MOCK_DATA"
        @navigate="setPage"
        @toggle-role="toggleRole"
        @sign-out="handleSignOut"
      />
      <main class="main">
        <AppHeader :page-title="pageTitles[page]" :today="today" />
        <div class="content">
          <div class="data-source-indicator" :class="dataSourceStatus">
            <strong>{{ dataSourceTitle }}</strong><span>{{ dataSourceMessage }}</span>
          </div>
          <div v-if="notice" class="notice">
            <span>{{ notice }}</span><button @click="notice = ''">×</button>
          </div>
          <DashboardView
            v-if="page === 'dashboard'"
            :bikes="bikes"
            :dashboard-rows="dashboardRows"
            :upcoming-count="upcomingCount"
            :pending-count="pendingCount"
            :completed-count="completedCount"
            :customer-sort-label="sortLabel('dashboard', 'customer')"
            :date-sort-label="sortLabel('dashboard', 'date')"
            :status-sort-label="sortLabel('dashboard', 'status')"
            @sort="toggleListSort('dashboard', $event)"
            @open-customer="openCustomerDetail"
          />
          <ShopsView
            v-if="page === 'shops'"
            :shops="shops"
            :staff="staff"
            :search-text="searchShops"
            @update:search-text="searchShops = $event"
            @add-shop="showShopModal = true"
            @add-staff="showStaffModal = true"
          />
          <PlansView v-else-if="page === 'plans'" :plans="plans" @add-plan="showPlanModal = true" />
          <template v-else-if="page === 'bikes'">
            <BikeContractsView
              :customers-count="customers.length"
              :bikes-count="bikes.length"
              :search-text="searchBikes"
              :sort-label="sortLabel('bikes', 'customer')"
              :rows="bikeContractRows"
              @update:search-text="searchBikes = $event"
              @sort="toggleListSort('bikes', 'customer')"
              @add-contract="showContractModal = true"
              @open-customer="openCustomerDetail"
              @open-bike="openBikeDetail"
            />
          </template>
          <template v-else-if="page === 'overhaul'">
            <OverhaulView
              :rows="overhaulRows"
              :count="overhaulBikeFilterCount"
              :customer-filter-text="customerFilterText"
              :bike-filter-text="bikeFilterText"
              :selected-bike-filter="selectedBikeFilter"
              :search-text="searchOverhauls"
              :customer-sort-label="sortLabel('overhaul', 'customer')"
              :date-sort-label="sortLabel('overhaul', 'date')"
              :status-sort-label="sortLabel('overhaul', 'status')"
              @update:customer-filter-text="customerFilterText = $event"
              @update:bike-filter-text="bikeFilterText = $event"
              @update:search-text="searchOverhauls = $event"
              @clear-filters="clearBikeNameFilters"
              @sort="toggleListSort('overhaul', $event)"
              @open-customer="openCustomerDetail"
              @edit="openEdit('overhaul', $event)"
              @start-work="startWork('overhaul', $event)"
              @complete="startCompletion('overhaul', $event)"
            />
          </template>

          <template v-else-if="page === 'maintenance'">
            <MaintenanceView
              :rows="maintenanceRows"
              :count="maintenanceBikeFilterCount"
              :customer-filter-text="customerFilterText"
              :bike-filter-text="bikeFilterText"
              :selected-bike-filter="selectedBikeFilter"
              :search-text="searchMaintenance"
              :customer-sort-label="sortLabel('maintenance', 'customer')"
              :date-sort-label="sortLabel('maintenance', 'date')"
              :status-sort-label="sortLabel('maintenance', 'status')"
              @update:customer-filter-text="customerFilterText = $event"
              @update:bike-filter-text="bikeFilterText = $event"
              @update:search-text="searchMaintenance = $event"
              @clear-filters="clearBikeNameFilters"
              @sort="toggleListSort('maintenance', $event)"
              @open-customer="openCustomerDetail"
              @status-change="updateMaintenanceStatus"
              @edit="openEdit('maintenance', $event)"
              @start-work="startWork('maintenance', $event)"
              @complete="startCompletion('maintenance', $event)"
            />
          </template>

          <template v-else-if="page === 'events'">
            <EventInspectionView
              :rows="eventInspectionRows"
              :count="eventInspectionCount"
              :customer-filter-text="customerFilterText"
              :bike-filter-text="bikeFilterText"
              :selected-bike-filter="selectedBikeFilter"
              :search-text="searchEvents"
              :customer-sort-label="sortLabel('events', 'customer')"
              :date-sort-label="sortLabel('events', 'date')"
              :status-sort-label="sortLabel('events', 'status')"
              @update:customer-filter-text="customerFilterText = $event"
              @update:bike-filter-text="bikeFilterText = $event"
              @update:search-text="searchEvents = $event"
              @clear-filters="clearBikeNameFilters"
              @sort="toggleListSort('events', $event)"
              @open-customer="openCustomerDetail"
              @edit="openEdit('event', $event)"
              @start-work="startWork('event', $event)"
              @complete="startCompletion('event', $event)"
            />
          </template>

          <template v-else-if="page === 'history'">
            <HistoryView
              :bikes="bikes"
              :selected-bike-id="selectedBikeId"
              :selected-bike="selectedBike"
              :rows="historyRows"
              :count="selectedBikeHistoryCount"
              :search-text="searchHistory"
              :date-sort-label="sortLabel('history', 'date')"
              @update:selected-bike-id="selectedBikeId = $event"
              @update:search-text="searchHistory = $event"
              @sort="toggleListSort('history', 'date')"
            />
          </template>

          <template v-else-if="page === 'messages'">
            <MessagesView
              :rows="messageRows"
              :pending-count="pendingCount"
              :search-text="searchMessages"
              :customer-sort-label="sortLabel('messages', 'customer')"
              :date-sort-label="sortLabel('messages', 'date')"
              @update:search-text="searchMessages = $event"
              @sort="toggleListSort('messages', $event)"
              @open-customer="openCustomerDetail"
              @compose-mail="composeMail"
            />
          </template>

          <template v-else>
            <MypageView
              :customer-name="customerPortalName"
              :bike-cards="mypageBikeCards"
              :schedule-rows="mypageScheduleRows"
              :search-text="searchMyPage"
              :date-sort-label="sortLabel('mypage', 'date')"
              :status-sort-label="sortLabel('mypage', 'status')"
              @update:search-text="searchMyPage = $event"
              @sort="toggleListSort('mypage', $event)"
            />
          </template>
        </div>
      </main>
    </div>
    <BikeDetailModal
      v-if="showBikeDetail"
      :bike="bikeDetails.bike"
      :plan="bikeDetails.plan"
      :shop-name="bikeDetails.shopName"
      :remaining="bikeDetails.remaining"
      @close="showBikeDetail = false"
    />
    <CustomerDetailModal
      v-if="showCustomerDetail"
      :customer-name="selectedCustomerName"
      :customer-email="selectedCustomer?.email ?? ''"
      :bikes="selectedCustomerBikeCards"
      :works="selectedCustomerWorks"
      :histories="selectedCustomerHistories"
      :show-identity-link="Boolean(portalProfile?.isStaffAdmin && selectedCustomerId !== null)"
      :identity-tenant-id="customerIdentityTenantId"
      :identity-object-id="customerIdentityObjectId"
      :identity-error="customerIdentityError"
      :identity-message="customerIdentityMessage"
      :identity-saving="customerIdentitySaving"
      @update:identity-tenant-id="customerIdentityTenantId = $event"
      @update:identity-object-id="customerIdentityObjectId = $event"
      @close="showCustomerDetail = false"
      @link-identity="linkSelectedCustomerIdentity"
      @register-bike="openCustomerBikeRegistration($event.kind, $event.bike)"
      @open-bike-page="openCustomerBikePage($event.target, $event.bikeId)"
    />
    <ContractRegistrationModal
      v-if="showContractModal"
      v-model:form="contractForm"
      :plans="plans"
      :shops="shops"
      :staff="staff"
      @close="showContractModal = false"
      @submit="addContract"
    />
    <MasterDataModals
      :show-shop="showShopModal"
      :show-staff="showStaffModal"
      :show-plan="showPlanModal"
      v-model:shop-name="newShopName"
      v-model:shop-address="newShopAddress"
      v-model:shop-phone="newShopPhone"
      v-model:staff-name="newStaffName"
      v-model:staff-shop="newStaffShop"
      v-model:plan="newPlan"
      :shops="shops"
      @close-shop="showShopModal = false"
      @close-staff="showStaffModal = false"
      @close-plan="showPlanModal = false"
      @add-shop="addShop"
      @add-staff="addStaff"
      @add-plan="addPlan"
    />
    <OverhaulRegistrationModal
      v-if="showOverhaulModal"
      v-model:bike-search="overhaulBikeSearch"
      v-model:reception-date="newOverhaulReceptionDate"
      v-model:start-date="newOverhaulDate"
      v-model:end-date="newOverhaulEndDate"
      v-model:title="newOverhaulTitle"
      v-model:detail="newOverhaulDetail"
      :bike-selected="Boolean(overhaulScheduleCapacity.bike)"
      :available-count="overhaulScheduleCapacity.available"
      :contract-limit="overhaulScheduleCapacity.limit"
      @bike-change="setBikeFromLabel($event, 'overhaul')"
      @close="showOverhaulModal = false"
      @submit="addOverhaul"
    />
    <MaintenanceRegistrationModal
      v-if="showMaintenanceModal"
      v-model:bike-search="maintenanceBikeSearch"
      v-model:start-date="newWorkDate"
      v-model:end-date="newWorkEndDate"
      v-model:title="newWorkTitle"
      v-model:detail="newWorkDetail"
      :bike-selected="Boolean(maintenanceScheduleCapacity.bike)"
      :available-count="maintenanceScheduleCapacity.available"
      :contract-limit="maintenanceScheduleCapacity.limit"
      @bike-change="setBikeFromLabel($event, 'maintenance')"
      @close="showMaintenanceModal = false"
      @submit="addMaintenance"
    />
    <EventInspectionRegistrationModal
      v-if="showEventModal"
      v-model:bike-search="eventBikeSearch"
      v-model:event-name="newEventName"
      v-model:event-date="newEventDate"
      v-model:inspection-start-date="newInspectionDate"
      v-model:inspection-end-date="newInspectionEndDate"
      :bike-selected="Boolean(eventScheduleCapacity.bike)"
      :available-count="eventScheduleCapacity.available"
      :contract-limit="eventScheduleCapacity.limit"
      @bike-change="setBikeFromLabel($event, 'event')"
      @close="showEventModal = false"
      @submit="addEvent"
    />
    <WorkCompletionModal
      v-if="completionTarget"
      v-model:date="completionDate"
      v-model:memo="completionMemo"
      @close="completionTarget = null"
      @submit="confirmCompletion"
    />
    <WorkEditModal
      v-if="editTarget"
      v-model:start-date="editDate"
      v-model:end-date="editEndDate"
      v-model:title="editTitle"
      v-model:detail="editDetail"
      v-model:receptionist="editReceptionist"
      :staff="staff"
      @close="editTarget = null"
      @submit="saveEdit"
    />
    <EmailPreviewModal
      v-if="showMail"
      :customers="customers"
      :target="mailTarget"
      :subject="mailKind"
      @update:target="mailTarget = $event"
      @update:subject="mailKind = $event"
      @close="showMail = false"
      @send="sendMail"
    />
    <datalist id="customer-options">
      <option v-for="c in customers" :key="c.name" :value="c.name" /></datalist
    ><datalist id="bike-options">
      <option v-for="b in bikes" :key="b.id" :value="bikeLabel(b.id)" />
    </datalist>
  </template>
</template>
