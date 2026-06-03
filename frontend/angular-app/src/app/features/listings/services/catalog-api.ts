import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { CatalogProduct, CatalogProductDetails } from '../models/catalog-product';

@Injectable({
  providedIn: 'root',
})
export class CatalogApi {
  private readonly http = inject(HttpClient);

  getProducts(): Observable<CatalogProduct[]> {
    return this.http.get<CatalogProduct[]>('http://localhost:5001/api/products');
  }

  getProductById(productId: string): Observable<CatalogProductDetails> {
    return this.http.get<CatalogProductDetails>(`http://localhost:5001/api/products/${productId}`);
  }
}