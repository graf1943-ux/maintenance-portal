using RoadbikeCare.Api.Models;

namespace RoadbikeCare.Api.Data;

// Mock implementation. Replace this store with repository + PostgreSQL in the next phase.
public sealed class MockDataStore
{
    public List<Customer> Customers { get; } =
    [
        new(1, "山田 太郎", "t.yamada@example.com", "090-0000-0001"),
        new(2, "佐藤 花子", "h.sato@example.com", "090-0000-0002"),
        new(3, "鈴木 一郎", "i.suzuki@example.com", "090-0000-0003")
    ];

    public List<Shop> Shops { get; } =
    [new(1, "金沢本店", "石川県金沢市", "076-000-0001"), new(2, "富山店", "富山県富山市", "076-000-0002")];

    public List<StaffMember> Staff { get; } =
    [new(1, 1, "田中", "受付担当"), new(2, 1, "山本", "メカニック"), new(3, 2, "佐々木", "受付担当")];

    public List<Plan> Plans { get; } =
    [new(1, "プレミアムプラン", 1, 4, 3), new(2, "スタンダードプラン", 1, 3, 2), new(3, "ライトプラン", 1, 2, 1)];

    public List<Bike> Bikes { get; } =
    [
        new(1, 1, "SPECIALIZED", "Tarmac SL8 Expert", 2025, "WSBC0250A001", 1),
        new(2, 2, "TREK", "Madone SL 7", 2024, "WTU12345678", 2),
        new(3, 3, "GIANT", "TCR Advanced Pro", 2023, "GNT2023-0088", 3),
        new(4, 1, "CANYON", "Speedmax CF 7", 2025, "CNY-SM-2025-014", 4)
    ];

    public List<Contract> Contracts { get; } =
    [
        new(1, 1, 1, new(2026, 12, 15), new(2026, 12, 1), new(2027, 11, 30), 1, 1, 0, 1, 1),
        new(2, 2, 2, new(2026, 12, 15), new(2026, 12, 1), new(2027, 11, 30), 1, 1),
        new(3, 3, 3, new(2026, 12, 15), new(2026, 12, 1), new(2027, 11, 30), 2, 3, 1, 2, 0),
        new(4, 4, 2, new(2026, 12, 15), new(2026, 12, 1), new(2027, 11, 30), 1, 1, 0, 1, 0)
    ];

    public List<WorkItem> WorkItems { get; } =
    [
        new(1, 1, "overhaul", new(2027, 1, 15), new(2027, 1, 16), "作業中", "年間オーバーホール", "全体オーバーホール・消耗品点検", 1, 1),
        new(2, 2, "overhaul", new(2027, 2, 5), null, "予約済み", "年間オーバーホール", "納車後初回オーバーホール", 1, 1),
        new(3, 3, "overhaul", new(2027, 1, 15), new(2027, 1, 16), "完了", "年間オーバーホール", "駆動系洗浄・ワイヤー交換", 2, 3, new(2027, 1, 16)),
        new(4, 4, "overhaul", new(2027, 2, 12), null, "予定", "年間オーバーホール", "専用コクピット・油圧ブレーキ・駆動系点検", 1, 1),
        new(5, 1, "maintenance", new(2027, 4, 10), new(2027, 4, 10), "完了", "春の目標イベント前メンテナンス", "駆動系・変速・ブレーキ調整", 1, 1, new(2027, 4, 10)),
        new(6, 1, "maintenance", new(2027, 6, 5), null, "予定", "夏季メインイベントに向けたメンテナンス", "消耗品・ホイール・ポジション確認", 1, 1),
        new(7, 3, "maintenance", new(2027, 5, 20), new(2027, 5, 21), "完了", "シーズン前メンテナンス", "変速・ブレーキ・タイヤ点検", 2, 3, new(2027, 5, 21)),
        new(8, 3, "maintenance", new(2027, 8, 1), new(2027, 8, 1), "完了", "夏季イベント前メンテナンス", "チェーン清掃・注油・各部増し締め", 2, 3, new(2027, 8, 1)),
        new(9, 4, "maintenance", new(2027, 6, 12), null, "予定", "TTイベント前ポジション・機材確認", "エアロポジション・変速・ブレーキ・タイヤ確認", 1, 1)
    ];

    public List<EventCheck> Events { get; } =
    [
        new(1, 1, "シーズン開幕ロードレース", new(2027, 4, 18), new(2027, 4, 14), new(2027, 4, 15), "作業中", "イベント直前の安全確認", 1, 1),
        new(2, 1, "夏のメインイベント", new(2027, 7, 25), new(2027, 7, 21), null, "予定", "タイヤ・ブレーキ・締結確認", 1, 1),
        new(3, 2, "冬季サイクルイベント", new(2027, 12, 10), new(2027, 12, 7), null, "予定", "次年度契約のイベント予定例", 1, 1),
        new(4, 4, "夏季TTメインイベント", new(2027, 6, 20), new(2027, 6, 16), null, "予定", "TTポジション・ホイール・ブレーキの事前確認", 1, 1)
    ];

    public List<WorkHistory> History { get; } =
    [
        new(1, 1, new(2027, 4, 10), new(2027, 4, 10), "メンテナンス", "春の目標イベント前メンテナンス", "駆動系・変速・ブレーキ調整"),
        new(2, 3, new(2027, 1, 16), new(2027, 1, 15), "オーバーホール", "年間オーバーホール", "駆動系洗浄・ワイヤー交換"),
        new(3, 3, new(2027, 5, 21), new(2027, 5, 20), "メンテナンス", "シーズン前メンテナンス", "変速・ブレーキ・タイヤ点検"),
        new(4, 3, new(2027, 8, 1), new(2027, 8, 1), "メンテナンス", "夏季イベント前メンテナンス", "チェーン清掃・注油・各部増し締め"),
        new(5, 4, new(2027, 5, 18), new(2027, 5, 18), "メンテナンス", "初回メンテナンス", "ポジション・変速・ブレーキ調整", "左右パッドクリアランスを再確認")
    ];

    public int NextId<T>(IReadOnlyCollection<T> items, Func<T, int> getId) => items.Count == 0 ? 1 : items.Max(getId) + 1;
}
