import { TestBed } from '@angular/core/testing';
import { firstValueFrom, toArray } from 'rxjs';
import { FETCH } from '../http/fetch';
import { HttpProblemError } from '../http/problem-details';
import { SseClient } from './sse-client';
import { SseConnectionError } from './sse-connection-error';
import type { SseMessage } from './sse-parser';

const isTerminal = (message: SseMessage) => message.event === 'done';

/** A fetch whose response body the test writes to. */
class FakeFetch {
  requests: { url: string; init: RequestInit }[] = [];
  private controller?: ReadableStreamDefaultController<Uint8Array>;
  private readonly encoder = new TextEncoder();

  constructor(
    private readonly respond: (body: ReadableStream<Uint8Array>) => Response = (body) =>
      new Response(body, { status: 200, headers: { 'content-type': 'text/event-stream' } }),
  ) {}

  readonly fetch = (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const url = input instanceof Request ? input.url : input instanceof URL ? input.href : input;
    this.requests.push({ url, init: init ?? {} });
    const body = new ReadableStream<Uint8Array>({
      start: (controller) => (this.controller = controller),
    });
    return Promise.resolve(this.respond(body));
  };

  write(text: string): void {
    this.controller?.enqueue(this.encoder.encode(text));
  }

  end(): void {
    this.controller?.close();
  }

  get signal(): AbortSignal | null | undefined {
    return this.requests[0]?.init.signal;
  }
}

function setUp(fake: FakeFetch | { fetch: typeof fetch }): SseClient {
  TestBed.configureTestingModule({ providers: [{ provide: FETCH, useValue: fake.fetch }] });
  return TestBed.inject(SseClient);
}

async function flush(): Promise<void> {
  await new Promise<void>((resolve) => setTimeout(resolve));
}

describe('SseClient', () => {
  it('posts the body as JSON and asks for an event stream', async () => {
    const fake = new FakeFetch();
    const subscription = setUp(fake)
      .post('/api/searches', { origin: 'Haifa' }, isTerminal)
      .subscribe();
    await flush();

    const { url, init } = fake.requests[0] ?? { url: '', init: {} };
    expect(url).toBe('/api/searches');
    expect(init.method).toBe('POST');
    expect(init.body).toBe('{"origin":"Haifa"}');
    expect(init.headers).toEqual({
      'Content-Type': 'application/json',
      Accept: 'text/event-stream',
    });
    subscription.unsubscribe();
  });

  it('emits each event as it arrives and completes after the terminal event', async () => {
    const fake = new FakeFetch();
    const received: string[] = [];
    let completed = false;
    setUp(fake)
      .post('/api/searches', {}, isTerminal)
      .subscribe({
        next: (message) => received.push(message.event),
        complete: () => (completed = true),
      });

    fake.write('event: first\ndata: 1\n\n');
    await flush();
    expect(received).toEqual(['first']);

    fake.write('event: done\ndata: 2\n\nevent: ignored\ndata: 3\n\n');
    await flush();
    expect(received).toEqual(['first', 'done']);
    expect(completed).toBe(true);
  });

  it('aborts the request when unsubscribed, without reporting an error', async () => {
    const fake = new FakeFetch();
    const onError = vi.fn();
    const subscription = setUp(fake)
      .post('/api/searches', {}, isTerminal)
      .subscribe({ error: onError });
    await flush();

    subscription.unsubscribe();
    await flush();

    expect(fake.signal?.aborted).toBe(true);
    expect(onError).not.toHaveBeenCalled();
  });

  it('reports a problem response as an HttpProblemError', async () => {
    const problem = {
      title: 'One or more validation errors occurred.',
      status: 400,
      errors: { origin: ['A location is required.'] },
    };
    const fake = new FakeFetch(
      () =>
        new Response(JSON.stringify(problem), {
          status: 400,
          headers: { 'content-type': 'application/problem+json' },
        }),
    );

    const failure = firstValueFrom(setUp(fake).post('/api/searches', {}, isTerminal));

    await expect(failure).rejects.toBeInstanceOf(HttpProblemError);
    await expect(failure).rejects.toMatchObject({ status: 400, problem });
  });

  it('reports a non-JSON error response with a generic message', async () => {
    const fake = new FakeFetch(
      () => new Response('Bad gateway', { status: 502, headers: { 'content-type': 'text/plain' } }),
    );

    const failure = firstValueFrom(setUp(fake).post('/api/searches', {}, isTerminal));

    await expect(failure).rejects.toMatchObject({
      status: 502,
      message: 'The request failed with status 502.',
    });
  });

  it('reports a stream that ends before its terminal event as a lost connection', async () => {
    const fake = new FakeFetch();
    const events = firstValueFrom(
      setUp(fake).post('/api/searches', {}, isTerminal).pipe(toArray()),
    );

    fake.write('event: first\ndata: 1\n\ndata: partial');
    fake.end();

    await expect(events).rejects.toBeInstanceOf(SseConnectionError);
  });

  it('reports an unreachable server as a connection error', async () => {
    const failing = { fetch: () => Promise.reject(new TypeError('Failed to fetch')) };

    const failure = firstValueFrom(setUp(failing).post('/api/searches', {}, isTerminal));

    await expect(failure).rejects.toMatchObject({
      name: 'SseConnectionError',
      message: 'The server could not be reached.',
    });
  });
});
