import { HttpErrorResponse } from '@angular/common/http';
import type { ProblemDetails } from '../../../core/http/problem-details';

/** A message for the user explaining why the history couldn't be loaded. */
export function describeHistoryError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return 'The server could not be reached. Is the API running?';
    }

    if (error.status === 400) {
      const problem = error.error as ProblemDetails | null;
      return (
        Object.values(problem?.errors ?? {})
          .at(0)
          ?.at(0) ?? 'These filters are not valid.'
      );
    }
  }

  return 'The history could not be loaded. Please try again.';
}
