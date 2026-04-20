import { FileItem } from './FileItem';
import { FolderItem } from './FolderItem';

/**
 * Union type representing either a file or folder
 */
export type HierarchyItem = FileItem | FolderItem;

/**
 * Type guard to check if item is a file
 */
export function isFileItem(item: HierarchyItem): item is FileItem {
  return item instanceof FileItem;
}

/**
 * Type guard to check if item is a folder
 */
export function isFolderItem(item: HierarchyItem): item is FolderItem {
  return item instanceof FolderItem;
}
