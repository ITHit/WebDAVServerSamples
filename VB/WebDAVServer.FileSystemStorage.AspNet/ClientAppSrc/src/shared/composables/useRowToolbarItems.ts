import type { HierarchyItem } from '@/domain/entities/HierarchyItem';
import type {
  FileBrowserContext,
  ResolvedRowToolbarItem,
  RowToolbarItemConfig,
} from '@/shared/config/config-types';
import { defaultRowToolbarItems } from '@/shared/config/row-toolbar-config';
import type { FileBrowserContract } from '@/shared/contracts/fileBrowserContract';
import { t } from '@/shared/i18n/translate';

export function useRowToolbarItems(
  fileBrowser: FileBrowserContract,
  options?: { rowToolbarItems?: RowToolbarItemConfig[] }
) {
  const cfgItems = options?.rowToolbarItems ?? defaultRowToolbarItems;

  const getRowToolbarItems = (item: HierarchyItem): ResolvedRowToolbarItem[] => {
    const context: FileBrowserContext = { fileBrowser };

    return cfgItems
      .filter(cfg => !cfg.isVisible || cfg.isVisible(item, context))
      .map(cfg => ({
        id: cfg.id,
        title: t(cfg.title),
        icon: cfg.icon,
        disabled: cfg.isDisabled ? cfg.isDisabled(item, context) : false,
        action: () => cfg.action(item, context),
      }));
  };

  return { getRowToolbarItems };
}
