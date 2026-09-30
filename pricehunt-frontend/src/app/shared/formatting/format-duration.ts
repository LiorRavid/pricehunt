/** Formats a duration in milliseconds as seconds with one decimal, for example `1.2 s`. */
export function formatDuration(milliseconds: number): string {
  return `${(milliseconds / 1000).toFixed(1)} s`;
}
