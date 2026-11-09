<script setup lang="ts">
import { computed, ref } from 'vue'

type Page = 'dashboard' | 'bikes' | 'overhaul' | 'maintenance' | 'events' | 'history' | 'messages' | 'mypage'
type Status = '予定' | '案内待ち' | '案内済み' | '予約済み' | '完了'
type Bike = { id:number; customer:string; name:string; maker:string; model:string; year:number; serial:string; plan:string; contractStart:string; contractEnd:string; regularLimit:number; regularUsed:number; overhaulId:number }
type Overhaul = { id:number; bikeId:number; plannedDate:string; completedDate:string; status:Status; memo:string }
type Schedule = { id:number; bikeId:number; date:string; status:Status; cycle:number }
type EventCheck = { id:number; bikeId:number; name:string; eventDate:string; inspectionDate:string; status:Status; memo:string }
type History = { id:number; bikeId:number; date:string; category:string; title:string; detail:string }

const page = ref<Page>('dashboard')
const role = ref<'staff'|'customer'>('staff')
const selectedBikeId = ref(1)
const notice = ref('')
const showMail = ref(false)
const mailTarget = ref('山田 太郎')
const mailKind = ref('定期メンテナンスのご案内')
const today = '2026年11月10日（火）'
const customers = [
  { name:'山田 太郎', email:'t.yamada@example.com', phone:'090-1234-5678' },
  { name:'佐藤 花子', email:'h.sato@example.com', phone:'080-2345-6789' },
  { name:'鈴木 一郎', email:'i.suzuki@example.com', phone:'070-3456-7890' },
]
const bikes = ref<Bike[]>([
  { id:1, customer:'山田 太郎', name:'ロードバイク A', maker:'SPECIALIZED', model:'Tarmac SL8 Expert', year:2025, serial:'WSBC0250A001', plan:'プレミアムプラン', contractStart:'2026/09/01', contractEnd:'2027/08/31', regularLimit:6, regularUsed:0, overhaulId:1 },
  { id:2, customer:'佐藤 花子', name:'ロードバイク B', maker:'TREK', model:'Madone SL 7', year:2024, serial:'WTU12345678', plan:'スタンダードプラン', contractStart:'2026/11/01', contractEnd:'2027/10/31', regularLimit:4, regularUsed:0, overhaulId:2 },
  { id:3, customer:'鈴木 一郎', name:'ロードバイク C', maker:'GIANT', model:'TCR Advanced Pro', year:2023, serial:'GNT2023-0088', plan:'ライトプラン', contractStart:'2026/04/01', contractEnd:'2027/03/31', regularLimit:3, regularUsed:2, overhaulId:3 },
])
const overhauls = ref<Overhaul[]>([
  { id:1, bikeId:1, plannedDate:'2026/09/15', completedDate:'2026/09/15', status:'完了', memo:'全体オーバーホール・消耗品点検' },
  { id:2, bikeId:2, plannedDate:'2026/11/20', completedDate:'', status:'予約済み', memo:'納車後初回オーバーホール' },
  { id:3, bikeId:3, plannedDate:'2026/05/10', completedDate:'2026/05/10', status:'完了', memo:'駆動系洗浄・ワイヤー交換' },
])
function addMonthsClamped(dateText:string, months:number) {
  const [y,m,d] = dateText.split('/').map(Number)
  const first = new Date(y, m - 1 + months, 1)
  const lastDay = new Date(first.getFullYear(), first.getMonth() + 1, 0).getDate()
  const day = Math.min(d, lastDay)
  return `${first.getFullYear()}/${String(first.getMonth()+1).padStart(2,'0')}/${String(day).padStart(2,'0')}`
}
const schedules = ref<Schedule[]>([
  ...Array.from({length:6},(_,i)=>({id:i+1,bikeId:1,date:addMonthsClamped('2026/09/15',(i+1)*2),status:(i===0?'予約済み':'予定') as Status,cycle:i+1})),
  { id:7, bikeId:3, date:'2026/07/10', status:'完了', cycle:1 },
  { id:8, bikeId:3, date:'2026/09/10', status:'完了', cycle:2 },
  { id:9, bikeId:3, date:'2026/11/10', status:'案内待ち', cycle:3 },
])
const eventChecks = ref<EventCheck[]>([
  { id:1, bikeId:1, name:'シーズン開幕ロードレース', eventDate:'2027/03/14', inspectionDate:'2027/03/10', status:'予定', memo:'変速・ブレーキ・ホイール確認' },
  { id:2, bikeId:1, name:'春のロングライドイベント', eventDate:'2027/04/18', inspectionDate:'2027/04/14', status:'予定', memo:'タイヤ・駆動系・ボルト確認' },
  { id:3, bikeId:2, name:'冬季サイクルイベント', eventDate:'2026/12/10', inspectionDate:'2026/12/07', status:'案内済み', memo:'安全点検' },
])
const histories = ref<History[]>([
  {id:1,bikeId:1,date:'2026/09/15',category:'オーバーホール',title:'初回オーバーホール',detail:'全体洗浄、駆動系点検、ブレーキ調整'},
  {id:2,bikeId:3,date:'2026/05/10',category:'オーバーホール',title:'オーバーホール',detail:'駆動系洗浄・ワイヤー交換'},
  {id:3,bikeId:3,date:'2026/07/10',category:'定期メンテナンス',title:'定期点検 1回目',detail:'変速・ブレーキ・タイヤ点検'},
  {id:4,bikeId:3,date:'2026/09/10',category:'定期メンテナンス',title:'定期点検 2回目',detail:'チェーン清掃・注油・各部増し締め'},
])
const selectedBike = computed(()=>bikes.value.find(b=>b.id===selectedBikeId.value) ?? bikes.value[0])
const selectedOverhaul = (bikeId:number)=>overhauls.value.find(o=>o.bikeId===bikeId)!
const bikeSchedules = (bikeId:number)=>schedules.value.filter(s=>s.bikeId===bikeId).sort((a,b)=>a.date.localeCompare(b.date))
const bikeEvents = (bikeId:number)=>eventChecks.value.filter(e=>e.bikeId===bikeId).sort((a,b)=>a.inspectionDate.localeCompare(b.inspectionDate))
const remaining = (bike:Bike)=>Math.max(0,bike.regularLimit-bike.regularUsed)
const completedCount = computed(()=>histories.value.length)
const upcomingCount = computed(()=>schedules.value.filter(s=>s.status!=='完了').length + overhauls.value.filter(o=>o.status!=='完了').length)
const pendingCount = computed(()=>schedules.value.filter(s=>s.status==='案内待ち').length)
const allUpcoming = computed(()=>[
  ...overhauls.value.filter(o=>o.status!=='完了').map(o=>({id:`o${o.id}`,date:o.plannedDate,kind:'オーバーホール',bike:bikes.value.find(b=>b.id===o.bikeId)!,status:o.status})),
  ...schedules.value.filter(s=>s.status!=='完了').map(s=>({id:`s${s.id}`,date:s.date,kind:`定期点検 ${s.cycle}回目`,bike:bikes.value.find(b=>b.id===s.bikeId)!,status:s.status})),
].sort((a,b)=>a.date.localeCompare(b.date)))
function setPage(p:Page){page.value=p;notice.value=''}
function setOverhaulComplete(o:Overhaul) {
  if(o.status==='完了') return
  o.status='完了'
  o.completedDate=o.plannedDate
  const bike=bikes.value.find(b=>b.id===o.bikeId)
  if(!bike) return
  const existing=schedules.value.some(s=>s.bikeId===bike.id)
  if(!existing) {
    for(let i=1;i<=bike.regularLimit;i++) schedules.value.push({id:Math.max(0,...schedules.value.map(s=>s.id))+1,bikeId:bike.id,date:addMonthsClamped(o.completedDate,i*2),status:'予定',cycle:i})
    notice.value=`オーバーホール完了。${bike.name}の定期メンテナンス予定を2か月後から${bike.regularLimit}回分作成しました。`
  } else notice.value='オーバーホールを完了にしました。既存の定期点検予定は維持しています。'
  histories.value.unshift({id:Math.max(0,...histories.value.map(h=>h.id))+1,bikeId:bike.id,date:o.completedDate,category:'オーバーホール',title:'オーバーホール完了',detail:o.memo || '作業完了'})
}
function completeSchedule(s:Schedule) {
  if(s.status==='完了') return
  const bike=bikes.value.find(b=>b.id===s.bikeId)
  if(!bike || remaining(bike)<=0){notice.value='定期メンテナンスの契約回数がありません。';return}
  s.status='完了'; bike.regularUsed++
  histories.value.unshift({id:Math.max(0,...histories.value.map(h=>h.id))+1,bikeId:bike.id,date:s.date,category:'定期メンテナンス',title:`定期点検 ${s.cycle}回目`,detail:'変速・ブレーキ・タイヤ・駆動系の点検'})
  notice.value='定期メンテナンスを完了にしました。契約回数を1回消費しました。'
}
function completeEvent(e:EventCheck){if(e.status!=='完了'){e.status='完了';const b=bikes.value.find(x=>x.id===e.bikeId)!;histories.value.unshift({id:Math.max(0,...histories.value.map(h=>h.id))+1,bikeId:b.id,date:e.inspectionDate,category:'イベント前点検',title:e.name,detail:e.memo});notice.value='イベント前点検を完了にしました。定期メンテナンスの回数は消費しません。'}}
function sendMail(){showMail.value=false;notice.value=`${mailTarget.value} 様への「${mailKind.value}」を送信した想定のデモです（実際には送信されません）。`}
const customerBikes = computed(()=>bikes.value.filter(b=>b.customer==='山田 太郎'))
</script>

