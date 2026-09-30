# ADR-004: Frontend state, stale-event guard and flicker-free ordering

- Status: Accepted
- Date: 2026-09-30
- Requirements: CL1, CL2, CL3, CL4, CL5

## Context

Quotes arrive in random order over up to 6 seconds. The list must stay sorted cheapest-first (CL2) without flicker or jumping (CL3). It must show progress and a clear final state (CL4). A new search must cancel the old one, and a late result from the old search must never appear (CL5).

The app uses Angular 22 with zoneless change detection and signals.

## Decision

**State.** A signal store scoped to the search route holds the state. A **pure reducer** drives it through a state machine: `idle → searching → completed | timedOut | cancelled | error`. The reducer is the main unit-test target. Components read derived `computed()` values: sorted rows, progress text and the final badge.

**Cancellation and stale events, in three layers:**

1. **Transport.** Searches run through RxJS `switchMap`. Starting a new search unsubscribes from the previous stream, and unsubscribing calls `AbortController.abort()`, which cancels the request and, through `RequestAborted`, the supplier calls.
2. **Attempt tag.** Every action the stream dispatches carries the client-side attempt number it belongs to. The reducer drops actions from any other attempt.
3. **Search id and terminal state.** The reducer also drops events whose `searchId` isn't the active search, and anything that arrives after a terminal event.

A Cancel button moves the store to `cancelled`. Leaving the search page cancels a search in progress.

**Flicker-free ordering:**

- As soon as a search starts, there is **one fixed-height row slot per selected supplier**, with a skeleton while it's pending. The list's height never changes during a search.
- **Row order:**
  - Priced rows first, ascending by price, then response time, then supplier name.
  - Then failed rows.
  - Then pending and no-response rows.
- Rows are rendered with `@for (…; track supplierId)` **in sorted DOM order**, so the reading order for screen readers and tests always matches what's on screen. Angular moves the existing DOM nodes rather than recreating them.
- **Rows are absolutely positioned** at `translateY(rank × rowHeight)`, so reordering never changes layout, and Cumulative Layout Shift is 0 by construction.
- **Movement.** A small directive animates rank changes with **additive Web Animations API transform animations**, started after each render.
  - CSS transitions are cancelled when Angular moves a node in the DOM, so a moved row would jump. A WAAPI animation started after the move isn't affected.
  - Additive animations also stay continuous when a row is re-ranked mid-animation.
  - Under `prefers-reduced-motion`, rows move without animation.

**Progress:**

- "X of N suppliers responded". Failures count as responded, and failures also get their own count.
- Chips for the pending suppliers.
- A deadline bar animated with `transform: scaleX` over the server's `maxDurationMs`, timed from when `search-started` arrives. Timing from arrival makes clock differences between client and server irrelevant.
- The final badge (Completed, Timed out naming the silent suppliers, Cancelled or Error) sits in a `role="status"` region that is always present, so screen readers announce it.

## Alternatives considered

- **Stable DOM order, with only `translateY` changing and CSS transitions.** DOM nodes never move, but the DOM order wouldn't match the visual order. That fails WCAG 1.3.2 (meaningful sequence) and makes "is the list sorted?" hard to check reliably.
- **Sorted DOM plus classic FLIP with CSS transitions.** Moved nodes lose their transition and jump, and the row reflow can register as layout shift.
- **Keying `@for` on the index, or re-creating the list.** Rows would be torn down and rebuilt, which is exactly the flicker CL3 forbids.
- **NgRx or another state library.** One feature's state doesn't need it. A pure reducer with signals is smaller and just as testable.

## Consequences

- Every row has a fixed height, so long supplier names are truncated with a tooltip.
- The "no flicker" claim is checked in a real browser (C2 checks 1–3): row elements stay the same objects, prices are always in ascending DOM order, and a performance trace shows no layout shift.
