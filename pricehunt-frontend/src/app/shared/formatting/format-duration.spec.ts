import { formatDuration } from './format-duration';

describe('formatDuration', () => {
  it('formats milliseconds as seconds with one decimal', () => {
    expect(formatDuration(1234)).toBe('1.2 s');
    expect(formatDuration(500)).toBe('0.5 s');
    expect(formatDuration(6000)).toBe('6.0 s');
  });
});
