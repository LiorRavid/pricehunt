import { HttpProblemError } from '../../../core/http/problem-details';
import { SseConnectionError } from '../../../core/sse/sse-connection-error';

/** A message for the user explaining why a search stream failed. */
export function describeSearchError(error: unknown): string {
  if (error instanceof HttpProblemError) {
    const firstFieldError = Object.values(error.problem.errors ?? {})[0]?.[0];
    return error.status >= 500
      ? 'The server could not run the search. Please try again.'
      : (firstFieldError ?? error.problem.title ?? 'The search was rejected.');
  }

  return error instanceof SseConnectionError
    ? error.message
    : 'Something went wrong. Please try again.';
}
