import { createSelector } from "@reduxjs/toolkit";
import { RootState } from "./store";

export const isAllSelected = createSelector(
    (state: RootState) => state.grid.items,
    (state: RootState) => state.grid.selectedIndexes,
    (items, selectedIndexes) => selectedIndexes.length > 0 && items.length === selectedIndexes.length
);

export const getSelectedItems = createSelector(
    (state: RootState) => state.grid.items,
    (state: RootState) => state.grid.selectedIndexes,
    (items, selectedIndexes) => selectedIndexes.map(index => items[index])
);