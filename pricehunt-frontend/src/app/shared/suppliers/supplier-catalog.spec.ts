import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SupplierCatalog } from './supplier-catalog';

describe('SupplierCatalog', () => {
  let http: HttpTestingController;
  let catalog: SupplierCatalog;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    catalog = TestBed.inject(SupplierCatalog);
  });

  afterEach(() => {
    http.verify();
  });

  it('loads the suppliers from the API', async () => {
    TestBed.tick();
    http.expectOne('/api/suppliers').flush([
      { id: 'albatross-freight', name: 'Albatross Freight' },
      { id: 'gullwing-transport', name: 'Gullwing Transport' },
    ]);
    await TestBed.inject(ApplicationRef).whenStable();

    expect(catalog.suppliers.value()).toEqual([
      { id: 'albatross-freight', name: 'Albatross Freight' },
      { id: 'gullwing-transport', name: 'Gullwing Transport' },
    ]);
  });

  it('starts empty and exposes a failed load as an error', async () => {
    expect(catalog.suppliers.value()).toEqual([]);

    TestBed.tick();
    http
      .expectOne('/api/suppliers')
      .flush('down', { status: 503, statusText: 'Service Unavailable' });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(catalog.suppliers.error()).toBeDefined();
  });
});
