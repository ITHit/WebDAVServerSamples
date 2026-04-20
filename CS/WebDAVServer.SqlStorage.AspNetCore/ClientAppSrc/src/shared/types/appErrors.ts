/**
 * Base application error class
 */
export class AppError extends Error {
  constructor(
    message: string,
    public readonly code: string,
    public readonly userMessage: string,
    public readonly originalError?: Error
  ) {
    super(message);
    this.name = 'AppError';
    Object.setPrototypeOf(this, AppError.prototype);
  }
}

/**
 * Network/connectivity errors
 */
export class NetworkError extends AppError {
  constructor(message: string, originalError?: Error) {
    super(
      message,
      'NETWORK_ERROR',
      'Unable to connect to the server. Please check your internet connection.',
      originalError
    );
    this.name = 'NetworkError';
  }
}

/**
 * Not found errors (404)
 */
export class NotFoundError extends AppError {
  constructor(message: string, originalError?: Error) {
    super(
      message,
      'NOT_FOUND',
      'The requested item could not be found.',
      originalError
    );
    this.name = 'NotFoundError';
  }
}

/**
 * Permission/authentication errors
 */
export class PermissionError extends AppError {
  constructor(message: string, originalError?: Error) {
    super(
      message,
      'PERMISSION_ERROR',
      'You do not have permission to perform this action.',
      originalError
    );
    this.name = 'PermissionError';
  }
}

/**
 * Server errors (5xx)
 */
export class ServerError extends AppError {
  constructor(message: string, originalError?: Error) {
    super(
      message,
      'SERVER_ERROR',
      'A server error occurred. Please try again later.',
      originalError
    );
    this.name = 'ServerError';
  }
}

/**
 * Validation errors — thrown by domain value objects and use cases.
 * The `message` field carries an i18n key resolved by the error handler.
 */
export class ValidationError extends AppError {
  constructor(message: string, originalError?: unknown) {
    super(message, 'VALIDATION_ERROR', message, originalError as Error | undefined);
    this.name = 'ValidationError';
    Object.setPrototypeOf(this, ValidationError.prototype);
  }
}

/**
 * Authentication errors (401)
 */
export class AuthenticationError extends AppError {
  constructor(message: string, originalError?: Error) {
    super(
      message,
      'AUTH_ERROR',
      'Authentication is required to perform this action.',
      originalError
    );
    this.name = 'AuthenticationError';
    Object.setPrototypeOf(this, AuthenticationError.prototype);
  }
}

/**
 * Thrown when copying items to a different folder and some names already exist.
 * Carries the paths + display names of conflicting items so the caller can
 * show an overwrite prompt and retry only those items.
 */
export class CopyConflictError extends AppError {
  constructor(
    public readonly conflictingPaths: string[],
    public readonly conflictingNames: string[]
  ) {
    super(
      'Copy conflict: items already exist at the destination',
      'COPY_CONFLICT',
      'Some items already exist in the target folder.',
    );
    this.name = 'CopyConflictError';
    Object.setPrototypeOf(this, CopyConflictError.prototype);
  }
}
