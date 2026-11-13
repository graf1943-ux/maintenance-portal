/** 数値IDを持つ一覧から、現在の最大値に1を加えたIDを返します。 */
export function nextId(items: readonly { id: number }[]) {
  let maxId = 0;
  for (const item of items) maxId = Math.max(maxId, item.id);
  return maxId + 1;
}
