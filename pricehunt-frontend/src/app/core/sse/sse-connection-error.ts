/** The server could not be reached, or the stream ended before its final event. */
export class SseConnectionError extends Error {
  constructor(message = 'The connection to the server was lost.', options?: ErrorOptions) {
    super(message, options);
    this.name = 'SseConnectionError';
  }
}
