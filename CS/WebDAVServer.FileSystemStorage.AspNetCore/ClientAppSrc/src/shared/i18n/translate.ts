import translation from '@/i18n/en/translation.json';

type TranslationTree = Record<string, unknown>;

const catalog = translation as TranslationTree;

function getByPath(tree: TranslationTree, path: string): unknown {
  return path.split('.').reduce<unknown>((current, key) => {
    if (!current || typeof current !== 'object') {
      return undefined;
    }

    return (current as Record<string, unknown>)[key];
  }, tree);
}

function formatTemplate(template: string, params?: Record<string, string | number>): string {
  if (!params) {
    return template;
  }

  return template.replace(/\{(\w+)\}/g, (_, key: string) => {
    const value = params[key];
    return value === undefined ? `{${key}}` : String(value);
  });
}

export function t(key: string, params?: Record<string, string | number>): string {
  const value = getByPath(catalog, key);
  if (typeof value !== 'string') {
    return key;
  }

  return formatTemplate(value, params);
}
