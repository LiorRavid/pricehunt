import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

/** The shell: a top navigation bar over the routed page. */
@Component({
  selector: 'ph-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly links = [
    { path: '/search', label: 'Search' },
    { path: '/history', label: 'History' },
  ] as const;
}
