import { useNavigate } from 'react-router-dom';
import type { FileBrowserViewModel } from '@/features/hooks/useFileBrowser';
import { t } from '@/shared/i18n/translate';

interface Props {
  fileBrowser: FileBrowserViewModel;
}

export function BreadcrumbsBar({ fileBrowser }: Props) {
  const navigate = useNavigate();
  const { breadcrumbs } = fileBrowser;

  const canGoUp = breadcrumbs.length >= 2;

  const goUpOneLevel = () => {
    if (breadcrumbs.length >= 2) {
      const parent = breadcrumbs[breadcrumbs.length - 2];
      navigate(parent.path || '/');
    }
  };

  const navigateTo = (crumb: { path: string }) => {
    const normalizedPath = crumb.path === '/' ? '/' : `${crumb.path}/`;
    navigate(normalizedPath);
  };

  return (
    <div className="flex items-center gap-2 pt-2.5 pb-1 mx-4">
      <button
        type="button"
        title={t('phrases.breadcrumb.upOneLevelTitle')}
        className="mr-3 disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
        disabled={!canGoUp}
        onClick={goUpOneLevel}
      >
        <i className="icon icon-up-one-level" />
      </button>

      <nav className="flex items-center flex-wrap gap-1 text-sm min-w-0">
        {breadcrumbs.map((crumb, index) => (
          <span key={crumb.path} className="flex items-center gap-1">
            {index < breadcrumbs.length - 1 ? (
              <>
                <button
                  type="button"
                  onClick={() => navigateTo(crumb)}
                  className="text-secondary hover:text-foreground hover:underline transition-colors cursor-pointer"
                >
                  {index === 0 ? <i className="icon icon-home mr-1" /> : null}
                  {crumb.label}
                </button>
                <span className="text-muted">/</span>
              </>
            ) : (
              <span className="font-semibold text-foreground">
                {index === 0 ? <i className="icon icon-home mr-1" /> : null}
                {crumb.label}
              </span>
            )}
          </span>
        ))}
      </nav>
    </div>
  );
}
