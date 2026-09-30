/** Where a page sits in the results, for "Showing 21–40 of 312" and the page buttons (HC2). */
export interface PageSummary {
  /** 1-based position of the page's first item, or 0 when the page is empty. */
  readonly firstItem: number;
  readonly lastItem: number;
  readonly totalCount: number;
  readonly page: number;
  /** At least 1, so an empty result still has a page. */
  readonly pageCount: number;
}

export function summarizePage(page: number, pageSize: number, totalCount: number): PageSummary {
  const pageCount = Math.max(1, Math.ceil(totalCount / pageSize));
  const offset = (page - 1) * pageSize;
  if (offset >= totalCount) {
    return { firstItem: 0, lastItem: 0, totalCount, page, pageCount };
  }

  return {
    firstItem: offset + 1,
    lastItem: Math.min(offset + pageSize, totalCount),
    totalCount,
    page,
    pageCount,
  };
}
