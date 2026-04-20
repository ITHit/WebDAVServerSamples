import {
  SelectionState,
  clearSelection as clearSelectionState,
  moveSelection as moveSelectionState,
  selectAll as selectAllState,
  selectSingle as selectSingleState,
  setRangeFromAnchor as setRangeFromAnchorState,
  toggleSelectAll as toggleSelectAllState,
  toggleSelection as toggleSelectionState,
} from '@/features/models/fileBrowserSelection';
import type { MutableBox } from '@/shared/types/box';

export interface SelectionControllerConfig {
  selectedIndexes: MutableBox<number[]>;
  selectionAnchor: MutableBox<number | null>;
  getItemCount: () => number;
}

function currentSelectionState(config: SelectionControllerConfig): SelectionState {
  return {
    selectedIndexes: config.selectedIndexes.value,
    selectionAnchor: config.selectionAnchor.value,
  };
}

function applySelectionState(config: SelectionControllerConfig, next: SelectionState): void {
  config.selectedIndexes.value = next.selectedIndexes;
  config.selectionAnchor.value = next.selectionAnchor;
}

export function createSelectionController(config: SelectionControllerConfig) {
  const toggleSelection = (index: number) => {
    const next = toggleSelectionState(currentSelectionState(config), index);
    applySelectionState(config, next);
  };

  const selectSingle = (index: number) => {
    const next = selectSingleState(index);
    applySelectionState(config, next);
  };

  const setRangeFromAnchor = (index: number) => {
    const next = setRangeFromAnchorState(currentSelectionState(config), index);
    applySelectionState(config, next);
  };

  const selectAll = () => {
    const next = selectAllState(config.getItemCount());
    // Keep existing anchor behavior for compatibility with current UI flow.
    config.selectedIndexes.value = next.selectedIndexes;
  };

  const clearSelection = () => {
    const next = clearSelectionState();
    applySelectionState(config, next);
  };

  const toggleSelectAll = () => {
    const next = toggleSelectAllState(currentSelectionState(config), config.getItemCount());
    applySelectionState(config, next);
  };

  const moveSelection = (delta: 1 | -1, extend: boolean) => {
    const next = moveSelectionState(currentSelectionState(config), config.getItemCount(), delta, extend);
    applySelectionState(config, next);
  };

  return {
    toggleSelection,
    selectSingle,
    setRangeFromAnchor,
    selectAll,
    clearSelection,
    toggleSelectAll,
    moveSelection,
  };
}
