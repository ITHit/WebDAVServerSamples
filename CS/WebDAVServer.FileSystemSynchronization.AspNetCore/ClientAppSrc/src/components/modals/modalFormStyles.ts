export const modalFormStyles = {
  body: 'px-6 py-4',
  footer:
    'flex items-center justify-end gap-3 px-6 py-4 bg-surface-secondary border-t border-border rounded-b-xl',
  input:
    'w-full px-4 py-2.5 rounded-lg border-input bg-input placeholder outline-none transition-all disabled:opacity-50 disabled:cursor-not-allowed cursor-text',
  buttonSecondary:
    'px-4 py-2 rounded-lg font-medium text-secondary hover:bg-surface-hover transition-colors disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer',
  buttonPrimary:
    'btn btn-primary rounded-lg disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 cursor-pointer',
  errorBox: 'mt-2 flex items-start gap-2 p-3 bg-error-subtle border border-error rounded-lg',
  errorText: 'text-sm text-error',
} as const;
