import type { PropsWithChildren } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import type { FileBrowserViewModel } from '@/features/hooks/useFileBrowser';

interface Props extends PropsWithChildren {
  className: string;
  fieldName: string;
  fileBrowser: FileBrowserViewModel;
}

export function TableSortHeader({ className, fieldName, fileBrowser, children }: Props) {
  const location = useLocation();
  const navigate = useNavigate();
  const isCurrentSort = fileBrowser.sortColumn === fieldName;
  const stateClass = isCurrentSort ? (fileBrowser.sortAscending ? 'ascending' : 'descending') : '';

  const handleClick = () => {
    const newAscending = isCurrentSort ? !fileBrowser.sortAscending : true;

    // Apply sort immediately so UI/data respond even before route effects process query changes.
    fileBrowser.setSort(fieldName, newAscending);
    void fileBrowser.refresh();

    const nextParams = new URLSearchParams(location.search);
    nextParams.set('sortcolumn', fieldName);
    nextParams.set('sortascending', String(newAscending));

    navigate({ pathname: location.pathname, search: `?${nextParams.toString()}` });
  };

  return (
    <th className={className} scope="col" onClick={handleClick}>
      <div
        className={['sort cursor-pointer hover:text-foreground w-fit', stateClass].join(' ').trim()}
      >
        {children}
      </div>
    </th>
  );
}
