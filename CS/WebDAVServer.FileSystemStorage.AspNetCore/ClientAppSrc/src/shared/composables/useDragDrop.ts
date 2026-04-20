import { useCallback, useRef } from 'react';

export function useDragDrop(onDraggingChange: (isDragging: boolean) => void) {
  const dropCounterRef = useRef(0);

  const handleDragEnter = useCallback(() => {
    dropCounterRef.current += 1;
    onDraggingChange(true);
  }, [onDraggingChange]);

  const handleDragLeave = useCallback(() => {
    dropCounterRef.current -= 1;
    if (dropCounterRef.current <= 0) {
      dropCounterRef.current = 0;
      onDraggingChange(false);
    }
  }, [onDraggingChange]);

  const handleDrop = useCallback(() => {
    dropCounterRef.current = 0;
    onDraggingChange(false);
  }, [onDraggingChange]);

  return { handleDragEnter, handleDragLeave, handleDrop };
}
