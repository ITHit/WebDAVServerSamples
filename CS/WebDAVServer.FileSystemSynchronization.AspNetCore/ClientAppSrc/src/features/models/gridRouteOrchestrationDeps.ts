import type { AppEventBus } from '@/shared/contracts/appEventBus';

export interface GridRouteOrchestrationDeps {
  getServerRootUrl: () => string;
  getServerUrl: (path: string) => string;
  eventBus: AppEventBus;
  reportError: (error: unknown) => void;
  reportEventBusError: (error: unknown) => void;
  logLocalizedError: (error: unknown) => void;
}
