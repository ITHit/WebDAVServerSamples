import { TableSortHeader } from '@/components/grid/TableSortHeader';
import type { FileBrowserViewModel } from '@/features/hooks/useFileBrowser';
import { t } from '@/shared/i18n/translate';

interface Props {
  fileBrowser: FileBrowserViewModel;
}

export function GridHeader({ fileBrowser }: Props) {
  return (
    <thead className="bg-surface-secondary sticky top-0 z-10">
      <tr className="thead-row">
        <th className="w-10 px-4 py-2 text-right">
          <label className="custom-checkbox">
            <input
              type="checkbox"
              checked={fileBrowser.isAllSelected}
              className="w-4 h-4 rounded border-input text-primary focus:ring-primary align-middle"
              onChange={() => fileBrowser.toggleSelectAll()}
            />
          </label>
        </th>

        <th className="w-10 px-2 py-2" />

        <TableSortHeader
          className="px-4 py-2 text-left"
          fieldName="displayname"
          fileBrowser={fileBrowser}
        >
          <span>{t('phrases.grid.tableHeader.displayName')}</span>
        </TableSortHeader>

        <TableSortHeader
          className="hidden xl:table-cell w-32 px-4 py-2 text-left"
          fieldName="getcontenttype"
          fileBrowser={fileBrowser}
        >
          <span>{t('phrases.grid.tableHeader.type')}</span>
        </TableSortHeader>

        <TableSortHeader
          className="hidden xl:table-cell w-40 px-4 py-2 text-left"
          fieldName="quota-used-bytes"
          fileBrowser={fileBrowser}
        >
          <span>{t('phrases.grid.tableHeader.size')}</span>
        </TableSortHeader>

        <TableSortHeader
          className="hidden lg:table-cell w-60 px-4 py-2 text-left"
          fieldName="getlastmodified"
          fileBrowser={fileBrowser}
        >
          <span>{t('phrases.grid.tableHeader.modified')}</span>
        </TableSortHeader>

        <th className="w-4 px-4 py-2" />
      </tr>
    </thead>
  );
}
