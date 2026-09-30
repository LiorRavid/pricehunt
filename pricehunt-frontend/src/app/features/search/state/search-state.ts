import type { ResultRow } from '../domain/result-row';

/** idle → searching → completed | timedOut | cancelled | error; a new search starts over. */
export type SearchPhase = 'idle' | 'searching' | 'completed' | 'timedOut' | 'cancelled' | 'error';

export interface SearchState {
  readonly phase: SearchPhase;
  /** Increments with every search the user starts; tags every action of that search. */
  readonly attempt: number;
  /** The server's id for the active search, known once it has started. */
  readonly searchId: string | null;
  /** One row per selected supplier, in selection order (views sort them). */
  readonly rows: readonly ResultRow[];
  readonly maxDurationMs: number | null;
  readonly errorMessage: string | null;
}

export const initialSearchState: SearchState = {
  phase: 'idle',
  attempt: 0,
  searchId: null,
  rows: [],
  maxDurationMs: null,
  errorMessage: null,
};
