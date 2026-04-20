import { useMemo } from 'react';
import type { FileBrowserContext, ResolvedToolbarButton, ToolbarButtonConfig } from '@/shared/config/config-types';
import { defaultToolbarButtons } from '@/shared/config/toolbar-config';
import type { FileBrowserContract } from '@/shared/contracts/fileBrowserContract';
import { t } from '@/shared/i18n/translate';

export function useToolbar(
  fileBrowser: FileBrowserContract,
  options?: { toolbarButtons?: ToolbarButtonConfig[] }
) {
  const context: FileBrowserContext = useMemo(
    () => ({ fileBrowser }),
    [fileBrowser]
  );

  const buttons = options?.toolbarButtons ?? defaultToolbarButtons;

  const toolbarButtons = useMemo<ResolvedToolbarButton[]>(() => {
    return buttons
      .filter(btn => !btn.isVisible || btn.isVisible(context))
      .map(btn => ({
        ...btn,
        label: {
          ...btn.label,
          text: t(btn.label.text),
        },
        disabled: btn.isDisabled ? btn.isDisabled(context) : false,
      }));
  }, [buttons, context]);

  const executeToolbarAction = async (button: ToolbarButtonConfig) => {
    try {
      await button.action(context);
    } catch (error) {
      console.error(`Error executing toolbar action ${button.id}:`, error);
      throw error;
    }
  };

  return { toolbarButtons, executeToolbarAction };
}
