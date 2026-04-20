import type { ResolvedToolbarButton } from '@/shared/config/config-types';
import { t } from '@/shared/i18n/translate';

interface Props {
  button: ResolvedToolbarButton;
  onClick: () => void | Promise<void>;
}

export function BaseToolbarButton({ button, onClick }: Props) {
  if (button.inputFor) {
    return (
      <label
        id={button.id}
        htmlFor={button.inputFor}
        data-toolbar-button={button.id}
        className={[
          'btn-tool flex opacity-100 cursor-pointer items-center',
          button.disabled ? 'opacity-50 cursor-not-allowed pointer-events-none' : '',
        ].join(' ')}
        title={t(button.label.text)}
      >
        <i className={`icon ${button.icon}`} />
        {button.label.show ? (
          <span className={['ml-2 pt-1 text-primary-icon', button.label.class ?? ''].join(' ')}>
            {t(button.label.text)}
          </span>
        ) : null}
      </label>
    );
  }

  return (
    <button
      id={button.id}
      type="button"
      data-toolbar-button={button.id}
      className="btn-tool flex opacity-100 disabled:opacity-50 cursor-pointer disabled:cursor-not-allowed items-center"
      title={t(button.label.text)}
      disabled={button.disabled}
      onClick={() => {
        void onClick();
      }}
    >
      <i className={`icon ${button.icon}`} />
      {button.label.show ? (
        <span className={['ml-2 pt-1 text-primary-icon', button.label.class ?? ''].join(' ')}>
          {t(button.label.text)}
        </span>
      ) : null}
    </button>
  );
}
