import type { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'search' },
  {
    path: 'search',
    title: 'Search · PriceHunt',
    loadComponent: () => import('./features/search/search-page').then((m) => m.SearchPage),
  },
  {
    path: 'history',
    title: 'History · PriceHunt',
    loadComponent: () => import('./features/history/history-page').then((m) => m.HistoryPage),
  },
  { path: '**', redirectTo: 'search' },
];
