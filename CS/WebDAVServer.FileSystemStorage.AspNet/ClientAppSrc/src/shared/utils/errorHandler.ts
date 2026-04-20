
import {
  AppError,
  NetworkError,
  ValidationError,
} from '@/shared/types/appErrors';

type TranslateFn = (key: string) => string;

/** Maps AppError codes to i18n keys for known error types. */
const APP_ERROR_CODE_KEYS: Record<string, string> = {
  'NETWORK_ERROR': 'phrases.errors.networkError',
  'NOT_FOUND': 'phrases.errors.notFound',
  'PERMISSION_ERROR': 'phrases.errors.permissionError',
  'SERVER_ERROR': 'phrases.errors.serverError',
  'UNKNOWN_ERROR': 'phrases.errors.unknown',
};

/**
 * Convert any error to AppError without any localization side effects.
 */
function normalizeError(error: unknown): AppError {
  // Already an AppError — preserve as-is for later translation/presentation.
  if (error instanceof AppError) {
    return error;
  }

  // Network errors
  if (error instanceof TypeError && error.message.includes('fetch')) {
    return new NetworkError(error.message, error);
  }

  // Generic Error
  if (error instanceof Error) {
    return new AppError(
      error.message,
      'UNKNOWN_ERROR',
      'An unexpected error occurred. Please try again.',
      error
    );
  }

  // Unknown error type
  return new AppError(
    String(error),
    'UNKNOWN_ERROR',
    'An unexpected error occurred. Please try again.'
  );
}

/**
 * Apply localization to an AppError using an explicit translator.
 */
function localizeError(error: AppError, translate: TranslateFn): AppError {
  if (error instanceof ValidationError) {
    return new AppError(
      error.message,
      error.code,
      translate(error.message),
      error.originalError
    );
  }

  const i18nKey = APP_ERROR_CODE_KEYS[error.code];
  if (!i18nKey) {
    return error;
  }

  return new AppError(
    error.message,
    error.code,
    translate(i18nKey),
    error.originalError
  );
}

/**
 * Convert any error to AppError with user-friendly localized message.
 */
export function handleError(error: unknown, translate: TranslateFn): AppError {
  return localizeError(normalizeError(error), translate);
}

/**
 * Log error to console in development
 */
export function logError(error: AppError): void {
  if (import.meta.env.DEV) {
    console.error('[AppError]', {
      code: error.code,
      message: error.message,
      userMessage: error.userMessage,
      original: error.originalError,
      stack: error.stack,
    });
  }
}

