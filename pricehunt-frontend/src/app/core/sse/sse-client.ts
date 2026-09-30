import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { FETCH } from '../http/fetch';
import { HttpProblemError } from '../http/problem-details';
import { SseConnectionError } from './sse-connection-error';
import { SseParser, type SseMessage } from './sse-parser';

/** How long a finished stream may stay open before it's aborted to free the connection. */
const DRAIN_TIMEOUT_MS = 2000;

/**
 * Streams server-sent events from a POST request (ADR-001). The stream is cold: subscribing sends
 * the request, and unsubscribing before the terminal event aborts it, which the server sees as a
 * disconnect. After the terminal event the response is left to end on its own, so only abandoning
 * a search aborts its request.
 */
@Injectable({ providedIn: 'root' })
export class SseClient {
  private readonly fetch = inject(FETCH);

  /**
   * Posts `body` as JSON and emits each event until `isTerminal` accepts one, then completes.
   * Errors: {@link HttpProblemError} for an error status; {@link SseConnectionError} when the server
   * can't be reached or the stream ends before a terminal event.
   */
  post(
    url: string,
    body: unknown,
    isTerminal: (message: SseMessage) => boolean,
  ): Observable<SseMessage> {
    return new Observable<SseMessage>((subscriber) => {
      const abort = new AbortController();
      let finished = false;

      const stream = async (): Promise<void> => {
        const response = await this.fetch(url, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
          body: JSON.stringify(body),
          signal: abort.signal,
        });
        if (!response.ok) {
          subscriber.error(await HttpProblemError.fromResponse(response));
          return;
        }

        if (!response.body) {
          throw new SseConnectionError('The server sent no event stream.');
        }

        const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();
        const parser = new SseParser();
        for (;;) {
          const { done, value } = await reader.read();
          if (done) {
            throw new SseConnectionError();
          }

          for (const message of parser.push(value)) {
            subscriber.next(message);
            if (isTerminal(message)) {
              finished = true;
              subscriber.complete();
              await drain(reader, abort);
              return;
            }
          }
        }
      };

      stream().catch((error: unknown) => {
        // Unsubscribing aborts the request, and a finished stream has nothing left to report.
        if (abort.signal.aborted || finished) {
          return;
        }

        subscriber.error(
          error instanceof HttpProblemError || error instanceof SseConnectionError
            ? error
            : new SseConnectionError('The server could not be reached.', { cause: error }),
        );
      });

      return () => {
        if (!finished) {
          abort.abort();
        }
      };
    });
  }
}

/** Reads what's left after the terminal event, aborting if the server keeps the stream open. */
async function drain(
  reader: ReadableStreamDefaultReader<string>,
  abort: AbortController,
): Promise<void> {
  const timer = setTimeout(() => {
    abort.abort();
  }, DRAIN_TIMEOUT_MS);
  try {
    while (!(await reader.read()).done) {
      // Anything after the terminal event is ignored.
    }
  } finally {
    clearTimeout(timer);
  }
}
