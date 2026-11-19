const DIACRITICS = /\p{Diacritic}/gu;
const NON_ALPHANUMERIC = /[^\p{L}\p{N}]+/gu;

export function normalizeCommandText(value: string): string {
  return value
    .normalize('NFD')
    .replace(DIACRITICS, '')
    .toLowerCase()
    .replace(NON_ALPHANUMERIC, ' ')
    .trim();
}

export function commandQueryTokens(query: string): string[] {
  return normalizeCommandText(query).split(' ').filter(token => token.length > 0);
}

export function scoreCommandText(tokens: string[], ...texts: string[]): number {
  const normalized = texts.map(normalizeCommandText);

  let score = 0;
  for (const token of tokens) {
    let tokenScore = 0;
    for (const text of normalized) {
      tokenScore = Math.max(tokenScore, scoreToken(text, token));
    }

    if (tokenScore === 0) return 0;
    score += tokenScore;
  }

  return score;
}

function scoreToken(text: string, token: string): number {
  if (text === token) return 100;
  if (text.startsWith(token)) return 80;
  if (text.split(' ').some(word => word.startsWith(token))) return 60;
  if (text.includes(token)) return 40;
  if (isSubsequence(token, text)) return 10;
  return 0;
}

function isSubsequence(needle: string, haystack: string): boolean {
  let index = 0;
  for (const character of haystack) {
    if (character === needle[index]) {
      index++;
      if (index === needle.length) return true;
    }
  }

  return needle.length === 0;
}
