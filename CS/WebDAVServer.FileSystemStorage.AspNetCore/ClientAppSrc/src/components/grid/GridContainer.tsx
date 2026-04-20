import { GridBody } from '@/components/grid/GridBody';
import { GridHeader } from '@/components/grid/GridHeader';
import { SkeletonGridContainer } from '@/components/grid/SkeletonGridContainer';
import type { HierarchyItem } from '@/domain/entities/HierarchyItem';
import type { FileBrowserViewModel } from '@/features/hooks/useFileBrowser';

interface Props {
  fileBrowser: FileBrowserViewModel;
  onRowContextMenu?: (item: HierarchyItem, event: globalThis.MouseEvent) => void;
}

export function GridContainer({ fileBrowser, onRowContextMenu }: Props) {
  return (
    <div className="flex flex-col flex-1 min-h-0 mt-2 mb-5 border-2 rounded-lg overflow-hidden border-transparent relative">
      <div className="bg-surface rounded-lg border border-border overflow-auto flex-1 min-h-0">
        <table className="min-w-full divide-y divide-border table-fixed">
          <GridHeader fileBrowser={fileBrowser} />
          {fileBrowser.loadingWithSkeleton ? (
            <SkeletonGridContainer />
          ) : (
            <GridBody fileBrowser={fileBrowser} onRowContextMenu={onRowContextMenu} />
          )}
        </table>
      </div>
    </div>
  );
}
