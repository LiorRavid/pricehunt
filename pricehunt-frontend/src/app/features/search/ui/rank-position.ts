import { afterRenderEffect, computed, Directive, ElementRef, inject, input } from '@angular/core';

const ANIMATION: KeyframeAnimationOptions = {
  duration: 300,
  easing: 'cubic-bezier(0.2, 0, 0, 1)',
  composite: 'add',
};

/**
 * Places an absolutely positioned row at its rank and animates rank changes (CL3, ADR-004). Only
 * `transform` changes, so re-sorting never shifts layout. Animations are additive and start after
 * render, so they survive Angular moving the node and stay smooth when a new re-rank interrupts one.
 */
@Directive({
  selector: '[phRankPosition]',
  host: { '[style.transform]': 'transform()' },
})
export class RankPosition {
  readonly rank = input.required<number>({ alias: 'phRankPosition' });

  protected readonly transform = computed(
    () => `translateY(calc(var(--spacing-result-row) * ${String(this.rank())}))`,
  );

  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private previousRank: number | undefined;

  constructor() {
    afterRenderEffect({
      earlyRead: () => ({ rank: this.rank(), rowHeight: this.element.offsetHeight }),
      write: (measured) => {
        const { rank, rowHeight } = measured();
        const previous = this.previousRank;
        this.previousRank = rank;
        if (previous === undefined || previous === rank || !this.canAnimate()) {
          return;
        }

        this.element.animate(
          [
            { transform: `translateY(${String((previous - rank) * rowHeight)}px)` },
            { transform: 'translateY(0)' },
          ],
          ANIMATION,
        );
      },
    });
  }

  private canAnimate(): boolean {
    // jsdom and older engines lack one or both APIs.
    const reducedMotion =
      typeof globalThis.matchMedia === 'function' &&
      globalThis.matchMedia('(prefers-reduced-motion: reduce)').matches;
    return typeof this.element.animate === 'function' && !reducedMotion;
  }
}
