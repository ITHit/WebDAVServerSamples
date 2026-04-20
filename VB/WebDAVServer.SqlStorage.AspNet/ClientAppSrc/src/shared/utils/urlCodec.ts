export function getTail(url1: string, url2: string) {
  return url1.includes(url2) ? url1.replace(url2, '') : '';
}

export function encodeUri(text: string) {
  if (!text) {
    return text;
  }

  return encodeURI(text).replace(/%25/g, '%');
}

export function decodeUri(text: string) {
  if (!text) {
    return text;
  }

  return decodeURI(
    text.replace(/%([^0-9A-F]|.(?:[^0-9A-F]|$)|$)/gi, '%25$1')
  );
}

export function decode(path: string) {
  if (!path) {
    return path;
  }

  const res = path
    .replace(/%7E/gi, '~')
    .replace(/%21/g, '!')
    .replace(/%40/g, '@')
    .replace(/%23/g, '#')
    .replace(/%24/g, '$')
    .replace(/%26/g, '&')
    .replace(/%2A/gi, '*')
    .replace(/%28/g, '(')
    .replace(/%29/g, ')')
    .replace(/%2D/gi, '-')
    .replace(/%5F/gi, '_')
    .replace(/%2B/gi, '+')
    .replace(/%3D/gi, '=')
    .replace(/%27/g, "'")
    .replace(/%3B/gi, ';')
    .replace(/%2E/gi, '.')
    .replace(/%2C/gi, ',')
    .replace(/%3F/gi, '?')
    .replace(/%60/gi, '`')
    .replace(/%5B/gi, '[')
    .replace(/%5D/gi, ']')
    .replace(/%7B/gi, '{')
    .replace(/%7D/gi, '}')
    .replace(/%7C/gi, '|')
    .replace(/%5C/gi, '\\')
    .replace(/%5E/gi, '^');

  return decodeUri(res);
}

export function encode(path: string) {
  if (!path) {
    return path;
  }

  const res = path
    .replace(/~/g, '%7E')
    .replace(/!/g, '%21')
    .replace(/@/g, '%40')
    .replace(/#/g, '%23')
    .replace(/\$/g, '%24')
    .replace(/&/g, '%26')
    .replace(/\*/g, '%2A')
    .replace(/\(/g, '%28')
    .replace(/\)/g, '%29')
    .replace(/-/g, '%2D')
    .replace(/_/g, '%5F')
    .replace(/\+/g, '%2B')
    .replace(/=/g, '%3D')
    .replace(/'/g, '%27')
    .replace(/;/g, '%3B')
    .replace(/\./g, '%2E')
    .replace(/,/g, '%2C')
    .replace(/\?/g, '%3F')
    .replace(/ /g, '%20');

  return encodeUri(res);
}
