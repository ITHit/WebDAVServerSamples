import { useState, useEffect, useMemo } from "react";
import { CommonService } from "../../services/CommonService";

/**
 * Custom hook for refreshing date display at regular intervals
 * @param date - The date to format and refresh
 * @param intervalMs - Refresh interval in milliseconds (default: 60000ms)
 * @returns Formatted date string that updates at specified intervals
 */
export const useRefreshingDate = (date: Date, intervalMs: number = 60000): string => {
  const [tick, setTick] = useState(0);

  useEffect(() => {
    const interval = setInterval(() => setTick((prev) => prev + 1), intervalMs);
    return () => clearInterval(interval);
  }, [intervalMs]);

  // eslint-disable-next-line react-hooks/exhaustive-deps
  return useMemo(() => CommonService.formatDate(date), [date, tick]);
};
