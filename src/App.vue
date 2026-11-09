<script setup lang="ts">
import { computed, ref } from 'vue'

type Page = 'dashboard'|'customers'|'maintenance'|'events'|'messages'|'mypage'
type Schedule = { id:number; customer:string; equipment:string; date:string; status:'案内待ち'|'案内済み'|'予約済み'|'完了'; contractId:number }
const page = ref<Page>('dashboard')
const role = ref<'staff'|'customer'>('staff')
const selectedCustomer = ref('山田 太郎')
const showMail = ref(false)
const mailTarget = ref('山田 太郎')
const notice = ref('')
const customers = ref([
  {name:'山田 太郎',email:'t.yamada@example.com',phone:'090-1234-5678',equipment:'業務用コーヒーマシン A-100',serial:'A100-2025-001',contract:'2026/04/01〜2027/03/31',total:6,used:2},
  {name:'佐藤 花子',email:'h.sato@example.com',phone:'080-2345-6789',equipment:'エスプレッソマシン E-200',serial:'E200-2025-014',contract:'2026/06/01〜2027/05/31',total:6,used:1},
  {name:'鈴木 一郎',email:'i.suzuki@example.com',phone:'070-3456-7890',equipment:'業務用グラインダー G-50',serial:'G50-2024-022',contract:'2026/01/01〜2026/12/31',total:4,used:3}
])
const schedules = ref<Schedule[]>([
  {id:1,customer:'山田 太郎',equipment:'業務用コーヒーマシン A-100',date:'2026/11/15',status:'案内待ち',contractId:1},
  {id:2,customer:'佐藤 花子',equipment:'エスプレッソマシン E-200',date:'2026/11/18',status:'案内済み',contractId:2},
  {id:3,customer:'鈴木 一郎',equipment:'業務用グラインダー G-50',date:'2026/11/22',status:'予約済み',contractId:3},
  {id:4,customer:'山田 太郎',equipment:'業務用コーヒーマシン A-100',date:'2026/09/15',status:'完了',contractId:1},
])
const events = ref([
  {id:1,customer:'山田 太郎',name:'秋の展示会',date:'2026/11/28',inspection:'2026/11/25',status:'予定'},
  {id:2,customer:'佐藤 花子',name:'冬季販売イベント',date:'2026/12/10',inspection:'2026/12/07',status:'案内済み'},
])
const currentCustomer = computed(()=>customers.value.find(c=>c.name===selectedCustomer.value) ?? customers.value[0])
const remaining = (c: typeof customers.value[number]) => Math.max(0,c.total-c.used)
const completedCount = computed(()=>schedules.value.filter(s=>s.status==='完了').length)
const pendingCount = computed(()=>schedules.value.filter(s=>s.status==='案内待ち').length)
const upcomingCount = computed(()=>schedules.value.filter(s=>s.status!=='完了').length)
function setPage(p:Page){page.value=p;notice.value=''}
function complete(s:Schedule){
  if(s.status==='完了') return
  const c=customers.value.find(x=>x.name===s.customer)
  if(c && remaining(c)>0){ c.used++; s.status='完了'; notice.value='実施完了として登録しました。契約残回数を1回消費しました。' }
  else notice.value='契約残回数がないため完了登録できません。'
}
function changeStatus(s:Schedule,status:Schedule['status']){s.status=status}
function sendMail(){showMail.value=false;notice.value=`${mailTarget.value} 様へのメール送信をデモ実行しました（実際には送信されません）。`}
</script>

