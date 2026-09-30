import { httpResource } from '@angular/common/http';
import { Component } from '@angular/core';

interface Supplier {
  id: number;
  name: string;
}

@Component({
  imports: [],
  selector: 'app-supplier-list',
  styleUrl: './supplier-list.css',
  templateUrl: './supplier-list.html',
})
export class SupplierList {
  protected readonly suppliers = httpResource<Supplier[]>(() => '/api/suppliers', {
    defaultValue: [],
  });
}
