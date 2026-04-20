import { BaseToolbarButton } from '@/components/toolbar/BaseToolbarButton';
import { useToolbar } from '@/shared/composables/useToolbar';
import type { ToolbarButtonConfig } from '@/shared/config/config-types';
import type { FileBrowserContract } from '@/shared/contracts/fileBrowserContract';

interface Props {
  fileBrowser: FileBrowserContract;
  toolbarButtons?: ToolbarButtonConfig[];
}

export function FlexibleToolbar({ fileBrowser, toolbarButtons }: Props) {
  const { toolbarButtons: resolvedButtons, executeToolbarAction } = useToolbar(fileBrowser, {
    toolbarButtons,
  });

  return (
    <div className="flex flex-wrap items-center gap-4 w-full px-4 py-2">
      {resolvedButtons.map(button => (
        <div key={button.id} className="w-auto">
          <BaseToolbarButton
            button={button}
            onClick={() => {
              void executeToolbarAction(button);
            }}
          />
        </div>
      ))}
    </div>
  );
}
