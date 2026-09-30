import type { HistoryPage } from '../domain/history-item';
import type { HistoryPageDto } from './history-dtos';

/** Maps the API's page to the history's own model. */
export function toHistoryPage(dto: HistoryPageDto): HistoryPage {
  return {
    items: dto.items.map((item) => ({
      id: item.id,
      searchId: item.searchId,
      receivedAt: new Date(item.receivedAt),
      origin: item.origin,
      destination: item.destination,
      shipDateFrom: item.shipDateFrom,
      shipDateTo: item.shipDateTo,
      supplierId: item.supplierId,
      supplierName: item.supplierName,
      outcome: item.outcome,
      price: item.price,
      responseTimeMs: item.responseTimeMs,
    })),
    page: dto.page,
    pageSize: dto.pageSize,
    totalCount: dto.totalCount,
  };
}
