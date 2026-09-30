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

1. **Transport.** Searches run through RxJS `switchMap`. Starting a new search unsubscribes from the previous stream, and unsubscribing from a stream that hasn't finished calls `AbortController.abort()`, which cancels the request and, through `RequestAborted`, the supplier calls (ADR-001).
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
- Chips for the pending suppliers, tracked by supplier id. Tracking the name strings by identity made Angular warn (`NG0956`) whenever a new search replaced every chip.
- A deadline bar animated with `transform: scaleX` over the server's `maxDurationMs`, timed from when `search-started` arrives. Timing from arrival makes clock differences between client and server irrelevant.
- The final badge (Completed, Timed out naming the silent suppliers, Cancelled or Error) sits in a `role="status"` region that is always present, so screen readers announce it.
- **The panel keeps one height from the start of a search to its end,** because anything above the list that changes height moves the whole list:
  - The count takes a single line.
  - The deadline bar's track stays in place after the search.
  - The chips and, later, the badge share one fixed-height line. The chips stay on that line, fading out at the edge if there are too many, and a long badge detail is truncated, with the full text in its tooltip and in the announcement.
  - The Search and Cancel buttons are the same height, so the button row doesn't grow when Cancel appears.

## Alternatives considered

- **Stable DOM order, with only `translateY` changing and CSS transitions.** DOM nodes never move, but the DOM order wouldn't match the visual order. That fails WCAG 1.3.2 (meaningful sequence) and makes "is the list sorted?" hard to check reliably.
- **Sorted DOM plus classic FLIP with CSS transitions.** Moved nodes lose their transition and jump, and the row reflow can register as layout shift.
- **Keying `@for` on the index, or re-creating the list.** Rows would be torn down and rebuilt, which is exactly the flicker CL3 forbids.
- **NgRx or another state library.** One feature's state doesn't need it. A pure reducer with signals is smaller and just as testable.

## Consequences

- Every row has a fixed height, so long supplier names are truncated with a tooltip.
- The "no flicker" claim is checked in a real browser (C2 checks 1–3). In Phase 6, during a live search with all seven suppliers:
  - The seven rows marked at the start were still the same connected elements after the list had passed through six different orders.
  - All 234 samples of the rendered prices were in ascending DOM order.
  - The 19 animations were additive, transform-only and 300 ms long, and the list height stayed at 504 px throughout.
  - With reduced motion emulated there were no animations.
- Phase 8 measured layout stability with performance traces:
  - The first trace of a live search showed **CLS 0.18**. The pending chips wrapped onto fewer lines as suppliers answered, and the list below moved with them. The end-to-end suite now checks that the list keeps its position and height for a whole search. That check fails on the old layout, where the list sat at four different heights.
  - After the fix, two searches in a row gave CLS 0.01. What remains is the pending chips sliding left as each one disappears. The rows never move except by transform.
  - At 4× CPU slowdown on Fast 3G: CLS 0.00, INP 173 ms, no long tasks, and the list was sorted after every DOM change.
- The history screen had CLS 0.57 on load: pagination showed "No results" under a five-row skeleton, then the first 20 rows pushed it down. Pagination now appears with the first page. The vertical scrollbar also nudged the centred page sideways when it appeared, so `html` reserves its space (`scrollbar-gutter: stable`). Load CLS is now 0.003.
