import { useCallback, useEffect, useMemo, useRef, type MouseEvent } from 'react';
import { useLocation } from 'react-router-dom';
import { getAppServices } from '@/app/appServices';
import { setupUploader } from '@/app/composition/setupUploader';
import { appEventBus } from '@/app/events/appEventBus';
import { createAppShellServices } from '@/app/composition/createAppShellServices';
import { ContextMenu } from '@/components/context-menu/ContextMenu';
import { GridContainer } from '@/components/grid/GridContainer';
import { BreadcrumbsBar } from '@/components/grid/BreadcrumbsBar';
import { HotkeyInfo } from '@/components/grid/HotkeyInfo';
import { WebDavDriveButtons } from '@/components/grid/WebDavDriveButtons';
import { FlexibleToolbar } from '@/components/toolbar/FlexibleToolbar';
import { Uploader } from '@/components/upload/Uploader';
import type { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { UploadItemRow } from '@/features/models/uploadItemRow';
import type { Uploader as UploadController } from '@/features/models/uploader';
import type { FileBrowserViewModel } from '@/features/hooks/useFileBrowser';
import { useGridRouteOrchestration } from '@/features/hooks/useGridRouteOrchestration';
import { useHotkeys } from '@/features/hooks/useHotkeys';
import { useUpload } from '@/features/hooks/useUpload';
import { useWebSocket } from '@/features/hooks/useWebSocket';
import type { FileBrowserDomainEventPort } from '@/features/models/fileBrowserDomainEvents';
import { WebDavError } from '@/infrastructure/errors/webDavError';
import {
  getServerOrigin,
  getServerRootUrl,
  getServerUrl,
} from '@/infrastructure/services/webDavBaseUrl';
import { webSocketService } from '@/infrastructure/services/webSocketService';
import { isAnyContextMenuOpen, useContextMenu } from '@/shared/composables/useContextMenu';
import { useContextMenuItems } from '@/shared/composables/useContextMenuItems';
import { useDragDrop } from '@/shared/composables/useDragDrop';
import { isAnyModalOpen } from '@/shared/composables/useModalRegistry';
import { toast } from '@/shared/composables/useToast';
import {
  defaultContainerContextMenuItems,
  defaultContextMenuItems,
  defaultMultiContextMenuItems,
} from '@/shared/config/context-menu-config';
import { APP_EVENTS } from '@/shared/contracts/appEventBus';
import { t } from '@/shared/i18n/translate';
import { handleError as processError, logError } from '@/shared/utils/errorHandler';
import { FormatUtils } from '@/shared/utils/formatUtils';
import { decode, encodeUri } from '@/shared/utils/urlCodec';

interface Props {
  fileBrowser: FileBrowserViewModel;
}

export function InnerContainer({ fileBrowser }: Props) {
  const location = useLocation();
  const appServices = useMemo(() => getAppServices(), []);
  const services = useMemo(() => createAppShellServices(appServices), [appServices]);
  const upload = useUpload();
  const uploadRef = useRef(upload);
  uploadRef.current = upload;
  const contextMenu = useContextMenu();
  const { getContextMenuItems: getRowContextMenuItems } =
    useContextMenuItems(defaultContextMenuItems);
  const { getContextMenuItems: getMultiContextMenuItems } = useContextMenuItems(
    defaultMultiContextMenuItems
  );
  const { getContextMenuItems: getContainerContextMenuItems } = useContextMenuItems(
    defaultContainerContextMenuItems
  );
  const menuContext = useMemo(() => ({ fileBrowser }), [fileBrowser]);
  const onPathChangedHandlersRef = useRef(new Set<(newPath: string, oldPath: string) => void>());
  const previousPathRef = useRef(location.pathname);
  const uploaderRef = useRef<UploadController | null>(null);
  const pendingUploaderDestroyRef = useRef<ReturnType<typeof window.setTimeout> | null>(null);

  const handleUiError = useCallback((error: unknown) => {
    const appError = processError(error, key => key);
    logError(appError);
    toast.error(appError.userMessage);
  }, []);

  const logOnlyError = useCallback((error: unknown) => {
    logError(processError(error, key => key));
  }, []);

  const gridRouteOrchestrationDeps = useMemo(
    () => ({
      getServerRootUrl,
      getServerUrl,
      eventBus: appEventBus,
      reportError: handleUiError,
      reportEventBusError: (error: unknown) => {
        if (error instanceof WebDavError) {
          logOnlyError(error);
          return;
        }
        handleUiError(error);
      },
      logLocalizedError: logOnlyError,
    }),
    [handleUiError, logOnlyError]
  );

  useGridRouteOrchestration(fileBrowser, gridRouteOrchestrationDeps);

  useEffect(() => {
    if (pendingUploaderDestroyRef.current !== null) {
      clearTimeout(pendingUploaderDestroyRef.current);
      pendingUploaderDestroyRef.current = null;
    }

    if (uploaderRef.current === null) {
      onPathChangedHandlersRef.current.clear();

      const uploader = setupUploader(
        {
          get uploadItemRows() {
            return uploadRef.current.uploadItemRows;
          },
          get rewriteItemsData() {
            return uploadRef.current.rewriteItemsData;
          },
          get isDragging() {
            return uploadRef.current.isDragging;
          },
          get uploader() {
            return uploadRef.current.uploader;
          },
          addUploadItemRow: upload.addUploadItemRow,
          removeUploadItemRow: upload.removeUploadItemRow,
          setRewriteItemsData: upload.setRewriteItemsData,
          setIsDragging: upload.setIsDragging,
          setUploader: upload.setUploader,
          clearUploads: upload.clearUploads,
        },
        appServices,
        {
          eventBus: appEventBus,
          runtime: {
            getServerOrigin,
            getServerUrl,
            decode,
            encodeUri,
            onPathChange: handler => {
              onPathChangedHandlersRef.current.add(handler);
            },
          },
          createUploadItemRow: uploadItem => new UploadItemRow(uploadItem),
          validation: {
            validateName: name =>
              FormatUtils.validateName(name, 'Not allowed to contain following characters: {0}'),
          },
          errors: {
            createValidationError: (validationMessage, itemUrl) =>
              new WebDavError(`${validationMessage}\nUri:${itemUrl}`, null),
            createExistsCheckError: originalError =>
              new WebDavError('Failed to check file existence', originalError),
          },
        }
      );

      uploader.addDropzone();
      uploaderRef.current = uploader;
    }

    const pathChangedHandlers = onPathChangedHandlersRef.current;

    return () => {
      pendingUploaderDestroyRef.current = window.setTimeout(() => {
        uploaderRef.current?.destroy();
        uploaderRef.current = null;
        pathChangedHandlers.clear();
        pendingUploaderDestroyRef.current = null;
      }, 0);
    };
  }, [
    appServices,
    upload.addUploadItemRow,
    upload.removeUploadItemRow,
    upload.setRewriteItemsData,
    upload.setIsDragging,
    upload.setUploader,
    upload.clearUploads,
  ]);

  useEffect(() => {
    const oldPath = previousPathRef.current;
    const newPath = location.pathname;

    if (oldPath === newPath) {
      return;
    }

    onPathChangedHandlersRef.current.forEach(handler => {
      handler(newPath, oldPath);
    });
    previousPathRef.current = newPath;
  }, [location.pathname]);

  const { handleDragEnter, handleDragLeave, handleDrop } = useDragDrop(upload.setIsDragging);

  const domainEvents = useMemo<FileBrowserDomainEventPort>(
    () => ({
      onFolderRefreshRequested: () => {
        appEventBus.emit(APP_EVENTS.FOLDER_REFRESH_REQUESTED);
      },
      onItemUpdated: (fullPath: string) => {
        appEventBus.emit(APP_EVENTS.ITEM_UPDATED, fullPath);
      },
      onError: error => {
        appEventBus.emit(APP_EVENTS.ERROR_OCCURRED, error);
      },
    }),
    []
  );

  const currentFolderPathname = useMemo(() => {
    try {
      return new URL(fileBrowser.currentFolderPath).pathname;
    } catch {
      return window.location.pathname;
    }
  }, [fileBrowser.currentFolderPath]);

  useWebSocket(domainEvents, {
    webSocketService,
    getServerRootUrl,
    decode: value => decodeURIComponent(value),
    getPathname: () => currentFolderPathname,
    enabled: typeof WebSocket !== 'undefined' && import.meta.env.MODE !== 'test',
  });

  useHotkeys({
    fileBrowser,
    isSuspended: () => isAnyContextMenuOpen.value || isAnyModalOpen.value,
  });

  const handleRowContextMenu = (item: HierarchyItem, event: globalThis.MouseEvent) => {
    const selectedItems = fileBrowser.selectedItems;
    const isClickedItemSelected = selectedItems.some(s => s.path === item.path);
    const shouldUseMultiMenu = selectedItems.length > 1 && isClickedItemSelected;

    contextMenu.show(
      event,
      shouldUseMultiMenu
        ? getMultiContextMenuItems(item, menuContext)
        : getRowContextMenuItems(item, menuContext)
    );
  };

  const handleContainerContextMenu = (event: MouseEvent<HTMLElement>) => {
    const target = event.target as HTMLElement;
    if (target.closest('.context-menu-target')) {
      return;
    }

    contextMenu.show(event.nativeEvent, getContainerContextMenuItems(undefined, menuContext));
  };

  return (
    <div
      id="ithit-dropzone"
      className={[
        'relative flex flex-col flex-1 min-h-0',
        upload.isDragging ? 'dropzone' : '',
      ].join(' ')}
      onContextMenu={handleContainerContextMenu}
      onDragEnterCapture={handleDragEnter}
      onDragLeave={handleDragLeave}
      onDropCapture={handleDrop}
    >
      {upload.isDragging ? (
        <div className="absolute inset-0 z-50 pointer-events-none flex items-center justify-center rounded-md bg-primary/10 border-2 border-dashed border-primary">
          <div className="flex flex-col items-center gap-3 text-primary select-none">
            <i className="icon icon-upload-items" style={{ width: '3rem', height: '3rem' }} />
            <span className="text-lg font-semibold">{t('phrases.dropzone.dropHere')}</span>
          </div>
        </div>
      ) : null}

      <div className="flex justify-between items-center">
        <BreadcrumbsBar fileBrowser={fileBrowser} />
        <div className="flex gap-5 py-2 items-center">
          <WebDavDriveButtons
            currentFolderPath={fileBrowser.currentFolderPath}
            appShellServices={services}
          />
          <HotkeyInfo />
        </div>
      </div>

      <div className="flex-none">
        <FlexibleToolbar fileBrowser={fileBrowser} />
      </div>

      <Uploader upload={upload} />
      <GridContainer fileBrowser={fileBrowser} onRowContextMenu={handleRowContextMenu} />

      <input id="ithit-hidden-input" className="hidden" type="file" multiple />

      <ContextMenu
        items={contextMenu.state.items}
        x={contextMenu.state.x}
        y={contextMenu.state.y}
        isVisible={contextMenu.state.isVisible}
        onClose={contextMenu.hide}
        onSelect={() => contextMenu.hide()}
      />
    </div>
  );
}
