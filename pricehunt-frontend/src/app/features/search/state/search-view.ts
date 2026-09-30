import type { FinalBadge, SearchProgress } from '../domain/search-summary';
import type { SearchState } from './search-state';

export function selectProgress(state: SearchState): SearchProgress {
  const quoted = state.rows.filter((row) => row.state === 'quoted').length;
  const failed = state.rows.filter((row) => row.state === 'failed').length;
  return {
    responded: quoted + failed,
    failed,
    total: state.rows.length,
    pending: state.rows
      .filter((row) => row.state === 'pending')
      .map((row) => ({ id: row.supplierId, name: row.supplierName })),
  };
}

/** The final state to announce, or null while idle or searching. */
export function selectBadge(state: SearchState): FinalBadge | null {
  const { responded, failed, total } = selectProgress(state);
  switch (state.phase) {
    case 'completed':
      return {
        kind: 'completed',
        title: 'Completed',
        detail: `${String(responded)} of ${String(total)} suppliers responded${failed > 0 ? `, ${String(failed)} failed` : ''}.`,
      };
    case 'timedOut': {
      const silent = state.rows
        .filter((row) => row.state === 'noResponse')
        .map((row) => row.supplierName);
      return {
        kind: 'timedOut',
        title: 'Timed out',
        detail: `No response from ${silent.join(', ')}.`,
      };
    }
    case 'cancelled':
      return { kind: 'cancelled', title: 'Cancelled', detail: 'The search was stopped.' };
    case 'error':
      return { kind: 'error', title: 'Error', detail: state.errorMessage ?? 'The search failed.' };
    default:
      return null;
  }
}
