import { HierarchyItem } from '@/domain/entities/HierarchyItem';
import {
  getDocumentUrls,
  getSingleDocumentUrl,
  ManageDocumentOperation,
} from '@/features/models/fileBrowserDocumentActions';

export type ManageDocumentsFn = (fileUrls: string[], operation: ManageDocumentOperation) => void;

export function printItemsCore(selectedItems: HierarchyItem[], manageDocuments: ManageDocumentsFn): void {
  const fileUrls = getDocumentUrls(selectedItems);
  if (fileUrls.length === 0) {
    return;
  }

  manageDocuments(fileUrls, 'Print');
}

export function editItemCore(
  item: HierarchyItem | undefined,
  selectedItems: HierarchyItem[],
  manageDocuments: ManageDocumentsFn
): void {
  const fileUrl = getSingleDocumentUrl(item ?? selectedItems[0]);
  if (!fileUrl) {
    return;
  }

  manageDocuments([fileUrl], 'Edit');
}

export function editItemWithCore(
  item: HierarchyItem | undefined,
  selectedItems: HierarchyItem[],
  manageDocuments: ManageDocumentsFn
): void {
  const fileUrl = getSingleDocumentUrl(item ?? selectedItems[0]);
  if (!fileUrl) {
    return;
  }

  manageDocuments([fileUrl], 'OpenWith');
}

export function lockItemsCore(
  items: HierarchyItem[] | undefined,
  selectedItems: HierarchyItem[],
  manageDocuments: ManageDocumentsFn
): void {
  const itemSet = items ?? selectedItems;
  const fileUrls = getDocumentUrls(itemSet);

  if (fileUrls.length === 0) {
    return;
  }

  manageDocuments(fileUrls, 'Lock');
}

export function unlockItemsCore(
  items: HierarchyItem[] | undefined,
  selectedItems: HierarchyItem[],
  manageDocuments: ManageDocumentsFn
): void {
  const itemSet = items ?? selectedItems;
  const fileUrls = getDocumentUrls(itemSet);

  if (fileUrls.length === 0) {
    return;
  }

  manageDocuments(fileUrls, 'Unlock');
}
