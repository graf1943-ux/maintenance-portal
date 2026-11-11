import { ref } from 'vue';

export function matches(query: string, ...values: unknown[]) {
  const normalizedQuery = query.trim().toLocaleLowerCase();
  return (
    !normalizedQuery ||
    values.some((value) =>
      String(value ?? '')
        .toLocaleLowerCase()
        .includes(normalizedQuery)
    )
  );
}

type SortField = 'date' | 'customer' | 'status';
type SortDirection = 'asc' | 'desc';
type ListSort = Record<string, { field: SortField; direction: SortDirection }>;

export function useListSort() {
  const listSort = ref<ListSort>({});

  function toggleListSort(key: string, field: SortField) {
    const current = listSort.value[key];
    listSort.value[key] =
      current?.field === field
        ? { field, direction: current.direction === 'asc' ? 'desc' : 'asc' }
        : { field, direction: 'asc' };
  }

  function sortByDate<T>(
    items: T[],
    key: string,
    getDate: (item: T) => string,
    getCustomer?: (item: T) => string,
    getStatus?: (item: T) => string
  ) {
    const sort = listSort.value[key] ?? {
      field: 'date' as SortField,
      direction: 'asc' as SortDirection,
    };
    const getter =
      sort.field === 'customer' ? getCustomer : sort.field === 'status' ? getStatus : getDate;
    const direction = sort.direction;

    return [...items].sort((a, b) => {
      const result = (getter?.(a) ?? '').localeCompare(getter?.(b) ?? '', 'ja', { numeric: true });
      return direction === 'asc' ? result : -result;
    });
  }

  function sortLabel(key: string, field: SortField) {
    const sort = listSort.value[key];
    if (sort?.field !== field) return '↕';
    return sort.direction === 'desc' ? '▼' : '▲';
  }

  return { toggleListSort, sortByDate, sortLabel };
}