<template>
<div class="app-shell">
  <aside class="sidebar">
    <div class="brand"><div class="brand-mark">M</div><div><strong>MAINTENANCE</strong><small>管理ポータル</small></div></div>
    <div class="side-label">管理メニュー</div>
    <button v-for="item in [
      {id:'dashboard',label:'ダッシュボード',icon:'▦'},
      {id:'customers',label:'顧客・機材管理',icon:'♙'},
      {id:'maintenance',label:'定期メンテナンス',icon:'⚙'},
      {id:'events',label:'イベント前点検',icon:'◇'},
      {id:'messages',label:'連絡・メール管理',icon:'✉'}
    ]" :key="item.id" class="nav-item" :class="{active:page===item.id}" @click="setPage(item.id as Page)">
      <span class="nav-icon">{{item.icon}}</span>{{item.label}}
    </button>
    <div class="sidebar-bottom">
      <div class="side-label">表示モード</div>
      <button class="mode-switch" @click="role=role==='staff'?'customer':'staff';page=role==='customer'?'mypage':'dashboard'">
        <span>{{role==='staff'?'店舗スタッフ':'顧客マイページ'}}</span><span>切替 ↗</span>
      </button>
      <div class="user"><div class="avatar">店</div><div><b>{{role==='staff'?'店舗管理者':'山田 太郎'}}</b><small>{{role==='staff'?'スタッフアカウント':'顧客アカウント'}}</small></div></div>
    </div>
  </aside>

  <main class="main">
    <header class="topbar">
      <div class="breadcrumb">メンテナンス管理 <span>/</span> {{page==='dashboard'?'ダッシュボード':page==='customers'?'顧客・機材管理':page==='maintenance'?'定期メンテナンス':page==='events'?'イベント前点検':page==='messages'?'連絡・メール管理':'マイページ'}}</div>
      <div class="top-actions"><span class="today">2026年11月9日（月）</span><button class="icon-btn" title="通知">♧<i></i></button><div class="avatar">店</div></div>
    </header>
    <div class="content">
      <div v-if="notice" class="notice"><span>{{notice}}</span><button @click="notice=''">×</button></div>

      <template v-if="page==='dashboard'">
        <div class="page-heading"><div><p class="eyebrow">OVERVIEW</p><h1>ダッシュボード</h1><p class="sub">メンテナンス契約と点検状況の概要です。</p></div><button class="primary" @click="setPage('maintenance')">＋ 点検予定を確認</button></div>
        <div class="stats">
          <div class="stat-card"><div class="stat-top"><span>登録顧客数</span><span class="stat-icon blue">♙</span></div><strong>{{customers.length}}</strong><small>顧客</small></div>
          <div class="stat-card"><div class="stat-top"><span>今月の点検予定</span><span class="stat-icon purple">⚙</span></div><strong>{{upcomingCount}}</strong><small>件（未完了）</small></div>
          <div class="stat-card"><div class="stat-top"><span>案内待ち</span><span class="stat-icon orange">✉</span></div><strong>{{pendingCount}}</strong><small>件</small></div>
          <div class="stat-card"><div class="stat-top"><span>点検完了</span><span class="stat-icon green">✓</span></div><strong>{{completedCount}}</strong><small>件（登録済み）</small></div>
        </div>
        <div class="section-grid">
          <section class="panel"><div class="panel-head"><div><h2>直近の定期メンテナンス</h2><p>予定日が近い点検</p></div><button class="text-btn" @click="setPage('maintenance')">一覧を見る →</button></div>
            <div class="table-wrap"><table><thead><tr><th>顧客</th><th>機材</th><th>予定日</th><th>状態</th></tr></thead><tbody><tr v-for="s in schedules.filter(x=>x.status!=='完了').slice(0,3)" :key="s.id"><td><b>{{s.customer}}</b></td><td>{{s.equipment}}</td><td>{{s.date}}</td><td><span class="status" :class="s.status==='案内待ち'?'warning':s.status==='予約済み'?'success':'info'">{{s.status}}</span></td></tr></tbody></table></div>
          </section>
          <section class="panel"><div class="panel-head"><div><h2>イベント前点検</h2><p>登録されているイベント</p></div><button class="text-btn" @click="setPage('events')">一覧を見る →</button></div>
            <div class="event-mini" v-for="e in events" :key="e.id"><div class="event-date"><b>{{e.inspection.slice(5,7)}}</b><small>{{e.inspection.slice(8,10)}}</small></div><div class="event-info"><b>{{e.name}}</b><small>{{e.customer}} ・ 点検 {{e.inspection}}</small></div><span class="status info">{{e.status}}</span></div>
            <div class="hint">※ イベント前点検は定期契約の回数に含まれません。</div>
          </section>
        </div>
        <section class="panel customer-summary"><div class="panel-head"><div><h2>契約状況</h2><p>顧客ごとの契約回数</p></div><button class="text-btn" @click="setPage('customers')">顧客一覧 →</button></div>
          <div class="contract-row" v-for="c in customers" :key="c.name"><div class="contract-customer"><div class="avatar pale">{{c.name.slice(0,1)}}</div><div><b>{{c.name}}</b><small>{{c.equipment}}</small></div></div><div class="contract-progress"><div class="progress-label"><span>利用 {{c.used}} / {{c.total}} 回</span><b>残り {{remaining(c)}} 回</b></div><div class="progress"><i :style="{width:(c.used/c.total*100)+'%'}"></i></div></div><span class="contract-period">{{c.contract}}</span></div>
        </section>
      </template>

      <template v-else-if="page==='customers'">
        <div class="page-heading"><div><p class="eyebrow">CUSTOMERS & EQUIPMENT</p><h1>顧客・機材管理</h1><p class="sub">顧客情報、購入機材、年間メンテナンス契約を管理します。</p></div><button class="primary" @click="notice='新規顧客登録はモックのため未実装です。'">＋ 顧客を登録</button></div>
        <section class="panel"><div class="panel-head"><div><h2>顧客一覧</h2><p>{{customers.length}} 件の顧客が登録されています</p></div><input class="search" placeholder="顧客名・機材名で検索"></div>
          <div class="table-wrap"><table><thead><tr><th>顧客名</th><th>連絡先</th><th>購入機材</th><th>契約期間</th><th>残回数</th><th></th></tr></thead><tbody><tr v-for="c in customers" :key="c.name"><td><b>{{c.name}}</b><small class="cell-sub">{{c.email}}</small></td><td>{{c.phone}}</td><td>{{c.equipment}}<small class="cell-sub">S/N: {{c.serial}}</small></td><td>{{c.contract}}</td><td><b class="remaining">{{remaining(c)}} 回</b><small class="cell-sub">契約 {{c.total}} 回</small></td><td><button class="outline small" @click="selectedCustomer=c.name;notice=`${c.name} の詳細を選択しました。`">詳細</button></td></tr></tbody></table></div>
        </section>
        <section class="panel detail-panel"><div class="panel-head"><div><h2>契約・機材詳細</h2><p>選択中：{{selectedCustomer}}</p></div><select v-model="selectedCustomer"><option v-for="c in customers" :key="c.name">{{c.name}}</option></select></div>
          <div class="detail-grid"><div><label>対象機材</label><b>{{currentCustomer.equipment}}</b></div><div><label>製造番号</label><b>{{currentCustomer.serial}}</b></div><div><label>契約期間</label><b>{{currentCustomer.contract}}</b></div><div><label>契約回数</label><b>{{currentCustomer.total}} 回</b></div><div><label>実施済み</label><b>{{currentCustomer.used}} 回</b></div><div><label>残回数</label><b class="remaining large">{{remaining(currentCustomer)}} 回</b></div></div>
        </section>
      </template>

      <template v-else-if="page==='maintenance'">
        <div class="page-heading"><div><p class="eyebrow">REGULAR MAINTENANCE</p><h1>定期メンテナンス</h1><p class="sub">契約に基づく定期点検の予定・案内・実施状況を管理します。</p></div><button class="primary" @click="notice='新規予定登録画面はモックのため未実装です。'">＋ 点検予定を登録</button></div>
        <div class="filter-row"><div class="filter-tabs"><button class="selected">すべて</button><button>案内待ち</button><button>案内済み</button><button>予約済み</button><button>完了</button></div><span class="muted">基本サイクル：2か月に1回</span></div>
        <section class="panel"><div class="panel-head"><div><h2>点検スケジュール</h2><p>契約ごとの点検予定と実施履歴</p></div><span class="count">{{schedules.length}} 件</span></div>
          <div class="table-wrap"><table><thead><tr><th>予定日</th><th>顧客</th><th>対象機材</th><th>案内・予約状況</th><th>操作</th></tr></thead><tbody><tr v-for="s in schedules" :key="s.id"><td><b>{{s.date}}</b></td><td>{{s.customer}}</td><td>{{s.equipment}}</td><td><select class="status-select" :value="s.status" @change="changeStatus(s,($event.target as HTMLSelectElement).value as Schedule['status'])"><option>案内待ち</option><option>案内済み</option><option>予約済み</option><option>完了</option></select></td><td><button v-if="s.status!=='完了'" class="outline small" @click="complete(s)">実施完了</button><span v-else class="done">✓ 完了</span></td></tr></tbody></table></div>
          <div class="panel-foot">※ 実施完了の登録時のみ、該当契約の残回数を1回消費します。予定作成・案内・予約では消費しません。</div>
        </section>
      </template>

      <template v-else-if="page==='events'">
        <div class="page-heading"><div><p class="eyebrow">EVENT INSPECTIONS</p><h1>イベント前点検</h1><p class="sub">顧客ごとのイベント日程に合わせて点検を管理します。</p></div><button class="primary" @click="notice='イベント登録画面はモックのため未実装です。'">＋ イベントを登録</button></div>
        <div class="callout"><b>定期メンテナンスとは別管理です</b><p>イベント前点検は定期契約の利用回数・残回数に影響しません。店舗スタッフがイベント情報と点検予定を登録します。</p></div>
        <section class="panel"><div class="panel-head"><div><h2>イベント・点検予定一覧</h2><p>イベント日と点検日を確認できます</p></div><span class="count">{{events.length}} 件</span></div>
          <div class="table-wrap"><table><thead><tr><th>イベント名</th><th>顧客</th><th>イベント日</th><th>点検予定日</th><th>状態</th><th></th></tr></thead><tbody><tr v-for="e in events" :key="e.id"><td><b>{{e.name}}</b></td><td>{{e.customer}}</td><td>{{e.date}}</td><td>{{e.inspection}}</td><td><span class="status info">{{e.status}}</span></td><td><button class="outline small" @click="e.status=e.status==='完了'?'予定':'完了'">状態変更</button></td></tr></tbody></table></div>
        </section>
      </template>

      <template v-else-if="page==='messages'">
        <div class="page-heading"><div><p class="eyebrow">COMMUNICATION</p><h1>連絡・メール管理</h1><p class="sub">点検案内メールの対象確認と送信履歴を管理します。</p></div><button class="primary" @click="showMail=true">✉ メールを作成</button></div>
        <section class="panel"><div class="panel-head"><div><h2>案内対象</h2><p>定期メンテナンスの案内待ち</p></div><span class="status warning">{{pendingCount}} 件 案内待ち</span></div>
          <div class="table-wrap"><table><thead><tr><th>顧客</th><th>メールアドレス</th><th>対象機材</th><th>点検予定日</th><th></th></tr></thead><tbody><tr v-for="s in schedules.filter(x=>x.status==='案内待ち')" :key="s.id"><td><b>{{s.customer}}</b></td><td>{{customers.find(c=>c.name===s.customer)?.email}}</td><td>{{s.equipment}}</td><td>{{s.date}}</td><td><button class="outline small" @click="mailTarget=s.customer;showMail=true">内容確認</button></td></tr></tbody></table></div>
        </section>
        <section class="panel"><div class="panel-head"><div><h2>メール送信履歴</h2><p>このモックでの送信操作は実送信されません</p></div></div><div class="empty-state"><div class="empty-icon">✉</div><b>送信履歴はまだありません</b><p>メール作成から案内内容を確認できます。</p></div></section>
      </template>

      <template v-else>
        <div class="page-heading"><div><p class="eyebrow">CUSTOMER PORTAL</p><h1>マイページ</h1><p class="sub">ご契約中の機材とメンテナンス予定をご確認いただけます。</p></div><span class="customer-welcome">山田 太郎 様</span></div>
        <section class="panel"><div class="panel-head"><div><h2>ご契約中の機材</h2><p>年間メンテナンス契約</p></div><span class="status success">契約中</span></div>
          <div class="equipment-card"><div class="equipment-symbol">⚙</div><div class="equipment-details"><b>{{customers[0].equipment}}</b><small>製造番号：{{customers[0].serial}}</small><small>契約期間：{{customers[0].contract}}</small></div><div class="remaining-box"><strong>{{remaining(customers[0])}}</strong><small>残り回数 / {{customers[0].total}} 回</small></div></div>
        </section>
        <section class="panel"><div class="panel-head"><div><h2>定期メンテナンス予定</h2><p>点検日程の変更をご希望の場合は店舗までご連絡ください。</p></div></div><div class="table-wrap"><table><thead><tr><th>予定日</th><th>内容</th><th>状態</th></tr></thead><tbody><tr v-for="s in schedules.filter(x=>x.customer==='山田 太郎')" :key="s.id"><td><b>{{s.date}}</b></td><td>定期メンテナンス</td><td><span class="status" :class="s.status==='完了'?'success':'info'">{{s.status}}</span></td></tr></tbody></table></div></section>
        <section class="panel"><div class="panel-head"><div><h2>イベント前点検</h2><p>イベントに合わせた点検予定</p></div></div><div class="event-mini" v-for="e in events.filter(x=>x.customer==='山田 太郎')" :key="e.id"><div class="event-date"><b>{{e.inspection.slice(5,7)}}</b><small>{{e.inspection.slice(8,10)}}</small></div><div class="event-info"><b>{{e.name}}</b><small>イベント日 {{e.date}} / 点検予定 {{e.inspection}}</small></div><span class="status info">{{e.status}}</span></div></section>
      </template>
    </div>
  </main>
</div>

<div v-if="showMail" class="modal-backdrop" @click.self="showMail=false"><div class="modal"><div class="modal-head"><div><p class="eyebrow">EMAIL PREVIEW</p><h2>点検案内メール</h2></div><button class="close" @click="showMail=false">×</button></div><label>送信先</label><select v-model="mailTarget"><option v-for="c in customers" :key="c.name">{{c.name}}</option></select><label>件名</label><input value="定期メンテナンスのご案内" /><label>本文</label><div class="mail-body">{{mailTarget}} 様<br><br>いつもご利用いただきありがとうございます。<br>定期メンテナンスの時期となりましたのでご案内いたします。<br><br>対象機材：{{customers.find(c=>c.name===mailTarget)?.equipment}}<br>点検予定日：2026年11月15日<br><br>日程の変更をご希望の場合は、店舗までご連絡ください。<br><br>今後ともよろしくお願いいたします。</div><div class="modal-note">※ モックのため、実際のメール送信は行われません。</div><div class="modal-actions"><button class="outline" @click="showMail=false">閉じる</button><button class="primary" @click="sendMail">送信（デモ）</button></div></div></div>
</template>