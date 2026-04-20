import { useCallback, useEffect, useState } from 'react';
import type { ResolvedContextMenuItem } from '@/shared/config/config-types';

export interface ContextMenuState {
  isVisible: boolean;
  x: number;
  y: number;
  items: ResolvedContextMenuItem[];
}

export interface UseContextMenuReturn {
  state: ContextMenuState;
  show: (event: MouseEvent, items: ResolvedContextMenuItem[]) => void;
  hide: () => void;
}

/** Module-level flag — true while any context menu is visible (read by useHotkeys). */
let _isAnyContextMenuOpen = false;
export const isAnyContextMenuOpen = {
  get value() {
    return _isAnyContextMenuOpen;
  },
};

export function useContextMenu(): UseContextMenuReturn {
  // Clear stale global state when a new owner mounts.
  useEffect(() => {
    _isAnyContextMenuOpen = false;

    return () => {
      _isAnyContextMenuOpen = false;
    };
  }, []);

  const [state, setState] = useState<ContextMenuState>({
    isVisible: false,
    x: 0,
    y: 0,
    items: [],
  });

  const show = useCallback((event: MouseEvent, items: ResolvedContextMenuItem[]) => {
    event.preventDefault();
    event.stopPropagation();
    _isAnyContextMenuOpen = true;
    setState({ isVisible: true, x: event.clientX, y: event.clientY, items });
  }, []);

  const hide = useCallback(() => {
    _isAnyContextMenuOpen = false;
    setState(prev => ({ ...prev, isVisible: false }));
  }, []);

  return { state, show, hide };
}
