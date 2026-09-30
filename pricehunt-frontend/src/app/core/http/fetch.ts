import { InjectionToken } from '@angular/core';

/** The Fetch API, injectable so tests can replace it without touching globals. */
export const FETCH = new InjectionToken<typeof fetch>('FETCH', {
  providedIn: 'root',
  factory: () => globalThis.fetch.bind(globalThis),
});
