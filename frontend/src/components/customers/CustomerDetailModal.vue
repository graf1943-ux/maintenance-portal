<script setup lang="ts">
  import type { Bike, Plan } from '../../types';
  import type { CustomerProfileForm } from '../../composables/useCustomerProfileEditor';
  import type { CustomerWorkRow } from '../../composables/useCustomerDetails';

  type CustomerBikeCard = {
    bike: Bike;
    plan: Plan;
    shopName: string;
    remaining: { overhaul: number; maintenance: number; inspection: number };
    scheduleRemaining: { overhaul: number; maintenance: number; inspection: number };
  };

  defineProps<{
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
    showIdentityLink: boolean;
    identityTenantId: string;
    identityObjectId: string;
    identityError: string;
    identityMessage: string;
    identitySaving: boolean;
  }>();

  const profile = defineModel<CustomerProfileForm>('customer-profile', { required: true });

  defineEmits<{
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
      <div class="modal-actions">
        <button class="outline" @click="$emit('close')">閉じる</button>
      </div>
    </div>
  </div>
</template>
