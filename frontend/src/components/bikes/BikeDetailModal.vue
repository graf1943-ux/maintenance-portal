<script setup lang="ts">
  import type { Bike, Plan } from '../../types';
  import { dateSlash, type ApiBikeComponent } from '../../services/apiMappers';
  import type { BikeComponentForm } from '../../composables/useBikeComponents';
  import type { BikeProfileForm } from '../../composables/useBikeProfileEditor';

  defineProps<{
    bike: Bike;
    plan: Plan;
    shopName: string;
    remaining: { overhaul: number; maintenance: number; inspection: number };
    profileEditing: boolean;
    profileSaving: boolean;
    profileError: string;
    components: ApiBikeComponent[];
    componentsLoading: boolean;
    componentSaving: boolean;
    componentError: string;
  }>();

  const profile = defineModel<BikeProfileForm>('profile', { required: true });
  const componentForm = defineModel<BikeComponentForm>('component-form', { required: true });

  defineEmits<{
    close: [];
    'edit-profile': [];
    'cancel-profile-edit': [];
    'save-profile': [];
    'add-component': [];
  }>();
</script>

<template>
  <div class="modal-backdrop bike-detail-backdrop" @click.self="$emit('close')">
    <div class="modal modal-wide bike-detail-modal">
      <div class="modal-head">
        <div>
          <p class="eyebrow">BIKE PROFILE</p>
          <h2>自転車詳細</h2>
          <p class="sub">自転車情報・コンポーネント履歴・契約内容</p>
        </div>
        <button class="close" @click="$emit('close')">×</button>
      </div>
      <section class="bike-profile-section">
        <div class="panel-head">
          <div><h3>自転車情報</h3></div>
          <button v-if="!profileEditing" class="outline small" @click="$emit('edit-profile')">
            自転車情報を編集
          </button>
        </div>
        <div v-if="profileEditing" class="detail-grid">
          <div><label>メーカー</label><input v-model="profile.maker" /></div>
          <div><label>モデル</label><input v-model="profile.model" /></div>
          <div><label>年式</label><input v-model.number="profile.year" type="number" /></div>
          <div><label>車体番号</label><input v-model="profile.serial" /></div>
          <div><label>購入日</label><input v-model="profile.purchaseDate" type="date" /></div>
        </div>
        <div v-else class="detail-grid">
          <div>
            <label>メーカー・モデル</label>
            <b>{{ bike.maker }} {{ bike.model }}</b>
          </div>
          <div>
            <label>年式</label><b>{{ bike.year }}年</b>
          </div>
          <div>
            <label>車体番号</label><b>{{ bike.serial || '未登録' }}</b>
          </div>
          <div>
            <label>購入日</label><b>{{ bike.purchaseDate || '未登録' }}</b>
          </div>
        </div>
        <p v-if="profileError" class="customer-identity-feedback error">{{ profileError }}</p>
        <div v-if="profileEditing" class="customer-profile-edit-actions">
          <button
            class="outline small"
            :disabled="profileSaving"
            @click="$emit('cancel-profile-edit')"
          >
            キャンセル
          </button>
          <button class="primary small" :disabled="profileSaving" @click="$emit('save-profile')">
            {{ profileSaving ? '保存中…' : '自転車情報を保存' }}
          </button>
        </div>
      </section>
      <section class="bike-profile-section">
        <div class="panel-head">
          <div>
            <h3>使用コンポーネント</h3>
            <p>同じ区分で登録すると、前回のコンポーネントは交換履歴になります。</p>
          </div>
        </div>
        <div class="detail-grid bike-component-form">
          <div>
            <label>区分</label>
            <input v-model="componentForm.category" placeholder="例：ドライブトレイン" />
          </div>
          <div><label>メーカー</label><input v-model="componentForm.manufacturer" /></div>
          <div><label>モデル</label><input v-model="componentForm.model" /></div>
          <div>
            <label>取付日</label><input v-model="componentForm.installedDate" type="date" />
          </div>
          <div class="bike-component-add">
            <button
              class="primary small"
              :disabled="componentSaving"
              @click="$emit('add-component')"
            >
              {{ componentSaving ? '登録中…' : 'コンポーネントを記録' }}
            </button>
          </div>
        </div>
        <p v-if="componentError" class="customer-identity-feedback error">{{ componentError }}</p>
        <p v-if="componentsLoading" class="muted">履歴を読み込み中です。</p>
        <div v-else-if="components.length" class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>区分</th>
                <th>メーカー・モデル</th>
                <th>取付日</th>
                <th>取外日</th>
                <th>状態</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="component in components" :key="component.id">
                <td>{{ component.category }}</td>
                <td>{{ component.manufacturer }} {{ component.model }}</td>
                <td>{{ dateSlash(component.installedDate) }}</td>
                <td>{{ dateSlash(component.removedDate) || '—' }}</td>
                <td>
                  <span :class="['status', component.removedDate ? 'info' : 'success']">
                    {{ component.removedDate ? '交換済み' : '使用中' }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-else class="empty-state">コンポーネントはまだ登録されていません。</div>
      </section>
      <section class="bike-profile-section">
        <div class="panel-head">
          <div><h3>契約内容</h3></div>
        </div>
        <div class="detail-grid">
          <div>
            <label>顧客</label><b>{{ bike.customer }}</b>
          </div>
          <div>
            <label>自転車</label><b>{{ bike.maker }} {{ bike.model }}</b>
          </div>
          <div>
            <label>プラン</label><b>{{ plan.name }}</b>
          </div>
          <div>
            <label>契約期間</label><b>{{ bike.contractStart }}〜{{ bike.contractEnd }}</b>
          </div>
          <div>
            <label>担当店舗・受付</label><b>{{ shopName }} / {{ bike.receptionist }}</b>
          </div>
          <div>
            <label>オーバーホール残</label>
            <b class="remaining large">{{ remaining.overhaul }} / {{ plan.overhaulCount }}回</b>
          </div>
          <div>
            <label>メンテナンス残</label>
            <b class="remaining large">
              {{ remaining.maintenance }} / {{ plan.maintenanceCount }}回
            </b>
          </div>
          <div>
            <label>点検サポート残</label>
            <b class="remaining large">{{ remaining.inspection }} / {{ plan.inspectionCount }}回</b>
          </div>
          <div>
            <label>オーバーホール完了日</label><b>{{ bike.overhaulCompleted || '未実施' }}</b>
          </div>
        </div>
      </section>
      <div class="modal-actions">
        <button class="outline" @click="$emit('close')">閉じる</button>
      </div>
    </div>
  </div>
</template>
