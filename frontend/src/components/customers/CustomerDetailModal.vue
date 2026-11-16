<script setup lang="ts">
  import { ref, watch } from 'vue';
  import QRCode from 'qrcode';
  import { apiGet, apiPost, apiPut } from '../../api';
  import type { Bike, Plan } from '../../types';
  import type { ApiCustomerContact, ApiStaff } from '../../services/apiMappers';
  import type { CustomerProfileForm } from '../../composables/useCustomerProfileEditor';
  import type { CustomerWorkRow } from '../../composables/useCustomerDetails';

  type CustomerBikeCard = {
    bike: Bike;
    plan: Plan;
    shopName: string;
    remaining: { overhaul: number; maintenance: number; inspection: number };
    scheduleRemaining: { overhaul: number; maintenance: number; inspection: number };
  };

  const props = defineProps<{
    customerId: number | null;
    customerName: string;
    customerEmail: string;
    customerPhone: string;
    customerProfileEditing: boolean;
    customerProfileSaving: boolean;
    customerProfileError: string;
    customerProfileMessage: string;
    bikes: CustomerBikeCard[];
    works: CustomerWorkRow[];
    staff: ApiStaff[];
    useMockData: boolean;
    mockContacts: ApiCustomerContact[];
    showIdentityLink: boolean;
    identityTenantId: string;
    identityObjectId: string;
    identityError: string;
    identityMessage: string;
    identitySaving: boolean;
    canIssueQr: boolean;
  }>();
  const qrImage = ref('');
  const qrExpires = ref('');
  const qrError = ref('');
  const qrLoading = ref(false);
  const contacts = ref<ApiCustomerContact[]>([]);
  const contactsLoading = ref(false);
  const contactSaving = ref(false);
  const contactError = ref('');
  const contactMessage = ref('');
  const followUpResults = ref<Record<number, string>>({});
  const updatingFollowUpId = ref<number | null>(null);
  const contactForm = ref({
    channel: '電話',
    summary: '',
    followUpDate: '',
    followUpStaffId: '',
  });

  async function loadContacts(customerId: number | null) {
    contactError.value = '';
    contactMessage.value = '';
    followUpResults.value = {};
    if (customerId === null) {
      contacts.value = [];
      return;
    }

    if (props.useMockData) {
      contacts.value = props.mockContacts
        .filter((contact) => contact.customerId === customerId)
        .map((contact) => ({ ...contact }));
      return;
    }

    contactsLoading.value = true;
    try {
      contacts.value = await apiGet<ApiCustomerContact[]>(`/customers/${customerId}/contacts`);
      followUpResults.value = Object.fromEntries(
        contacts.value.map((contact) => [contact.id, contact.followUpResult ?? ''])
      );
    } catch (error) {
      contactError.value =
        error instanceof Error ? error.message : '連絡履歴を読み込めませんでした。';
    } finally {
      contactsLoading.value = false;
    }
  }

  watch(
    () => props.customerId,
    (customerId) => void loadContacts(customerId),
    { immediate: true }
  );

  async function saveContact() {
    const customerId = props.customerId;
    const summary = contactForm.value.summary.trim();
    contactError.value = '';
    contactMessage.value = '';
    if (customerId === null) {
      contactError.value = '顧客を選択してください。';
      return;
    }
    if (!summary) {
      contactError.value = '連絡内容を入力してください。';
      return;
    }

    contactSaving.value = true;
    try {
      const request = {
        channel: contactForm.value.channel,
        summary,
        followUpDate: contactForm.value.followUpDate || null,
        followUpStaffId: contactForm.value.followUpDate
          ? Number(contactForm.value.followUpStaffId) || null
          : null,
      };
      if (props.useMockData) {
        const staffMember = props.staff.find((member) => member.id === request.followUpStaffId);
        contacts.value.unshift({
          id: Date.now(),
          customerId,
          ...request,
          contactedAtUtc: new Date().toISOString(),
          createdBy: 'スタッフ',
          followUpStaffName: staffMember?.name ?? null,
          followUpStatus: request.followUpDate ? '未対応' : '不要',
          followUpResult: null,
        });
      } else {
        await apiPost<ApiCustomerContact>(`/customers/${customerId}/contacts`, request);
        await loadContacts(customerId);
      }
      contactForm.value.summary = '';
      contactForm.value.followUpDate = '';
      contactForm.value.followUpStaffId = '';
      contactMessage.value = '連絡履歴を登録しました。';
      emit('contacts-changed', contacts.value);
    } catch (error) {
      contactError.value =
        error instanceof Error ? error.message : '連絡履歴を登録できませんでした。';
    } finally {
      contactSaving.value = false;
    }
  }

  async function updateFollowUp(
    contact: ApiCustomerContact,
    status: ApiCustomerContact['followUpStatus']
  ) {
    if (props.customerId === null) return;
    contactError.value = '';
    contactMessage.value = '';
    updatingFollowUpId.value = contact.id;
    try {
      const result = followUpResults.value[contact.id]?.trim() ?? '';
      if (props.useMockData) {
        contacts.value = contacts.value.map((item) =>
          item.id === contact.id
            ? { ...item, followUpStatus: status, followUpResult: result || null }
            : item
        );
      } else {
        const updated = await apiPut<ApiCustomerContact>(
          `/customers/${props.customerId}/contacts/${contact.id}/follow-up`,
          { followUpStatus: status, followUpResult: result }
        );
        contacts.value = contacts.value.map((item) => (item.id === contact.id ? updated : item));
      }
      contactMessage.value = 'フォローアップを更新しました。';
      emit('contacts-changed', contacts.value);
    } catch (error) {
      contactError.value =
        error instanceof Error ? error.message : 'フォローアップを更新できませんでした。';
    } finally {
      updatingFollowUpId.value = null;
    }
  }

  function formatContactDate(value: string) {
    return new Date(value).toLocaleString('ja-JP', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  function isFollowUpOverdue(contact: ApiCustomerContact) {
    if (!contact.followUpDate || contact.followUpStatus !== '未対応') return false;
    const today = new Date();
    today.setMinutes(today.getMinutes() - today.getTimezoneOffset());
    return contact.followUpDate < today.toISOString().slice(0, 10);
  }

  async function issueQr() {
    if (!props.customerId) return;
    qrLoading.value = true;
    qrError.value = '';
    qrImage.value = '';
    try {
      const grant = await apiPost<{ code: string; expiresAtUtc: string }>('/qr-access/grants', {
        customerId: props.customerId,
      });
      const link = `${window.location.origin}${window.location.pathname}?qr=${encodeURIComponent(grant.code)}`;
      qrImage.value = await QRCode.toDataURL(link, { width: 240, margin: 2 });
      qrExpires.value = new Date(grant.expiresAtUtc).toLocaleTimeString('ja-JP', {
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch (error) {
      qrError.value = error instanceof Error ? error.message : 'QRコードを発行できませんでした。';
    } finally {
      qrLoading.value = false;
    }
  }

  const profile = defineModel<CustomerProfileForm>('customer-profile', { required: true });

  const emit = defineEmits<{
    'edit-customer': [];
    'cancel-customer-edit': [];
    'save-customer': [];
    'update:identity-tenant-id': [value: string];
    'update:identity-object-id': [value: string];
    close: [];
    'link-identity': [];
    'register-bike': [payload: { kind: 'overhaul' | 'maintenance' | 'event'; bike: Bike }];
    'open-bike-page': [
      payload: { target: 'overhaul' | 'maintenance' | 'events' | 'history'; bikeId: number },
    ];
    'open-bike-detail': [bikeId: number];
    'contacts-changed': [contacts: ApiCustomerContact[]];
  }>();
</script>

<template>
  <div class="modal-backdrop" @click.self="$emit('close')">
    <div class="modal modal-wide customer-detail-modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">CUSTOMER PROFILE</p>
          <h2>{{ customerName }} 様の顧客情報</h2>
          <p class="sub">管理画面から顧客マイページと同等の契約・予定・履歴を確認できます。</p>
        </div>
        <button class="close" @click="$emit('close')">×</button>
      </div>
      <section class="customer-profile-summary">
        <template v-if="customerProfileEditing">
          <div><label>顧客名</label><input v-model="profile.name" /></div>
          <div><label>メールアドレス</label><input v-model="profile.email" type="email" /></div>
          <div><label>電話番号</label><input v-model="profile.phone" type="tel" /></div>
        </template>
        <template v-else>
          <div>
            <label>顧客名</label><b>{{ customerName }}</b>
          </div>
          <div>
            <label>メールアドレス</label><b>{{ customerEmail || '未登録' }}</b>
          </div>
          <div>
            <label>電話番号</label><b>{{ customerPhone || '未登録' }}</b>
          </div>
        </template>
        <div>
          <label>契約自転車</label><b>{{ bikes.length }}台</b>
        </div>
      </section>
      <div v-if="customerProfileEditing" class="customer-profile-edit-actions">
        <p v-if="customerProfileError" class="customer-identity-feedback error">
          {{ customerProfileError }}
        </p>
        <p v-if="customerProfileMessage" class="customer-identity-feedback success">
          {{ customerProfileMessage }}
        </p>
        <button
          class="outline small"
          :disabled="customerProfileSaving"
          @click="$emit('cancel-customer-edit')"
        >
          キャンセル
        </button>
        <button
          class="primary small"
          :disabled="customerProfileSaving"
          @click="$emit('save-customer')"
        >
          {{ customerProfileSaving ? '保存中…' : '顧客情報を保存' }}
        </button>
      </div>
      <div v-else-if="customerId !== null" class="customer-profile-edit-actions">
        <p v-if="customerProfileMessage" class="customer-identity-feedback success">
          {{ customerProfileMessage }}
        </p>
        <button class="outline small" @click="$emit('edit-customer')">顧客情報を編集</button>
        <button v-if="canIssueQr" class="outline small" :disabled="qrLoading" @click="issueQr">
          {{ qrLoading ? '発行中…' : '一時アクセスQRを発行' }}
        </button>
      </div>
      <div v-if="qrImage || qrError" class="customer-profile-edit-actions qr-access-result">
        <img v-if="qrImage" :src="qrImage" alt="顧客一時アクセス用QRコード" />
        <p v-if="qrImage">有効期限 {{ qrExpires }} まで。読み取り後30分、閲覧のみ利用できます。</p>
        <p v-if="qrError" class="customer-identity-feedback error">{{ qrError }}</p>
      </div>
      <section v-if="showIdentityLink" class="customer-profile-section customer-identity-section">
        <div class="panel-head">
          <div>
            <h3>顧客Entra IDの紐付け</h3>
            <p>External tenantのTenant IDと、登録ユーザーのObject IDを指定します。</p>
          </div>
        </div>
        <div class="customer-identity-form">
          <label>
            Tenant ID
            <input
              :value="identityTenantId"
              autocomplete="off"
              required
              @input="$emit('update:identity-tenant-id', ($event.target as HTMLInputElement).value)"
            />
          </label>
          <label>
            Object ID
            <input
              :value="identityObjectId"
              autocomplete="off"
              required
              @input="$emit('update:identity-object-id', ($event.target as HTMLInputElement).value)"
            />
          </label>
        </div>
        <p class="customer-identity-note">
          保存すると、この顧客の既存のEntra ID紐付けが置き換わります。
        </p>
        <p v-if="identityError" class="customer-identity-feedback error">{{ identityError }}</p>
        <p v-if="identityMessage" class="customer-identity-feedback success">
          {{ identityMessage }}
        </p>
        <div class="customer-identity-actions">
          <button class="primary small" :disabled="identitySaving" @click="$emit('link-identity')">
            {{ identitySaving ? '保存中…' : 'Entra IDを紐付け' }}
          </button>
        </div>
      </section>
      <section class="customer-profile-section">
        <div class="panel-head">
          <div>
            <h3>契約中の自転車・年間プラン</h3>
            <p>自転車ごとの契約期間とサービス残回数</p>
          </div>
        </div>
        <div v-for="card in bikes" :key="card.bike.id" class="customer-profile-bike">
          <div class="customer-profile-bike-title">
            <button class="customer-link" @click="$emit('open-bike-detail', card.bike.id)">
              {{ card.bike.maker }} {{ card.bike.model }}
            </button>
            <span class="status success">契約中</span>
          </div>
          <div class="customer-bike-actions customer-bike-register-actions">
            <button
              class="primary small"
              :disabled="card.scheduleRemaining.overhaul <= 0"
              @click="$emit('register-bike', { kind: 'overhaul', bike: card.bike })"
            >
              ＋ オーバーホール登録
            </button>
            <button
              class="primary small"
              :disabled="card.scheduleRemaining.maintenance <= 0"
              @click="$emit('register-bike', { kind: 'maintenance', bike: card.bike })"
            >
              ＋ メンテナンス登録
            </button>
            <button
              class="primary small"
              :disabled="card.scheduleRemaining.inspection <= 0"
              @click="$emit('register-bike', { kind: 'event', bike: card.bike })"
            >
              ＋ 点検サポート登録
            </button>
          </div>
          <div class="customer-bike-actions customer-bike-manage-actions">
            <button
              class="outline small"
              @click="$emit('open-bike-page', { target: 'overhaul', bikeId: card.bike.id })"
            >
              オーバーホール管理画面
            </button>
            <button
              class="outline small"
              @click="$emit('open-bike-page', { target: 'maintenance', bikeId: card.bike.id })"
            >
              メンテナンス管理画面
            </button>
            <button
              class="outline small"
              @click="$emit('open-bike-page', { target: 'events', bikeId: card.bike.id })"
            >
              点検サポート管理画面
            </button>
            <button
              class="outline small"
              @click="$emit('open-bike-page', { target: 'history', bikeId: card.bike.id })"
            >
              作業履歴
            </button>
          </div>
          <div class="detail-grid">
            <div>
              <label>プラン</label>
              <b>{{ card.plan.name }}</b>
            </div>
            <div>
              <label>契約期間</label>
              <b>{{ card.bike.contractStart }}〜{{ card.bike.contractEnd }}</b>
            </div>
            <div>
              <label>担当店舗・受付担当者</label>
              <b>{{ card.shopName }} / {{ card.bike.receptionist }}</b>
            </div>
            <div>
              <label>オーバーホール残</label>
              <b class="remaining"
                >{{ card.remaining.overhaul }} / {{ card.plan.overhaulCount }}回</b
              >
            </div>
            <div>
              <label>メンテナンス残</label>
              <b class="remaining">
                {{ card.remaining.maintenance }} / {{ card.plan.maintenanceCount }}回
              </b>
            </div>
            <div>
              <label>点検サポート残</label>
              <b class="remaining">
                {{ card.remaining.inspection }} / {{ card.plan.inspectionCount }}回
              </b>
            </div>
          </div>
        </div>
        <div v-if="!bikes.length" class="empty-state">契約中の自転車はありません。</div>
      </section>
      <section class="customer-profile-section">
        <div class="panel-head">
          <div>
            <h3>オーバーホール、メンテナンス・点検サポート予定</h3>
            <p>対象自転車・作業期間・状態</p>
          </div>
        </div>
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>対象自転車</th>
                <th>作業区分</th>
                <th>内容</th>
                <th>作業期間</th>
                <th>状態</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="work in works" :key="work.kind + '-' + work.id">
                <td>
                  <button
                    v-if="bikes.find((card) => card.bike.id === work.bikeId)"
                    class="customer-link"
                    @click="$emit('open-bike-detail', work.bikeId)"
                  >
                    {{ bikes.find((card) => card.bike.id === work.bikeId)?.bike.maker }}
                    {{ bikes.find((card) => card.bike.id === work.bikeId)?.bike.model }}
                  </button>
                  <span v-else>—</span>
                </td>
                <td>{{ work.kind }}</td>
                <td>
                  {{ work.title }}
                  <span v-if="work.isAdHoc" class="status warning">突発作業</span>
                </td>
                <td>{{ work.date }}〜{{ work.endDate || '未定' }}</td>
                <td>
                  <span class="status info">{{ work.status }}</span>
                </td>
              </tr>
              <tr v-if="!works.length">
                <td colspan="5" class="empty-state">予定は登録されていません。</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
      <section class="customer-profile-section customer-contact-section">
        <div class="panel-head">
          <div>
            <h3>連絡履歴・フォローアップ</h3>
            <p>連絡内容を記録し、次回の連絡予定と対応結果を管理します。</p>
          </div>
          <span class="status warning">
            未対応 {{ contacts.filter((contact) => contact.followUpStatus === '未対応').length }}件
          </span>
        </div>
        <form class="customer-contact-form" @submit.prevent="saveContact">
          <label>
            連絡方法
            <select v-model="contactForm.channel">
              <option>電話</option>
              <option>来店</option>
              <option>メール</option>
              <option>その他</option>
            </select>
          </label>
          <label class="customer-contact-summary">
            連絡内容
            <textarea
              v-model="contactForm.summary"
              maxlength="1000"
              rows="2"
              placeholder="相談内容や対応内容を入力"
              required
            />
          </label>
          <label>
            フォローアップ日（任意）
            <input v-model="contactForm.followUpDate" type="date" />
          </label>
          <label>
            担当者（任意）
            <select v-model="contactForm.followUpStaffId" :disabled="!contactForm.followUpDate">
              <option value="">未指定</option>
              <option v-for="member in staff" :key="member.id" :value="String(member.id)">
                {{ member.name }}
              </option>
            </select>
          </label>
          <button class="primary small" type="submit" :disabled="contactSaving">
            {{ contactSaving ? '登録中…' : '連絡を記録' }}
          </button>
        </form>
        <p v-if="contactError" class="customer-identity-feedback error">{{ contactError }}</p>
        <p v-if="contactMessage" class="customer-identity-feedback success">{{ contactMessage }}</p>
        <p v-if="contactsLoading" class="empty-state">連絡履歴を読み込み中です…</p>
        <div v-else-if="contacts.length" class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>日時・方法</th>
                <th>連絡内容</th>
                <th>次回連絡</th>
                <th>担当・状態</th>
                <th>対応結果</th>
                <th>更新</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="contact in contacts" :key="contact.id">
                <td>
                  {{ formatContactDate(contact.contactedAtUtc) }}
                  <span class="customer-contact-channel">{{ contact.channel }}</span>
                </td>
                <td>{{ contact.summary }}</td>
                <td>
                  <template v-if="contact.followUpDate">
                    {{ contact.followUpDate.replace(/-/g, '/') }}
                    <span v-if="isFollowUpOverdue(contact)" class="status danger">期限超過</span>
                  </template>
                  <span v-else>—</span>
                </td>
                <td>
                  <template v-if="contact.followUpDate">
                    <span>{{ contact.followUpStaffName || '未指定' }}</span>
                    <span
                      class="status"
                      :class="contact.followUpStatus === '未対応' ? 'warning' : 'success'"
                    >
                      {{ contact.followUpStatus }}
                    </span>
                  </template>
                  <span v-else>—</span>
                </td>
                <td>
                  <input
                    v-if="contact.followUpDate"
                    v-model="followUpResults[contact.id]"
                    class="customer-contact-result"
                    maxlength="1000"
                    placeholder="結果を記録"
                  />
                  <span v-else>—</span>
                </td>
                <td>
                  <div v-if="contact.followUpDate" class="customer-contact-actions">
                    <button
                      class="outline small"
                      :disabled="updatingFollowUpId === contact.id"
                      @click="
                        updateFollowUp(
                          contact,
                          contact.followUpStatus === '完了' ? '未対応' : '完了'
                        )
                      "
                    >
                      {{ contact.followUpStatus === '完了' ? '未対応に戻す' : '完了' }}
                    </button>
                    <button
                      v-if="contact.followUpStatus === '未対応'"
                      class="outline small"
                      :disabled="updatingFollowUpId === contact.id"
                      @click="updateFollowUp(contact, '不要')"
                    >
                      不要
                    </button>
                  </div>
                  <span v-else>—</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-else class="empty-state">連絡履歴はまだありません。</p>
      </section>
      <div class="modal-actions">
        <button class="outline" @click="$emit('close')">閉じる</button>
      </div>
    </div>
  </div>
</template>
