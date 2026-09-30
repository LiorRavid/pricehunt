import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { RankPosition } from './rank-position';

@Component({
  imports: [RankPosition],
  template: `<div [phRankPosition]="rank()"></div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
class Host {
  readonly rank = signal(2);
}

describe('RankPosition [CL3]', () => {
  let animate: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    animate = vi.fn();
    Object.defineProperty(HTMLElement.prototype, 'animate', {
      value: animate,
      configurable: true,
      writable: true,
    });
  });

  afterEach(() => {
    Reflect.deleteProperty(HTMLElement.prototype, 'animate');
    vi.unstubAllGlobals();
  });

  async function setUp() {
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();
    const element = (fixture.nativeElement as HTMLElement).querySelector('div');
    if (element === null) {
      throw new Error('The host renders no row.');
    }
    Object.defineProperty(element, 'offsetHeight', { value: 72 });
    return { fixture, element };
  }

  it('places the element at its rank without animating the first time', async () => {
    const { element } = await setUp();

    expect(element.style.transform).toBe('translateY(calc(var(--spacing-result-row) * 2))');
    expect(animate).not.toHaveBeenCalled();
  });

  it('animates from the old position to the new one, additively, when the rank changes', async () => {
    const { fixture, element } = await setUp();

    fixture.componentInstance.rank.set(0);
    await fixture.whenStable();

    expect(element.style.transform).toBe('translateY(calc(var(--spacing-result-row) * 0))');
    expect(animate).toHaveBeenCalledWith(
      [{ transform: 'translateY(144px)' }, { transform: 'translateY(0)' }],
      expect.objectContaining({ composite: 'add', duration: 300 }),
    );
  });

  it('does not animate when the user prefers reduced motion', async () => {
    vi.stubGlobal('matchMedia', (query: string) => ({ matches: query.includes('reduce') }));
    const { fixture } = await setUp();

    fixture.componentInstance.rank.set(0);
    await fixture.whenStable();

    expect(animate).not.toHaveBeenCalled();
  });
});
