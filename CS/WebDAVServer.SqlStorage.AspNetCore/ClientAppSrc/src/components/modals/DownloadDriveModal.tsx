import { DefaultModal } from '@/components/modals/DefaultModal';
import { modalFormStyles } from '@/components/modals/modalFormStyles';
import type { ModalComponentProps } from '@/shared/composables/useModalRegistry';
import { t } from '@/shared/i18n/translate';

interface Props extends ModalComponentProps<void> {
  appName?: string;
  serverRootUrl: string;
}

function getAssetUrl(serverRootUrl: string, fileName: string): string {
  const normalized = serverRootUrl.endsWith('/') ? serverRootUrl.slice(0, -1) : serverRootUrl;
  return `${normalized}/${fileName}`;
}

export function DownloadDriveModal({ onClose, appName, serverRootUrl }: Props) {
  const resolvedAppName = appName ?? 'WebDAV Drive';

  return (
    <DefaultModal
      title={t('phrases.downloadDrive.downloadTitle', { appName: resolvedAppName })}
      closeModal={onClose}
    >
      <div className={modalFormStyles.body}>
        <div
          className="bg-warning-subtle border border-warning text-warning px-4 py-3 rounded mb-4"
          role="alert"
        >
          <span>
            {t('phrases.downloadDrive.signedWithCertificate', { appName: resolvedAppName })}
          </span>
        </div>

        <div className="mb-4 space-y-3">
          <div>
            <h6 className="font-semibold">{t('phrases.downloadDrive.forTesting')}</h6>
            <p className="text-secondary">{t('phrases.downloadDrive.forTestingDescription')}</p>
          </div>

          <div>
            <h6 className="font-semibold">{t('phrases.downloadDrive.forProduction')}</h6>
            <p className="text-secondary">{t('phrases.downloadDrive.forProductionDescription')}</p>
          </div>
        </div>
      </div>

      <div className={[modalFormStyles.footer, 'justify-center'].join(' ')}>
        <a
          href={getAssetUrl(serverRootUrl, 'WebDAVDrive.zip')}
          className={modalFormStyles.buttonPrimary}
        >
          {t('phrases.downloadDrive.downloadZipForTesting')}
        </a>
        <a
          href={getAssetUrl(serverRootUrl, 'WebDAVDrive.msix')}
          className={modalFormStyles.buttonPrimary}
        >
          {t('phrases.downloadDrive.downloadMsixForProduction')}
        </a>
      </div>
    </DefaultModal>
  );
}
