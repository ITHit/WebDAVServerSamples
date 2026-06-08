import { useEffect, useMemo, useRef, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { ItemBreadcrumbs } from '@/components/search/ItemBreadcrumbs';
import { isFolderItem, type HierarchyItem } from '@/domain/entities/HierarchyItem';
import type { FileBrowserViewModel } from '@/features/hooks/useFileBrowser';
import { getAppPathFromServerUrl } from '@/infrastructure/services/webDavBaseUrl';
import { t } from '@/shared/i18n/translate';
import { toast } from '@/shared/composables/useToast';
import { handleError as processError, logError } from '@/shared/utils/errorHandler';

interface Props {
  fileBrowser: FileBrowserViewModel;
}

export function Search({ fileBrowser }: Props) {
  const location = useLocation();
  const navigate = useNavigate();
  const searchParam = useMemo(
    () => new URLSearchParams(location.search).get('search') ?? '',
    [location.search]
  );
  const [query, setQuery] = useState(searchParam);
  const [searchResults, setSearchResults] = useState<HierarchyItem[]>([]);
  const [mouseOverMenuDisplayed, setMouseOverMenuDisplayed] = useState(false);
  const [isFocusInput, setIsFocusInput] = useState(false);
  const [isMobileSearchOpen, setIsMobileSearchOpen] = useState(
    searchParam.length > 0 && window.innerWidth < 1024
  );
  const mobileInputRef = useRef<HTMLInputElement | null>(null);
  const previousFolderPathRef = useRef(fileBrowser.currentFolderPath);

  const showMenu = searchResults.length > 0 && (mouseOverMenuDisplayed || isFocusInput);
  const isDisabled =
    !fileBrowser.optionsInfoLoading && !fileBrowser.serverCapabilities.supportsSearch;

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setQuery(previous => (previous === searchParam ? previous : searchParam));
      if (!searchParam) {
        setSearchResults([]);
      }
    }, 0);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [searchParam]);

  useEffect(() => {
    const previousFolderPath = previousFolderPathRef.current;
    previousFolderPathRef.current = fileBrowser.currentFolderPath;

    if (previousFolderPath && previousFolderPath !== fileBrowser.currentFolderPath) {
      const timeoutId = window.setTimeout(() => {
        if (query) {
          setQuery('');
          setSearchResults([]);
        }
        setIsMobileSearchOpen(false);
        setIsFocusInput(false);
        setMouseOverMenuDisplayed(false);
      }, 0);

      return () => {
        window.clearTimeout(timeoutId);
      };
    }
  }, [fileBrowser.currentFolderPath, query]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      if (!query.trim()) {
        setSearchResults([]);
        return;
      }

      void fileBrowser
        .searchSuggestions(query)
        .then(results => {
          setSearchResults(results);
        })
        .catch(error => {
          const appError = processError(error, t);
          logError(appError);
          toast.error(appError.userMessage);
          setSearchResults([]);
        });
    }, 400);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [fileBrowser, query]);

  useEffect(() => {
    if (isMobileSearchOpen) {
      queueMicrotask(() => {
        mobileInputRef.current?.focus();
      });
    }
  }, [isMobileSearchOpen]);

  useEffect(() => {
    const handleEsc = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && isMobileSearchOpen) {
        setIsMobileSearchOpen(false);
        setIsFocusInput(false);
        setMouseOverMenuDisplayed(false);
      }
    };

    const handleResize = () => {
      if (window.innerWidth >= 1024 && isMobileSearchOpen) {
        setIsMobileSearchOpen(false);
        setIsFocusInput(false);
        setMouseOverMenuDisplayed(false);
      }
    };

    window.addEventListener('keydown', handleEsc);
    window.addEventListener('resize', handleResize);

    return () => {
      window.removeEventListener('keydown', handleEsc);
      window.removeEventListener('resize', handleResize);
    };
  }, [isMobileSearchOpen]);

  const closeMobileSearch = () => {
    setIsMobileSearchOpen(false);
    setIsFocusInput(false);
    setMouseOverMenuDisplayed(false);
  };

  const commitSearch = () => {
    const trimmedQuery = query.trim();
    if (!trimmedQuery) {
      return;
    }

    const nextParams = new URLSearchParams(location.search);
    nextParams.set('search', trimmedQuery);
    setSearchResults([]);
    closeMobileSearch();
    navigate({ pathname: location.pathname, search: `?${nextParams.toString()}` });
  };

  const clearSearch = () => {
    setQuery('');
    setSearchResults([]);
    closeMobileSearch();
    const nextParams = new URLSearchParams(location.search);
    nextParams.delete('search');
    const search = nextParams.toString();
    navigate({ pathname: location.pathname, search: search ? `?${search}` : '' });
    void fileBrowser.clearSearch();
  };

  const selectSearchedItem = (item: HierarchyItem) => {
    setQuery(item.name);
    setMouseOverMenuDisplayed(false);

    if (isFolderItem(item)) {
      navigate({ pathname: getAppPathFromServerUrl(item.path) });
    }
  };

  return (
    <div className="relative flex items-center justify-end">
      {!isMobileSearchOpen ? (
        <button
          type="button"
          className="flex lg:hidden items-center justify-center w-9 h-9 rounded hover:bg-surface-hover transition-colors"
          aria-label={t('phrases.searchPlaceholder')}
          onClick={() => setIsMobileSearchOpen(true)}
        >
          <i className="icon icon-search w-5 h-5 block align-middle" />
        </button>
      ) : null}

      <div
        className={[
          'lg:block lg:static lg:w-full lg:pt-1 lg:pb-1',
          isMobileSearchOpen
            ? 'block fixed inset-0 z-100 bottom-auto bg-surface px-4 py-2.5'
            : 'hidden',
        ].join(' ')}
      >
        <div className="relative">
          <input
            ref={mobileInputRef}
            type="text"
            value={query}
            disabled={isDisabled}
            placeholder={
              isDisabled && !fileBrowser.optionsInfoLoading
                ? t('phrases.validations.notSupportSearch')
                : t('phrases.searchPlaceholder')
            }
            className="w-full bg-surface px-4 py-1.5 pr-20 border-input rounded-lg bg-input placeholder:text-muted focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200"
            onChange={event => setQuery(event.target.value)}
            onKeyDown={event => {
              if (event.key === 'Enter') {
                event.preventDefault();
                commitSearch();
              }
            }}
            onFocus={() => setIsFocusInput(true)}
            onBlur={() => setIsFocusInput(false)}
          />

          {query.length > 0 || isMobileSearchOpen ? (
            <button
              type="button"
              className="absolute right-2 top-1/2 -translate-y-1/2 p-1.5 text-muted hover:text-secondary hover:bg-surface-hover rounded-md transition-colors duration-150"
              onClick={clearSearch}
            >
              <i className="icon icon-close w-4 h-4 block align-middle" />
            </button>
          ) : null}
        </div>

        {showMenu ? (
          <div
            className="bg-surface border border-border rounded-lg shadow-lg overflow-y-auto mt-3 max-h-[calc(100vh-90px)] lg:absolute lg:z-50 lg:w-full lg:mt-2 lg:max-h-100"
            onMouseEnter={() => setMouseOverMenuDisplayed(true)}
            onMouseLeave={() => setMouseOverMenuDisplayed(false)}
          >
            <div className="py-1">
              {searchResults.map((item, index) => (
                <div
                  key={`${item.path}-${index}`}
                  className="px-4 py-3 cursor-pointer hover:bg-surface-hover border-b border-border-light last:border-b-0 transition-colors duration-150"
                  onMouseDown={event => event.preventDefault()}
                  onClick={() => selectSearchedItem(item)}
                >
                  <div className="flex items-center gap-2 mb-1">
                    <i
                      className={[
                        'icon shrink-0 w-4 h-4',
                        isFolderItem(item) ? 'icon-folder text-warning' : 'icon-file text-muted',
                      ].join(' ')}
                    />
                    <span className="font-medium text-foreground truncate">{item.name}</span>
                  </div>
                  <ItemBreadcrumbs item={item} />
                </div>
              ))}
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}
