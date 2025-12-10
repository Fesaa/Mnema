export const environment = {
  production: true,
  apiUrl: `${getBaseUrl()}api/`
};

function getBaseUrl(): string {
  return document.getElementsByTagName('base')[0]?.getAttribute('href') || '/';
}
