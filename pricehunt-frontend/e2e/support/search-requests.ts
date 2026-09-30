import type { Page } from '@playwright/test';

/** How one `POST /api/searches` request ended, as the page saw it. */
export interface SearchRequest {
  readonly origin: string;
  /** The page aborted the request: it abandoned the search. */
  readonly aborted: boolean;
  /** The response body ran to its end. */
  readonly ended: boolean;
}

/**
 * Records how each search request ends. Chrome reports every fetch whose body is read as a
 * stream as `net::ERR_ABORTED`, even one read to its end, so the browser's own request status
 * can't tell a cancelled search from a finished one. Call it before the page loads.
 */
export async function recordSearchRequests(page: Page): Promise<void> {
  await page.addInitScript(() => {
    const requests: { origin: string; aborted: boolean; ended: boolean }[] = [];
    Object.assign(window, { searchRequests: requests });
    const original = window.fetch.bind(window);
    window.fetch = async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input instanceof URL ? input.href : input.url;
      if (!url.endsWith('/api/searches') || typeof init?.body !== 'string') {
        return original(input, init);
      }

      const entry = {
        origin: (JSON.parse(init.body) as { origin: string }).origin,
        aborted: false,
        ended: false,
      };
      requests.push(entry);
      init.signal?.addEventListener('abort', () => {
        entry.aborted = true;
      });
      const response = await original(input, init);
      if (!response.body) {
        return response;
      }

      const { readable, writable } = new TransformStream<Uint8Array, Uint8Array>({
        flush: () => {
          entry.ended = true;
        },
      });
      void response.body.pipeTo(writable).catch(() => undefined);
      return new Response(readable, response);
    };
  });
}

export async function searchRequests(page: Page): Promise<SearchRequest[]> {
  return page.evaluate(
    () => (window as unknown as { searchRequests: SearchRequest[] }).searchRequests,
  );
}
