import { inject, InjectionToken } from '@angular/core';

export interface SourceExApiConfig {
  baseUrl: string;
  apiVersion: string;
}

export const SOURCEEX_API_CONFIG = new InjectionToken<SourceExApiConfig>('SOURCEEX_API_CONFIG');

export function injectSourceExApiConfig(): SourceExApiConfig {
  return inject(SOURCEEX_API_CONFIG);
}

export function buildVersionedApiUrl(config: SourceExApiConfig, relativePath: string): string {
  const normalizedBaseUrl = config.baseUrl.replace(/\/+$/, '');
  const normalizedPath = relativePath.replace(/^\/+/, '');

  return `${normalizedBaseUrl}/api/${config.apiVersion}/${normalizedPath}`;
}

export function getApiVersionHeaderValue(config: SourceExApiConfig): string {
  return config.apiVersion.replace(/^v/i, '');
}

export function isSourceExApiRequest(requestUrl: string, config: SourceExApiConfig): boolean {
  const normalizedBaseUrl = config.baseUrl.replace(/\/+$/, '');

  if (normalizedBaseUrl.length === 0) {
    return requestUrl.startsWith('/api/');
  }

  return requestUrl.startsWith('/api/') || requestUrl.startsWith(`${normalizedBaseUrl}/api/`);
}
