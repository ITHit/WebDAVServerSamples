import type { MouseEvent } from 'react';
import { isFileItem, isFolderItem, type HierarchyItem } from '@/domain/entities/HierarchyItem';
import { ItemBreadcrumbs } from '@/components/search/ItemBreadcrumbs';
import { FormatUtils } from '@/shared/utils/formatUtils';
import type { ResolvedRowToolbarItem } from '@/shared/config/config-types';
import type { FileBrowserViewModel } from '@/features/hooks/useFileBrowser';
import { RowToolbar } from '@/components/toolbar/RowToolbar';
import { t } from '@/shared/i18n/translate';

interface Props {
  item: HierarchyItem;
  index: number;
  selected: boolean;
  isSearchResult?: boolean;
  fileBrowser: FileBrowserViewModel;
  getRowToolbarItems?: (item: HierarchyItem) => ResolvedRowToolbarItem[];
  onContextMenu?: (item: HierarchyItem, event: globalThis.MouseEvent) => void;
}

export function GridRow({
  item,
  index,
  selected,
  isSearchResult,
  fileBrowser,
  getRowToolbarItems,
  onContextMenu,
}: Props) {
  const resolvedToolbarItems = getRowToolbarItems?.(item) ?? [];
  const isFolder = isFolderItem(item);
  const fileSize = isFileItem(item) ? FormatUtils.formatFileSize(item.size) : '-';
  const formattedDate = new Date(item.modifiedAt).toLocaleString();

  const handleRowClick = (event: MouseEvent<HTMLTableRowElement>) => {
    if (event.shiftKey) {
      fileBrowser.setRangeFromAnchor(index);
      return;
    }

    if (event.ctrlKey || event.metaKey) {
      fileBrowser.toggleSelection(index);
      return;
    }

    fileBrowser.selectSingle(index);
  };

  const handleDoubleClick = () => {
    if (isFolder) {
      void fileBrowser.loadFolder(item.path);
      return;
    }

    fileBrowser.editItem(item);
  };

  const handleContextMenu = (event: MouseEvent<HTMLTableRowElement>) => {
    event.preventDefault();

    // Keep existing multi-selection when right-clicking one of the selected rows.
    if (!selected) {
      fileBrowser.selectSingle(index);
    }

    onContextMenu?.(item, event.nativeEvent);
  };

  return (
    <tr
      className={[
        'group cursor-pointer hover:bg-surface-hover transition-colors context-menu-target',
        selected ? 'bg-surface-active' : '',
      ].join(' ')}
      data-index={index}
      tabIndex={-1}
      onClick={handleRowClick}
      onDoubleClick={handleDoubleClick}
      onContextMenu={handleContextMenu}
    >
      <td className="w-10 px-4 py-1 text-right">
        <input
          type="checkbox"
          checked={selected}
          className="w-4 h-4 rounded border-input text-primary focus:ring-primary cursor-pointer align-middle"
          onChange={() => fileBrowser.toggleSelection(index)}
          onClick={event => event.stopPropagation()}
        />
      </td>

      <td className="w-10 px-2 py-1">
        <div className="relative">
          <i
            className={[
              'icon text-muted align-middle',
              isFolder
                ? 'icon-folder w-5 h-5 -mt-1'
                : `icon-file icon-file-${FormatUtils.getFileExtension(item.name).toLowerCase()} w-5 h-5`,
            ].join(' ')}
          />
          {item.locks.length > 0 ? (
            <i className="icon icon-lock w-3 h-3 absolute -bottom-1 -right-0.75" />
          ) : null}
        </div>
      </td>

      <td className="px-4 py-1 max-w-0">
        <div className="flex items-center gap-0.5">
          <div className="min-w-0 flex-1 overflow-hidden">
            <span
              className="inline-block align-middle max-w-full truncate font-medium text-foreground hover:underline cursor-pointer"
              title={item.name}
              onClick={() => handleDoubleClick()}
            >
              {item.name}
            </span>
            {isSearchResult ? <ItemBreadcrumbs item={item} className="mt-0.5" /> : null}
          </div>

          <RowToolbar items={resolvedToolbarItems} />
        </div>
      </td>

      <td className="hidden xl:table-cell w-32 px-4 py-1 text-sm text-secondary">
        {isFolder
          ? t('phrases.grid.folder')
          : `${t('phrases.grid.file')} ${FormatUtils.getFileExtension(item.name)}`}
      </td>

      <td className="hidden xl:table-cell w-40 px-4 py-1 text-sm text-secondary">{fileSize}</td>

      <td className="hidden lg:table-cell w-60 px-4 py-1 text-sm text-secondary">
        {formattedDate}
      </td>

      <td className="px-4 py-1 text-right">
        <button
          type="button"
          className="p-1.5 rounded hover:bg-surface-hover text-muted"
          onClick={event => {
            event.stopPropagation();
            handleContextMenu(event as unknown as MouseEvent<HTMLTableRowElement>);
          }}
        >
          ⋮
        </button>
      </td>
    </tr>
  );
}
