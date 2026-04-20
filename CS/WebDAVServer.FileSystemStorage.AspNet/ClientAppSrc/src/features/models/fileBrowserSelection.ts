export interface SelectionState {
  selectedIndexes: number[];
  selectionAnchor: number | null;
}

export function getSelectedItems<T>(items: T[], selectedIndexes: number[]): T[] {
  return selectedIndexes.map((index) => items[index]).filter(Boolean);
}

export function isAllSelected(itemCount: number, selectedCount: number): boolean {
  return itemCount > 0 && selectedCount === itemCount;
}

export function hasSelection(selectedCount: number): boolean {
  return selectedCount > 0;
}

export function toggleSelection(state: SelectionState, index: number): SelectionState {
  const nextSelectedIndexes = [...state.selectedIndexes];
  const pos = nextSelectedIndexes.indexOf(index);

  if (pos > -1) {
    nextSelectedIndexes.splice(pos, 1);
  } else {
    nextSelectedIndexes.push(index);
  }

  return {
    selectedIndexes: nextSelectedIndexes,
    selectionAnchor: index,
  };
}

export function selectSingle(index: number): SelectionState {
  return {
    selectedIndexes: [index],
    selectionAnchor: index,
  };
}

export function setRangeFromAnchor(state: SelectionState, index: number): SelectionState {
  const anchor = state.selectionAnchor ?? index;
  const start = Math.min(anchor, index);
  const end = Math.max(anchor, index);
  const selectedIndexes: number[] = [];

  for (let i = start; i <= end; i++) {
    selectedIndexes.push(i);
  }

  return {
    selectedIndexes,
    // Keep anchor fixed for Shift-based range operations.
    selectionAnchor: state.selectionAnchor ?? index,
  };
}

export function selectAll(itemCount: number): SelectionState {
  return {
    selectedIndexes: Array.from({ length: itemCount }, (_, index) => index),
    selectionAnchor: null,
  };
}

export function clearSelection(): SelectionState {
  return {
    selectedIndexes: [],
    selectionAnchor: null,
  };
}

export function toggleSelectAll(state: SelectionState, itemCount: number): SelectionState {
  if (isAllSelected(itemCount, state.selectedIndexes.length)) {
    return clearSelection();
  }

  return selectAll(itemCount);
}

export function moveSelection(state: SelectionState, itemCount: number, delta: 1 | -1, extend: boolean): SelectionState {
  if (itemCount === 0) {
    return state;
  }

  if (extend) {
    const anchor = state.selectionAnchor;

    if (anchor === null) {
      return selectSingle(delta === 1 ? 0 : itemCount - 1);
    }

    const min = state.selectedIndexes.length > 0 ? Math.min(...state.selectedIndexes) : anchor;
    const max = state.selectedIndexes.length > 0 ? Math.max(...state.selectedIndexes) : anchor;
    const focus = anchor === min ? max : min;

    return setRangeFromAnchor(state, (focus + delta + itemCount) % itemCount);
  }

  const next =
    state.selectionAnchor === null
      ? delta === 1 ? 0 : itemCount - 1
      : (state.selectionAnchor + delta + itemCount) % itemCount;

  return selectSingle(next);
}
