import { TestBed, type ComponentFixture } from '@angular/core/testing';
import type { FinalBadge, SearchProgress } from '../../domain/search-summary';
import { ProgressPanel } from './progress-panel';

describe('ProgressPanel [CL4]', () => {
  let fixture: ComponentFixture<ProgressPanel>;
  let element: HTMLElement;

  async function render(inputs: {
    searching: boolean;
    progress: SearchProgress;
    badge?: FinalBadge | null;
    maxDurationMs?: number | null;
  }) {
    fixture = TestBed.createComponent(ProgressPanel);
    fixture.componentRef.setInput('searching', inputs.searching);
    fixture.componentRef.setInput('progress', inputs.progress);
    fixture.componentRef.setInput('badge', inputs.badge ?? null);
    fixture.componentRef.setInput('maxDurationMs', inputs.maxDurationMs ?? null);
    await fixture.whenStable();
    element = fixture.nativeElement as HTMLElement;
  }

  it('shows how many suppliers answered and who is still pending', async () => {
    await render({
      searching: true,
      progress: {
        responded: 5,
        failed: 1,
        total: 7,
        pending: [
          { id: 'cobalt', name: 'Cobalt Harbor Lines' },
          { id: 'gullwing', name: 'Gullwing Transport' },
        ],
      },
      maxDurationMs: 6000,
    });

    expect(element.textContent).toContain('5 of 7 suppliers responded');
    expect(element.textContent).toContain('· 1 failed');
    const pending = element.querySelector('ul[aria-label="Suppliers still pending"]');
    expect(
      Array.from(pending?.querySelectorAll('li') ?? [], (item) => item.textContent.trim()),
    ).toEqual(['Cobalt Harbor Lines', 'Gullwing Transport']);
    expect(element.querySelector<HTMLElement>('.deadline-bar')?.style.animationDuration).toBe(
      '6000ms',
    );
  });

  it('announces the progress politely as suppliers answer', async () => {
    await render({
      searching: true,
      progress: { responded: 2, failed: 1, total: 7, pending: [] },
      maxDurationMs: 6000,
    });

    const count = Array.from(element.querySelectorAll('p')).find((paragraph) =>
      paragraph.textContent.includes('suppliers responded'),
    );
    expect(count?.getAttribute('aria-live')).toBe('polite');
    expect(count?.getAttribute('aria-atomic')).toBe('true');
    // The outcome stays the only status region.
    expect(element.querySelectorAll('[role="status"]')).toHaveLength(1);
  });

  it('announces the final state in a polite live region', async () => {
    await render({
      searching: false,
      progress: { responded: 6, failed: 0, total: 7, pending: [] },
      badge: {
        kind: 'timedOut',
        title: 'Timed out',
        detail: 'No response from Gullwing Transport.',
      },
    });

    const status = element.querySelector('[role="status"]');
    expect(status?.getAttribute('aria-live')).toBe('polite');
    expect(status?.textContent).toContain('Timed out');
    expect(status?.textContent).toContain('No response from Gullwing Transport.');
    expect(status?.querySelector('p')?.className).toContain('bg-amber-50');
    expect(element.querySelector('.deadline-bar')).toBeNull();
  });

  it('replaces every pending supplier without a dev-mode warning', async () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    const progress = (...names: string[]): SearchProgress => ({
      responded: 0,
      failed: 0,
      total: names.length,
      pending: names.map((name) => ({ id: name.toLowerCase(), name })),
    });
    // The runner shares modules between spec files, so the spy is restored even if this test fails.
    try {
      await render({ searching: true, progress: progress('Albatross', 'Bramble', 'Cobalt') });

      fixture.componentRef.setInput('progress', progress('Driftwood', 'Ember', 'Foxglove'));
      await fixture.whenStable();

      expect(element.textContent).toContain('Driftwood');
      expect(warn).not.toHaveBeenCalled();
    } finally {
      warn.mockRestore();
    }
  });

  it('shows nothing but an empty live region before the first search', async () => {
    await render({
      searching: false,
      progress: { responded: 0, failed: 0, total: 0, pending: [] },
    });

    expect(element.textContent.trim()).toBe('');
    expect(element.querySelector('[role="status"]')).not.toBeNull();
  });
});
