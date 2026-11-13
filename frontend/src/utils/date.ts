/** ローカル日付を日付入力向けの YYYY-MM-DD 形式で返します。 */
export function localDateInput() {
  const date = new Date();
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

/** YYYY-MM-DD または YYYY/M/D の日付と基準日の差をローカル日付で計算します。 */
export function daysUntilLocalDate(dateValue: string, today: Date): number | null {
  const match = /^(\d{4})[/-](\d{1,2})[/-](\d{1,2})$/.exec(dateValue);
  if (!match) return null;

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const targetDate = new Date(year, month - 1, day);
  if (
    targetDate.getFullYear() !== year ||
    targetDate.getMonth() !== month - 1 ||
    targetDate.getDate() !== day
  ) {
    return null;
  }

  const todayStart = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  return Math.round((targetDate.getTime() - todayStart.getTime()) / 86_400_000);
}
