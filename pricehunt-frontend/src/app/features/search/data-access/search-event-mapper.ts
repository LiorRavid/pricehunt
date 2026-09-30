import type { SseMessage } from '../../../core/sse/sse-parser';
import type { SearchEvent } from '../domain/search-event';
import type {
  QuoteReceivedDto,
  SearchCompletedDto,
  SearchStartedDto,
  SupplierFailedDto,
} from './search-dtos';

/** The server's final event; the stream ends after it. */
export const SEARCH_COMPLETED = 'search-completed';

/** Maps one server-sent event to a search event; unknown event types are ignored. */
export function toSearchEvent(message: SseMessage): SearchEvent | null {
  switch (message.event) {
    case 'search-started': {
      const dto = JSON.parse(message.data) as SearchStartedDto;
      return {
        type: 'started',
        searchId: dto.searchId,
        suppliers: dto.suppliers.map(({ id, name }) => ({ id, name })),
        maxDurationMs: dto.maxDurationMs,
      };
    }
    case 'quote-received': {
      const dto = JSON.parse(message.data) as QuoteReceivedDto;
      return {
        type: 'quote',
        searchId: dto.searchId,
        supplierId: dto.supplierId,
        price: { amount: dto.price.amount, currency: dto.price.currency },
        responseTimeMs: dto.responseTimeMs,
      };
    }
    case 'supplier-failed': {
      const dto = JSON.parse(message.data) as SupplierFailedDto;
      return {
        type: 'failure',
        searchId: dto.searchId,
        supplierId: dto.supplierId,
        errorMessage: dto.errorMessage,
        responseTimeMs: dto.responseTimeMs,
      };
    }
    case SEARCH_COMPLETED: {
      const dto = JSON.parse(message.data) as SearchCompletedDto;
      return {
        type: 'completed',
        searchId: dto.searchId,
        outcome: dto.status,
        noResponseSupplierIds: [...dto.noResponseSupplierIds],
      };
    }
    default:
      return null;
  }
}
