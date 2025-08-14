import { useCallback } from "react";
import { ITHit } from "webdav.client";
import { UrlResolveService } from "../../services/UrlResolveService";
import { WebDavSettings } from "../../webDavSettings";
import { useAppDispatch } from "./common";
import { showProtocolModal } from "../../features/grid/gridSlice";

export const useOpenFolderInFileManagerClick = () => {
  const dispatch = useAppDispatch();

  const showProtocolInstallModal = useCallback(() => {
    dispatch(showProtocolModal());
  }, [dispatch]);

  const handleOpenFolderInFileManagerClick = useCallback((folderHref?: string) => {
    if (folderHref) {
      ITHit.WebDAV.Client.DocManager.OpenFolderInOsFileManager(
        folderHref,
        UrlResolveService.getRootUrl(),
        showProtocolInstallModal,
        null,
        WebDavSettings.EditDocAuth.SearchIn,
        WebDavSettings.EditDocAuth.CookieNames,
        WebDavSettings.EditDocAuth.LoginUrl
      );
    }
  }, [showProtocolInstallModal]);

  return { handleOpenFolderInFileManagerClick };
};
