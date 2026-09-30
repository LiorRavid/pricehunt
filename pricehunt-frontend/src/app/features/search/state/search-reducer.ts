import type { ResultRow } from '../domain/result-row';
import type { SupplierRef } from '../domain/search-criteria';
import type { SearchEvent, SearchOutcome } from '../domain/search-event';
import type { SearchPhase, SearchState } from './search-state';

export type SearchAction =
  | {
      readonly type: 'searchRequested';
      readonly attempt: number;
      readonly suppliers: readonly SupplierRef[];
    }
  | { readonly type: 'eventReceived'; readonly attempt: number; readonly event: SearchEvent }
  | { readonly type: 'streamFailed'; readonly attempt: number; readonly message: string }
  | { readonly type: 'searchCancelled'; readonly attempt: number };

const phaseByOutcome: Record<SearchOutcome, SearchPhase> = {
  Completed: 'completed',
  TimedOut: 'timedOut',
  Faulted: 'error',
};

/**
 * The search state machine (ADR-004). A new search replaces everything; every other action is
 * dropped unless it belongs to the current attempt, the search is still running, and (for stream
 * events) it carries the active search id.
 */
export function searchReducer(state: SearchState, action: SearchAction): SearchState {
  if (action.type === 'searchRequested') {
    return {
      phase: 'searching',
      attempt: action.attempt,
      searchId: null,
      rows: action.suppliers.map(pendingRow),
      maxDurationMs: null,
      errorMessage: null,
    };
  }

  if (action.attempt !== state.attempt || state.phase !== 'searching') {
    return state;
  }

  switch (action.type) {
    case 'eventReceived':
      return applyEvent(state, action.event);
    case 'streamFailed':
      return { ...closePendingRows(state), phase: 'error', errorMessage: action.message };
    case 'searchCancelled':
      return { ...closePendingRows(state), phase: 'cancelled' };
  }
}

function applyEvent(state: SearchState, event: SearchEvent): SearchState {
  if (event.type === 'started') {
    return state.searchId === null
      ? {
          ...state,
          searchId: event.searchId,
          maxDurationMs: event.maxDurationMs,
          rows: event.suppliers.map(pendingRow),
        }
      : state;
  }

  if (event.searchId !== state.searchId) {
    return state;
  }

  switch (event.type) {
    case 'quote':
      return updatePendingRow(state, event.supplierId, (row) => ({
        supplierId: row.supplierId,
        supplierName: row.supplierName,
        state: 'quoted',
        price: event.price,
        responseTimeMs: event.responseTimeMs,
      }));
    case 'failure':
      return updatePendingRow(state, event.supplierId, (row) => ({
        supplierId: row.supplierId,
        supplierName: row.supplierName,
        state: 'failed',
        errorMessage: event.errorMessage,
        responseTimeMs: event.responseTimeMs,
      }));
    case 'completed':
      return {
        ...closePendingRows(state),
        phase: phaseByOutcome[event.outcome],
        errorMessage: event.outcome === 'Faulted' ? 'The search failed on the server.' : null,
      };
  }
}

function pendingRow(supplier: SupplierRef): ResultRow {
  return { supplierId: supplier.id, supplierName: supplier.name, state: 'pending' };
}

function updatePendingRow(
  state: SearchState,
  supplierId: string,
  update: (row: ResultRow) => ResultRow,
): SearchState {
  const isTarget = (row: ResultRow) => row.supplierId === supplierId && row.state === 'pending';
  return state.rows.some(isTarget)
    ? { ...state, rows: state.rows.map((row) => (isTarget(row) ? update(row) : row)) }
    : state;
}

function closePendingRows(state: SearchState): SearchState {
  return {
    ...state,
    rows: state.rows.map((row) =>
      row.state === 'pending'
        ? { supplierId: row.supplierId, supplierName: row.supplierName, state: 'noResponse' }
        : row,
    ),
  };
}
