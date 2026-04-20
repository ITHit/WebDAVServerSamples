import type { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { getAppPathFromServerUrl } from '@/infrastructure/services/webDavBaseUrl';

interface Props {
  item: HierarchyItem;
  className?: string;
}

type BreadcrumbItem = {
  label: string;
  path: string;
};

function buildItemBreadcrumbs(itemPath: string): BreadcrumbItem[] {
  const appPath = getAppPathFromServerUrl(itemPath);
  const segments = appPath.split('/').filter(Boolean);

  const breadcrumbs: BreadcrumbItem[] = [{ label: '', path: '/' }];
  let accumulatedPath = '';

  segments.forEach(segment => {
    accumulatedPath += `/${segment}`;
    breadcrumbs.push({
      label: decodeURIComponent(segment),
      path: accumulatedPath,
    });
  });

  return breadcrumbs;
}

export function ItemBreadcrumbs({ item, className }: Props) {
  const breadcrumbs = buildItemBreadcrumbs(item.path);

  return (
    <nav
      className={[
        'flex items-center flex-wrap gap-1 text-xs text-muted min-w-0',
        className ?? '',
      ].join(' ')}
    >
      {breadcrumbs.map((crumb, index) => (
        <span key={`${crumb.path}-${index}`} className="contents">
          {index !== 0 && index < breadcrumbs.length - 1 ? (
            <span className="truncate hover:underline text-muted">{crumb.label}</span>
          ) : index === breadcrumbs.length - 1 ? (
            <span className="truncate font-medium text-foreground">{crumb.label}</span>
          ) : null}
          {index < breadcrumbs.length - 1 ? <span className="text-muted shrink-0">/</span> : null}
        </span>
      ))}
    </nav>
  );
}
