/**
 * 契約・オーバーホール日程に関する業務ルール。
 * 日付は YYYY-MM-DD 形式を受け取ります。
 */

function isValidIsoDate(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const [year, month, day] = value.split('-').map(Number);
  const parsed = new Date(Date.UTC(year, month - 1, day));
  return (
    parsed.getUTCFullYear() === year &&
    parsed.getUTCMonth() === month - 1 &&
    parsed.getUTCDate() === day
  );
}

/** 年間契約は契約開始年の12月1日開始です。 */
export function isAnnualContractStart(value: string): boolean {
  return isValidIsoDate(value) && value.endsWith('-12-01');
}

/** 受付日は契約開始年内（12月31日まで）である必要があります。 */
export function isApplicationWithinContractYear(
  applicationDate: string,
  contractYear: number
): boolean {
  if (!isValidIsoDate(applicationDate) || !Number.isInteger(contractYear)) return false;
  return applicationDate >= `${contractYear}-01-01` && applicationDate <= `${contractYear}-12-31`;
}

/** オーバーホール実施期間は契約開始年11月1日から翌年2月末まで。 */
export function isOverhaulSeason(value: string, contractYear: number): boolean {
  if (!isValidIsoDate(value) || !Number.isInteger(contractYear)) return false;
  const start = `${contractYear}-11-01`;
  const lastDay = new Date(Date.UTC(contractYear + 1, 2, 0)).getUTCDate();
  const end = `${contractYear + 1}-02-${String(lastDay).padStart(2, '0')}`;
  return value >= start && value <= end;
}
