import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';
import type { Supplier } from './supplier';

/** The supplier catalogue from `GET /api/suppliers`, loaded once and shared by both screens. */
@Injectable({ providedIn: 'root' })
export class SupplierCatalog {
  readonly suppliers = httpResource<readonly Supplier[]>(() => '/api/suppliers', {
    defaultValue: [],
  });
}
