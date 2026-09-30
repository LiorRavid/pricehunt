import { summarizePage } from './page-summary';

describe('summarizePage [HC2]', () => {
  it('describes a page in the middle', () => {
    expect(summarizePage(2, 20, 312)).toEqual({
      firstItem: 21,
      lastItem: 40,
      totalCount: 312,
      page: 2,
      pageCount: 16,
    });
  });

  it('stops the last page at the total', () => {
    expect(summarizePage(16, 20, 312)).toMatchObject({ firstItem: 301, lastItem: 312 });
    expect(summarizePage(1, 20, 20)).toMatchObject({ lastItem: 20, pageCount: 1 });
    expect(summarizePage(1, 20, 21).pageCount).toBe(2);
  });

  it('counts one empty page when nothing matches', () => {
    expect(summarizePage(1, 20, 0)).toEqual({
      firstItem: 0,
      lastItem: 0,
      totalCount: 0,
      page: 1,
      pageCount: 1,
    });
  });

  it('shows no items for a page past the end', () => {
    expect(summarizePage(5, 20, 45)).toMatchObject({ firstItem: 0, lastItem: 0, pageCount: 3 });
  });
});
