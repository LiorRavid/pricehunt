import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [App], providers: [provideRouter([])] });
  });

  it('shows the main navigation with links to both screens', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const nav = (fixture.nativeElement as HTMLElement).querySelector('nav[aria-label="Main"]');
    const links = Array.from(nav?.querySelectorAll('a') ?? [], (link) => [
      link.textContent.trim(),
      link.getAttribute('href'),
    ]);
    expect(links).toEqual([
      ['PriceHunt', '/search'],
      ['Search', '/search'],
      ['History', '/history'],
    ]);
  });
});
