export interface ShopStaffMember {
  shopId: number;
  name: string;
}

/** 店舗内で指定担当者を優先して検索し、見つからなければ同店舗の担当者を返します。 */
export function findReceptionist<T extends ShopStaffMember>(
  staff: T[],
  shopId: number,
  preferredName?: string
) {
  return (
    staff.find((member) => member.shopId === shopId && member.name === preferredName) ??
    staff.find((member) => member.shopId === shopId)
  );
}
