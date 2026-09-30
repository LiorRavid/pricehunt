import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SupplierList } from './supplier-list';

describe('SupplierList', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SupplierList],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('should render the suppliers returned by GET /api/suppliers', async () => {
    const fixture = TestBed.createComponent(SupplierList);
    const httpTesting = TestBed.inject(HttpTestingController);

    TestBed.tick(); // runs the httpResource effect, which sends the request
    httpTesting.expectOne('/api/suppliers').flush([
      { id: 1, name: 'Supplier A' },
      { id: 2, name: 'Supplier B' },
    ]);
    await fixture.whenStable();

    const items = (fixture.nativeElement as HTMLElement).querySelectorAll('li');
    expect(Array.from(items, (li) => li.textContent?.trim())).toEqual(['Supplier A', 'Supplier B']);
    httpTesting.verify();
  });
});
