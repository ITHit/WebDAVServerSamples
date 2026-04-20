import type { HierarchyItem } from '@/domain/entities/HierarchyItem';
import type { FileBrowserUiPorts } from '@/features/models/fileBrowserUiPorts';
import { CreateFolderModal } from '@/components/modals/CreateFolderModal';
import { InstallProtocolModal } from '@/components/modals/InstallProtocolModal';
import { RenameItemModal } from '@/components/modals/RenameItemModal';
import { RewriteModal } from '@/components/modals/RewriteModal';
import { SubmitModal } from '@/components/modals/SubmitModal';
import { showModalComponent } from '@/shared/composables/useModalRegistry';
import { t } from '@/shared/i18n/translate';

type OpenFolderInOsFileManager = (folderPath: string, showProtocolInstallModal: () => void) => void;

export function createReactFileBrowserUiPorts(
  openFolderInOsFileManager: OpenFolderInOsFileManager
): FileBrowserUiPorts {
  return {
    async showCreateFolderDialog(createFolder) {
      await showModalComponent<void>(CreateFolderModal as never, {
        onSubmitAction: async (folderName: string) => {
          await createFolder(folderName);
        },
      });
    },

    async showRenameItemDialog(item: HierarchyItem, renameItem) {
      await showModalComponent<void>(RenameItemModal as never, {
        item,
        onSubmitAction: async (newName: string) => {
          await renameItem(newName);
        },
      });
    },

    async confirmDelete() {
      let confirmed = false;
      await showModalComponent<void>(SubmitModal as never, {
        message: t('phrases.modals.deleteMessage'),
        onSubmitAction: async () => {
          confirmed = true;
        },
      });
      return confirmed;
    },

    async showCopyConflictDialog(conflictingNames, overwriteConflictingItems) {
      await showModalComponent<void>(RewriteModal as never, {
        itemsList: conflictingNames,
        onSubmitAction: async () => {
          await overwriteConflictingItems();
        },
      });
    },

    openFolderInOsFileManager(folderPath: string) {
      openFolderInOsFileManager(folderPath, () => {
        void showModalComponent(InstallProtocolModal as never);
      });
    },
  };
}