<template>
<div class="app-shell">
  <aside class="sidebar">
    <div class="brand"><div class="brand-mark">R</div><div><strong>ROAD BIKE CARE</strong><small>メンテナンス管理</small></div></div>
    <div class="side-label">店舗管理メニュー</div>
    <button v-for="item in [
      {id:'dashboard',label:'ダッシュボード',icon:'▦'},
      {id:'bikes',label:'顧客・自転車管理',icon:'🚴'},
      {id:'overhaul',label:'オーバーホール',icon:'⚙'},
      {id:'maintenance',label:'定期メンテナンス',icon:'◷'},
      {id:'events',label:'イベント前点検',icon:'◇'},
      {id:'history',label:'メンテナンス履歴',icon:'▤'},
      {id:'messages',label:'連絡・メール管理',icon:'✉'}
    ]" :key="item.id" class="nav-item" :class="{active:page===item.id}" @click="setPage(item.id as Page)"><span class="nav-icon">{{item.icon}}</span>{{item.label}}</button>
    <div class="sidebar-bottom"><div class="side-label">表示モード</div><button class="mode-switch" @click="role=role==='staff'?'customer':'staff';page=role==='customer'?'mypage':'dashboard'"><span>{{role==='staff'?'店舗スタッフ':'顧客マイページ'}}</span><span>切替 ↗</span></button><div class="user"><div class="avatar">店</div><div><b>{{role==='staff'?'店舗管理者':'山田 太郎'}}</b><small>{{role==='staff'?'スタッフアカウント':'顧客アカウント'}}</small></div></div></div>
  </aside>
  <main class="main">
    <header class="topbar"><div class="breadcrumb">ROAD BIKE CARE <span>/</span> {{({dashboard:'ダッシュボード',bikes:'顧客・自転車管理',overhaul:'オーバーホール',maintenance:'定期メンテナンス',events:'イベント前点検',history:'メンテナンス履歴',messages:'連絡・メール管理',mypage:'マイページ'} as Record<Page,string>)[page]}}</div><div class="top-actions"><span class="today">{{today}}</span><button class="icon-btn" title="通知">♧<i></i></button><div class="avatar">店</div></div></header>
    <div class="content">
      <div v-if="notice" class="notice"><span>{{notice}}</span><button @click="notice=''">×</button></div>

      <template v-if="page==='dashboard'">
        <div class="page-heading"><div><p class="eyebrow">ROAD BIKE SERVICE OVERVIEW</p><h1>ダッシュボード</h1><p class="sub">オーバーホール・定期メンテナンス・イベント前点検の状況です。</p></div><button class="primary" @click="setPage('overhaul')">オーバーホール予定を確認</button></div>
        <div class="stats"><div class="stat-card"><div class="stat-top"><span>登録自転車数</span><span class="stat-icon blue">🚴</span></div><strong>{{bikes.length}}</strong><small>台</small></div><div class="stat-card"><div class="stat-top"><span>未完了の作業予定</span><span class="stat-icon purple">⚙</span></div><strong>{{upcomingCount}}</strong><small>件</small></div><div class="stat-card"><div class="stat-top"><span>案内待ち</span><span class="stat-icon orange">✉</span></div><strong>{{pendingCount}}</strong><small>件</small></div><div class="stat-card"><div class="stat-top"><span>作業履歴</span><span class="stat-icon green">✓</span></div><strong>{{completedCount}}</strong><small>件</small></div></div>
        <section class="panel"><div class="panel-head"><div><h2>直近のメンテナンス予定</h2><p>オーバーホール・定期点検を日付順に表示</p></div><button class="text-btn" @click="setPage('maintenance')">定期点検一覧 →</button></div><div class="table-wrap"><table><thead><tr><th>予定日</th><th>作業区分</th><th>顧客</th><th>自転車</th><th>状態</th></tr></thead><tbody><tr v-for="s in allUpcoming.slice(0,6)" :key="s.id"><td><b>{{s.date}}</b></td><td>{{s.kind}}</td><td>{{s.bike?.customer}}</td><td>{{s.bike?.maker}} {{s.bike?.model}}</td><td><span class="status" :class="s.status==='案内待ち'?'warning':s.status==='予約済み'?'success':'info'">{{s.status}}</span></td></tr></tbody></table></div></section>
        <div class="section-grid"><section class="panel"><div class="panel-head"><div><h2>イベント前点検</h2><p>登録済みのイベントと点検予定</p></div><button class="text-btn" @click="setPage('events')">一覧を見る →</button></div><div class="event-mini" v-for="e in eventChecks" :key="e.id"><div class="event-date"><b>{{e.inspectionDate.slice(5,7)}}</b><small>{{e.inspectionDate.slice(8,10)}}</small></div><div class="event-info"><b>{{e.name}}</b><small>{{bikes.find(b=>b.id===e.bikeId)?.customer}} ・ 点検 {{e.inspectionDate}}</small></div><span class="status info">{{e.status}}</span></div><div class="hint">※ イベント前点検は定期メンテナンスの契約回数を消費しません。</div></section>
          <section class="panel"><div class="panel-head"><div><h2>契約プラン</h2><p>自転車ごとの契約内容</p></div><button class="text-btn" @click="setPage('bikes')">自転車一覧 →</button></div><div v-for="b in bikes" :key="b.id" class="plan-line"><b>{{b.customer}}</b><span>{{b.maker}} {{b.model}}</span><small>{{b.plan}} · 定期 {{b.regularLimit}}回</small></div></section></div>
      </template>

      <template v-else-if="page==='bikes'">
        <div class="page-heading"><div><p class="eyebrow">CUSTOMERS & BICYCLES</p><h1>顧客・自転車管理</h1><p class="sub">顧客ごとに所有するロードバイクと契約プランを管理します。</p></div><button class="primary" @click="notice='自転車の新規登録フォームは次の実装段階で追加します。'">＋ 自転車を登録</button></div>
        <section class="panel"><div class="panel-head"><div><h2>登録自転車一覧</h2><p>{{bikes.length}}台の自転車が登録されています</p></div><span class="count">顧客・自転車単位で契約管理</span></div><div class="table-wrap"><table><thead><tr><th>顧客</th><th>自転車</th><th>メーカー・モデル</th><th>契約プラン</th><th>定期点検残回数</th><th></th></tr></thead><tbody><tr v-for="b in bikes" :key="b.id"><td><b>{{b.customer}}</b></td><td>{{b.name}}<small class="cell-sub">{{b.year}}年モデル · {{b.serial}}</small></td><td>{{b.maker}} {{b.model}}</td><td>{{b.plan}}<small class="cell-sub">{{b.contractStart}}〜{{b.contractEnd}}</small></td><td><b class="remaining">{{remaining(b)}} / {{b.regularLimit}}回</b></td><td><button class="outline small" @click="selectedBikeId=b.id;notice=`${b.name}の詳細を選択しました。`">詳細</button></td></tr></tbody></table></div></section>
        <section class="panel detail-panel"><div class="panel-head"><div><h2>自転車・契約詳細</h2><p>選択中の自転車</p></div><select v-model.number="selectedBikeId"><option v-for="b in bikes" :key="b.id" :value="b.id">{{b.customer}} / {{b.name}}</option></select></div><div class="detail-grid"><div><label>顧客</label><b>{{selectedBike.customer}}</b></div><div><label>メーカー・モデル</label><b>{{selectedBike.maker}} {{selectedBike.model}}</b></div><div><label>製造番号</label><b>{{selectedBike.serial}}</b></div><div><label>契約プラン</label><b>{{selectedBike.plan}}</b></div><div><label>契約期間</label><b>{{selectedBike.contractStart}}〜{{selectedBike.contractEnd}}</b></div><div><label>定期点検残回数</label><b class="remaining large">{{remaining(selectedBike)}} / {{selectedBike.regularLimit}}回</b></div></div><div class="hint">オーバーホールは1回、定期メンテナンスは契約プランの回数、イベント前点検はイベントごとに別管理します。</div></section>
      </template>

      <template v-else-if="page==='overhaul'">
        <div class="page-heading"><div><p class="eyebrow">OVERHAUL MANAGEMENT</p><h1>オーバーホール管理</h1><p class="sub">契約時に指定したオーバーホール日と作業完了を管理します。</p></div><button class="primary" @click="notice='オーバーホールの新規登録フォームは次の実装段階で追加します。'">＋ オーバーホールを登録</button></div>
        <div class="callout"><b>定期メンテナンスの起点</b><p>オーバーホールの実施完了日を起点に、2か月後・4か月後…の定期メンテナンス予定を契約回数分作成します。予定日を登録しただけでは定期点検は生成されません。</p></div>
        <section class="panel"><div class="panel-head"><div><h2>オーバーホール予定・実績</h2><p>自転車ごとに原則1回の初回オーバーホール</p></div><span class="count">{{overhauls.length}}件</span></div><div class="table-wrap"><table><thead><tr><th>顧客</th><th>自転車</th><th>プラン</th><th>指定日</th><th>実施完了日</th><th>状態</th><th>操作</th></tr></thead><tbody><tr v-for="o in overhauls" :key="o.id"><td><b>{{bikes.find(b=>b.id===o.bikeId)?.customer}}</b></td><td>{{bikes.find(b=>b.id===o.bikeId)?.maker}} {{bikes.find(b=>b.id===o.bikeId)?.model}}</td><td>{{bikes.find(b=>b.id===o.bikeId)?.plan}}</td><td>{{o.plannedDate}}</td><td>{{o.completedDate || '—'}}</td><td><span class="status" :class="o.status==='完了'?'success':o.status==='予約済み'?'info':'warning'">{{o.status}}</span></td><td><button v-if="o.status!=='完了'" class="outline small" @click="setOverhaulComplete(o)">実施完了</button><span v-else class="done">✓ 完了</span></td></tr></tbody></table></div><div class="panel-foot">※ オーバーホール完了後に定期点検予定を生成します。完了登録はデモ操作で、予定日を実施日として扱います。</div></section>
      </template>

      <template v-else-if="page==='maintenance'">
        <div class="page-heading"><div><p class="eyebrow">REGULAR MAINTENANCE</p><h1>定期メンテナンス</h1><p class="sub">オーバーホール完了日から2か月単位で生成された点検予定を管理します。</p></div><span class="status info">基本サイクル：2か月</span></div>
        <div class="callout"><b>予定生成ルール</b><p>オーバーホール実施完了日の2か月後を1回目として、契約回数分の予定を作成します。月末日など該当日がない場合は、その月の末日を使用します。</p></div>
        <section class="panel"><div class="panel-head"><div><h2>定期メンテナンス一覧</h2><p>実施完了時のみ定期メンテナンス回数を消費します</p></div><span class="count">{{schedules.length}}件</span></div><div class="table-wrap"><table><thead><tr><th>予定日</th><th>顧客・自転車</th><th>回数</th><th>案内・予約状況</th><th>契約残回数</th><th>操作</th></tr></thead><tbody><tr v-for="s in schedules" :key="s.id"><td><b>{{s.date}}</b></td><td>{{bikes.find(b=>b.id===s.bikeId)?.customer}}<small class="cell-sub">{{bikes.find(b=>b.id===s.bikeId)?.maker}} {{bikes.find(b=>b.id===s.bikeId)?.model}}</small></td><td>{{s.cycle}}回目</td><td><select class="status-select" v-model="s.status"><option>予定</option><option>案内待ち</option><option>案内済み</option><option>予約済み</option></select></td><td>{{remaining(bikes.find(b=>b.id===s.bikeId)! )}}回</td><td><button v-if="s.status!=='完了'" class="outline small" @click="completeSchedule(s)">実施完了</button><span v-else class="done">✓ 完了</span></td></tr></tbody></table></div><div class="panel-foot">※ 案内・予約・予定登録では回数を消費しません。完了ボタンで実施履歴を作成し、残回数を1回減らします。</div></section>
      </template>

      <template v-else-if="page==='events'">
        <div class="page-heading"><div><p class="eyebrow">EVENT INSPECTIONS</p><h1>イベント前点検</h1><p class="sub">レースやサイクルイベントごとに点検予定を登録します。</p></div><button class="primary" @click="notice='イベント前点検の新規登録フォームは次の実装段階で追加します。'">＋ イベント前点検を登録</button></div>
        <div class="callout"><b>定期メンテナンスとは独立したサービス</b><p>イベント前点検はイベントごとに複数回登録できます。定期メンテナンスの契約回数には含まれず、イベント点検の実施履歴として管理します。</p></div>
        <section class="panel"><div class="panel-head"><div><h2>イベント・点検予定一覧</h2><p>イベント日と点検日を管理</p></div><span class="count">{{eventChecks.length}}件</span></div><div class="table-wrap"><table><thead><tr><th>イベント名</th><th>顧客・自転車</th><th>イベント開催日</th><th>点検予定日</th><th>状態</th><th>操作</th></tr></thead><tbody><tr v-for="e in eventChecks" :key="e.id"><td><b>{{e.name}}</b><small class="cell-sub">{{e.memo}}</small></td><td>{{bikes.find(b=>b.id===e.bikeId)?.customer}}<small class="cell-sub">{{bikes.find(b=>b.id===e.bikeId)?.model}}</small></td><td>{{e.eventDate}}</td><td>{{e.inspectionDate}}</td><td><span class="status" :class="e.status==='完了'?'success':'info'">{{e.status}}</span></td><td><button v-if="e.status!=='完了'" class="outline small" @click="completeEvent(e)">点検完了</button><span v-else class="done">✓ 完了</span></td></tr></tbody></table></div><div class="panel-foot">イベント点検を完了しても、定期メンテナンスの残回数は変わりません。</div></section>
      </template>

      <template v-else-if="page==='history'">
        <div class="page-heading"><div><p class="eyebrow">SERVICE HISTORY</p><h1>メンテナンス履歴</h1><p class="sub">自転車ごとのオーバーホール・定期点検・イベント前点検の履歴です。</p></div><select class="search" v-model.number="selectedBikeId"><option v-for="b in bikes" :key="b.id" :value="b.id">{{b.customer}} / {{b.name}}</option></select></div>
        <section class="panel"><div class="panel-head"><div><h2>{{selectedBike.maker}} {{selectedBike.model}}</h2><p>{{selectedBike.customer}} 様の作業履歴</p></div><span class="count">{{histories.filter(h=>h.bikeId===selectedBikeId).length}}件</span></div><div class="table-wrap"><table><thead><tr><th>実施日</th><th>作業区分</th><th>作業内容</th><th>詳細</th></tr></thead><tbody><tr v-for="h in histories.filter(x=>x.bikeId===selectedBikeId).sort((a,b)=>b.date.localeCompare(a.date))" :key="h.id"><td><b>{{h.date}}</b></td><td><span class="status info">{{h.category}}</span></td><td>{{h.title}}</td><td>{{h.detail}}</td></tr></tbody></table></div></section>
      </template>

      <template v-else-if="page==='messages'">
        <div class="page-heading"><div><p class="eyebrow">COMMUNICATION</p><h1>連絡・メール管理</h1><p class="sub">オーバーホール・定期点検の案内対象を確認します。</p></div><button class="primary" @click="showMail=true">✉ メールを作成</button></div>
        <section class="panel"><div class="panel-head"><div><h2>定期メンテナンス案内対象</h2><p>案内待ちの点検予定</p></div><span class="status warning">{{pendingCount}}件 案内待ち</span></div><div class="table-wrap"><table><thead><tr><th>顧客</th><th>自転車</th><th>点検予定日</th><th></th></tr></thead><tbody><tr v-for="s in schedules.filter(x=>x.status==='案内待ち')" :key="s.id"><td>{{bikes.find(b=>b.id===s.bikeId)?.customer}}</td><td>{{bikes.find(b=>b.id===s.bikeId)?.model}}</td><td>{{s.date}}</td><td><button class="outline small" @click="mailTarget=bikes.find(b=>b.id===s.bikeId)?.customer || '';mailKind='定期メンテナンスのご案内';showMail=true">内容確認</button></td></tr></tbody></table></div></section>
        <section class="panel"><div class="panel-head"><div><h2>メール送信デモ</h2><p>実際のメール送信は行いません</p></div></div><div class="empty-state"><div class="empty-icon">✉</div><b>送信履歴はまだありません</b><p>メール作成から案内内容を確認できます。</p></div></section>
      </template>

      <template v-else>
        <div class="page-heading"><div><p class="eyebrow">CUSTOMER PORTAL</p><h1>マイページ</h1><p class="sub">ご登録のロードバイクとメンテナンス予定をご確認いただけます。</p></div><span class="customer-welcome">山田 太郎 様</span></div>
        <section class="panel"><div class="panel-head"><div><h2>ご契約中のロードバイク</h2><p>{{customerBikes.length}}台</p></div><span class="status success">契約中</span></div><div v-for="b in customerBikes" :key="b.id" class="customer-bike"><div class="bike-symbol">🚴</div><div class="equipment-details"><b>{{b.maker}} {{b.model}}</b><small>{{b.plan}} · 契約期間 {{b.contractStart}}〜{{b.contractEnd}}</small><small>定期メンテナンス残り {{remaining(b)}} / {{b.regularLimit}}回</small></div></div></section>
        <section class="panel"><div class="panel-head"><div><h2>定期メンテナンス予定</h2><p>オーバーホール完了日を起点に2か月ごと</p></div></div><div class="table-wrap"><table><thead><tr><th>予定日</th><th>自転車</th><th>回数</th><th>状態</th></tr></thead><tbody><tr v-for="s in schedules.filter(x=>customerBikes.some(b=>b.id===x.bikeId))" :key="s.id"><td><b>{{s.date}}</b></td><td>{{bikes.find(b=>b.id===s.bikeId)?.model}}</td><td>{{s.cycle}}回目</td><td><span class="status info">{{s.status}}</span></td></tr></tbody></table></div></section>
        <section class="panel"><div class="panel-head"><div><h2>イベント前点検</h2><p>イベントごとの点検予定</p></div></div><div class="event-mini" v-for="e in eventChecks.filter(x=>customerBikes.some(b=>b.id===x.bikeId))" :key="e.id"><div class="event-date"><b>{{e.inspectionDate.slice(5,7)}}</b><small>{{e.inspectionDate.slice(8,10)}}</small></div><div class="event-info"><b>{{e.name}}</b><small>イベント {{e.eventDate}} / 点検 {{e.inspectionDate}}</small></div><span class="status info">{{e.status}}</span></div></section>
      </template>
    </div>
  </main>
</div>
<div v-if="showMail" class="modal-backdrop" @click.self="showMail=false"><div class="modal"><div class="modal-head"><div><p class="eyebrow">EMAIL PREVIEW</p><h2>メンテナンス案内メール</h2></div><button class="close" @click="showMail=false">×</button></div><label>送信先</label><select v-model="mailTarget"><option v-for="c in customers" :key="c.name">{{c.name}}</option></select><label>件名</label><input v-model="mailKind"/><label>本文</label><div class="mail-body">{{mailTarget}} 様<br><br>いつもご利用いただきありがとうございます。<br>ロードバイクのメンテナンス時期となりましたのでご案内いたします。<br><br>ご希望の日程がございましたら店舗までご連絡ください。<br><br>今後ともよろしくお願いいたします。</div><div class="modal-note">※ モックのため、実際のメール送信は行われません。</div><div class="modal-actions"><button class="outline" @click="showMail=false">閉じる</button><button class="primary" @click="sendMail">送信（デモ）</button></div></div></div>
</template>
