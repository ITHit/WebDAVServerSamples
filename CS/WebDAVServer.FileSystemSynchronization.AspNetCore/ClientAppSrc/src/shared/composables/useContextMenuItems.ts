import type { HierarchyItem } from '@/domain/entities/HierarchyItem';
import type {
  ContextMenuItemConfig,
  ContainerContextMenuItemConfig,
  ContextMenuItemConfigBase,
  ResolvedContextMenuItem,
  FileBrowserContext,
} from '@/shared/config/config-types';
import { t } from '@/shared/i18n/translate';

export function useContextMenuItems(items: ContextMenuItemConfigBase[]) {
  const asContextItems = items as ContextMenuItemConfig[];
  const asContainerItems = items as ContainerContextMenuItemConfig[];

  const getContextMenuItems = (
    item?: HierarchyItem,
    context?: FileBrowserContext
  ): ResolvedContextMenuItem[] => {
    if (!context) return [];

    if (item !== undefined) {
      return asContextItems
        .filter(mi => !mi.isVisible || mi.isVisible(context, item))
        .map(mi => ({
          id: mi.id,
          label: mi.label ? t(mi.label) : undefined,
          icon: mi.icon,
          shortcutInfo: mi.shortcutInfo,
          type: mi.type,
          danger: mi.danger,
          disabled: mi.isDisabled ? mi.isDisabled(context, item) : false,
          action: mi.action ? () => mi.action?.(context, item) : undefined,
        }));
    }

    return asContainerItems
      .filter(mi => !mi.isVisible || mi.isVisible(context))
      .map(mi => ({
        id: mi.id,
        label: mi.label ? t(mi.label) : undefined,
        icon: mi.icon,
        shortcutInfo: mi.shortcutInfo,
        type: mi.type,
        danger: mi.danger,
        disabled: mi.isDisabled ? mi.isDisabled(context) : false,
        action: mi.action ? () => mi.action?.(context) : undefined,
      }));
  };

  return { getContextMenuItems };
}
