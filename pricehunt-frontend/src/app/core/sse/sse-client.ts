import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { FETCH } from '../http/fetch';
import { HttpProblemError } from '../http/problem-details';
import { SseConnectionError } from './sse-connection-error';
import { SseParser, type SseMessage } from './sse-parser';

/**
 * Streams server-sent events from a POST request (ADR-001). The stream is cold: subscribing sends
 * the request, and unsubscribing aborts it, which the server sees as a disconnect.
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
              subscriber.complete();
              return;
            }
          }
        }
      };

      stream().catch((error: unknown) => {
        // Unsubscribing aborts the request; that's not an error anyone should see.
        if (abort.signal.aborted) {
          return;
        }

        subscriber.error(
          error instanceof HttpProblemError || error instanceof SseConnectionError
            ? error
            : new SseConnectionError('The server could not be reached.', { cause: error }),
        );
      });

      return () => {
        abort.abort();
      };
    });
  }
}
