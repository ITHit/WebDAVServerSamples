import { HierarchyItem } from '@/domain/entities/HierarchyItem';

export interface FileBrowserUiPorts {
  showCreateFolderDialog(createFolder: (folderName: string) => Promise<void>): Promise<void>;
  showRenameItemDialog(item: HierarchyItem, renameItem: (newName: string) => Promise<void>): Promise<void>;
  confirmDelete(): Promise<boolean>;
  showCopyConflictDialog(
    conflictingNames: string[],
    overwriteConflictingItems: () => Promise<void>
  ): Promise<void>;
  openFolderInOsFileManager(folderPath: string): void;
}
