import { useState } from 'react';
import { resolveWebDavProtocol } from '@/app/protocol/resolveWebDavProtocol';
import { DefaultModal } from '@/components/modals/DefaultModal';
import { modalFormStyles } from '@/components/modals/modalFormStyles';
import type { ModalComponentProps } from '@/shared/composables/useModalRegistry';
import { t } from '@/shared/i18n/translate';

export function InstallProtocolModal({ onClose }: ModalComponentProps<void>) {
  const webDavProtocol = resolveWebDavProtocol();
  const [moreOsDisplayed, setMoreOsDisplayed] = useState(false);
  const [moreBrowsersDisplayed, setMoreBrowsersDisplayed] = useState(false);

  return (
    <DefaultModal
      title={t('phrases.modals.downloadProtocolTitle')}
      dialogClassName="modal-lg"
      closeModal={onClose}
    >
      <div className={modalFormStyles.body}>
        <div className="w-full px-1">
          <p className="mb-3">{t('phrases.downloadProtocol.installCustomProtocol')}</p>
          <ol className="list-decimal list-inside space-y-5">
            <li>
              <span>{t('phrases.downloadProtocol.downloadAndInstallFiles')}:</span>
              <div className="mt-2">
                {webDavProtocol.currentOs ? (
                  <span className="block mb-1">
                    <span>{webDavProtocol.currentOs.name}</span>
                    <a
                      className="text-primary hover:underline"
                      target="_blank"
                      href={webDavProtocol.currentOs.downloadLink}
                      rel="noreferrer"
                    >
                      {webDavProtocol.currentOs.fileName}
                    </a>
                  </span>
                ) : null}

                <button
                  type="button"
                  className="text-primary hover:underline"
                  onClick={() => setMoreOsDisplayed(value => !value)}
                >
                  <span>{moreOsDisplayed ? '- ' : '+ '}</span>
                  {t('phrases.otherOs')}:
                </button>

                {moreOsDisplayed ? (
                  <div className="mt-2 space-y-1">
                    {webDavProtocol.otherOs.map((item, index) => (
                      <span key={`os-${index}`} className="block">
                        <span>{item.name}</span>
                        <a
                          className="text-primary hover:underline"
                          target="_blank"
                          href={item.downloadLink}
                          rel="noreferrer"
                        >
                          {item.fileName}
                        </a>
                      </span>
                    ))}
                  </div>
                ) : null}
              </div>
            </li>

            <li>
              <span>{t('phrases.downloadProtocol.enableITHitEditDocumentOpener')}:</span>
              <div className="mt-2">
                {webDavProtocol.currentBrowser ? (
                  <span className="block mb-1">
                    <span>{webDavProtocol.currentBrowser.name}</span>
                    {webDavProtocol.currentBrowser.downloadLink ? (
                      <a
                        className="text-primary hover:underline"
                        target="_blank"
                        href={webDavProtocol.currentBrowser.downloadLink}
                        rel="noreferrer"
                      >
                        {webDavProtocol.currentBrowser.fileName}
                      </a>
                    ) : null}
                  </span>
                ) : null}

                <button
                  type="button"
                  className="text-primary hover:underline"
                  onClick={() => setMoreBrowsersDisplayed(value => !value)}
                >
                  <span>{moreBrowsersDisplayed ? '- ' : '+ '}</span>
                  {t('phrases.otherWebBrowsers')}:
                </button>

                {moreBrowsersDisplayed ? (
                  <div className="mt-2 space-y-1">
                    {webDavProtocol.otherBrowsers.map((item, index) => (
                      <span key={`browser-${index}`} className="block">
                        <a
                          className="text-primary hover:underline"
                          target="_blank"
                          href={item.downloadLink}
                          rel="noreferrer"
                        >
                          {item.fileName}
                        </a>
                      </span>
                    ))}
                  </div>
                ) : null}
              </div>
            </li>
          </ol>

          <p className="mt-4">
            {t('phrases.see')}{' '}
            <a
              href="https://www.webdavsystem.com/ajax/programming/open-doc-webpage/install/web_browser_extensions/"
              target="_blank"
              rel="noreferrer"
              className="text-primary hover:underline"
            >
              {t('phrases.downloadProtocol.installAndVerifyExtensions')}
            </a>
            .
          </p>
        </div>
      </div>

      <form
        onSubmit={event => {
          event.preventDefault();
          onClose();
        }}
      >
        <div className={modalFormStyles.footer}>
          <button type="submit" className={modalFormStyles.buttonSecondary}>
            {t('phrases.close')}
          </button>
        </div>
      </form>
    </DefaultModal>
  );
}
