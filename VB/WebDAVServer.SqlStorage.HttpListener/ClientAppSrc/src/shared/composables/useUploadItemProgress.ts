import { useEffect, useState } from 'react';
import type { UploadItemRow } from '@/features/models/uploadItemRow';

export function useUploadItemProgress(uploadItemRow: UploadItemRow, intervalMs = 500) {
  const [progress, setProgress] = useState(0);
  const [speed, setSpeed] = useState(0);

  useEffect(() => {
    const id = setInterval(() => {
      const p = uploadItemRow.uploadItem.GetProgress();
      setProgress(p.Completed);
      setSpeed(p.Speed);
    }, intervalMs);

    return () => {
      clearInterval(id);
    };
  }, [intervalMs, uploadItemRow]);

  return { progress, speed };
}
