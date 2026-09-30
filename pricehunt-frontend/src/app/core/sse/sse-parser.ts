/** One dispatched server-sent event. */
export interface SseMessage {
  /** The event type; "message" when the stream didn't name one. */
  readonly event: string;
  /** The event's data lines, joined with newlines. */
  readonly data: string;
  /** The last event id seen on the stream. */
  readonly id: string;
  /** The reconnection time the server asked for, in milliseconds. */
  readonly retry?: number;
}

/**
 * An incremental event-stream parser following the WHATWG rules: feed it decoded text in chunks
 * split anywhere (even inside a CRLF pair) and it returns the events each chunk completes. An event
 * that the stream ends in the middle of is never dispatched.
 */
export class SseParser {
  private buffer = '';
  private started = false;
  private skipLineFeed = false;
  private eventType = '';
  private data = '';
  private lastEventId = '';
  private retry: number | undefined;

  push(chunk: string): SseMessage[] {
    let text = this.started ? chunk : this.stripByteOrderMark(chunk);
    if (this.skipLineFeed && text.length > 0) {
      // The previous chunk ended with CR: an LF right after it completes the same CRLF.
      text = text.startsWith('\n') ? text.slice(1) : text;
      this.skipLineFeed = false;
    }

    this.buffer += text;
    const messages: SseMessage[] = [];
    let lineStart = 0;

    for (let index = 0; index < this.buffer.length; index++) {
      const character = this.buffer[index];
      if (character !== '\n' && character !== '\r') {
        continue;
      }

      const line = this.buffer.slice(lineStart, index);
      if (character === '\r') {
        if (index === this.buffer.length - 1) {
          this.skipLineFeed = true;
        } else if (this.buffer[index + 1] === '\n') {
          index++;
        }
      }

      lineStart = index + 1;
      const message = this.processLine(line);
      if (message) {
        messages.push(message);
      }
    }

    this.buffer = this.buffer.slice(lineStart);
    return messages;
  }

  private stripByteOrderMark(chunk: string): string {
    this.started = chunk.length > 0;
    return chunk.startsWith('\uFEFF') ? chunk.slice(1) : chunk;
  }

  private processLine(line: string): SseMessage | undefined {
    if (line === '') {
      return this.dispatch();
    }

    if (line.startsWith(':')) {
      return undefined;
    }

    const colon = line.indexOf(':');
    const field = colon === -1 ? line : line.slice(0, colon);
    let value = colon === -1 ? '' : line.slice(colon + 1);
    if (value.startsWith(' ')) {
      value = value.slice(1);
    }

    switch (field) {
      case 'event':
        this.eventType = value;
        break;
      case 'data':
        this.data += `${value}\n`;
        break;
      case 'id':
        if (!value.includes('\u0000')) {
          this.lastEventId = value;
        }
        break;
      case 'retry':
        if (/^\d+$/.test(value)) {
          this.retry = Number(value);
        }
        break;
    }

    return undefined;
  }

  private dispatch(): SseMessage | undefined {
    const retry = this.retry;
    const message: SseMessage | undefined =
      this.data === ''
        ? undefined
        : {
            event: this.eventType || 'message',
            data: this.data.slice(0, -1),
            id: this.lastEventId,
            ...(retry === undefined ? {} : { retry }),
          };

    this.eventType = '';
    this.data = '';
    this.retry = undefined;
    return message;
  }
}
