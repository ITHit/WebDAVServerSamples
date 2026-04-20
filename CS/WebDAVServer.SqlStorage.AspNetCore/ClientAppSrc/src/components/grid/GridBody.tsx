import { useEffect, useRef } from 'react';
import { GridRow } from '@/components/grid/GridRow';
import type { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { useRowToolbarItems } from '@/shared/composables/useRowToolbarItems';
import type { FileBrowserViewModel } from '@/features/hooks/useFileBrowser';

interface Props {
  fileBrowser: FileBrowserViewModel;
  onRowContextMenu?: (item: HierarchyItem, event: globalThis.MouseEvent) => void;
}

export function GridBody({ fileBrowser, onRowContextMenu }: Props) {
  const observerRef = useRef<HTMLTableRowElement | null>(null);
  const { getRowToolbarItems } = useRowToolbarItems(fileBrowser);

  useEffect(() => {
    if (!fileBrowser.hasNextPage || !observerRef.current) {
      return;
    }

    const observer = new IntersectionObserver(
      entries => {
        if (entries[0].isIntersecting && fileBrowser.hasNextPage) {
          void fileBrowser.nextPage();
        }
      },
      { threshold: 0 }
    );

    observer.observe(observerRef.current);

    return () => {
      observer.disconnect();
    };
  }, [fileBrowser, fileBrowser.hasNextPage]);

  useEffect(() => {
    if (fileBrowser.selectionAnchor === null) {
      return;
    }

    const activeRow = document.querySelector<HTMLTableRowElement>(
      `tr[data-index="${fileBrowser.selectionAnchor}"]`
    );
    activeRow?.scrollIntoView({ block: 'nearest' });
    activeRow?.focus({ preventScroll: true });

    if (fileBrowser.selectionAnchor === fileBrowser.items.length - 1 && fileBrowser.hasNextPage) {
      observerRef.current?.scrollIntoView({ block: 'nearest' });
    }
  }, [fileBrowser.hasNextPage, fileBrowser.items.length, fileBrowser.selectionAnchor]);

  return (
    <tbody className="bg-surface divide-y divide-border">
      {fileBrowser.items.map((item, index) => (
        <GridRow
          key={item.path}
          item={item}
          index={index}
          selected={fileBrowser.selectedIndexes.includes(index)}
          isSearchResult={fileBrowser.isSearchMode}
          fileBrowser={fileBrowser}
          getRowToolbarItems={getRowToolbarItems}
          onContextMenu={onRowContextMenu}
        />
      ))}

      {fileBrowser.hasNextPage ? (
        <tr ref={observerRef} className="h-px border-0" aria-hidden="true">
          <td colSpan={7} className="p-0" />
        </tr>
      ) : null}

      {fileBrowser.loadingNextPage ? (
        <tr>
          <td colSpan={7} className="py-4 text-center">
            <div className="icon icon-spinner text-primary" />
          </td>
        </tr>
      ) : null}
    </tbody>
  );
}
