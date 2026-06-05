import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URLS } from '../../../core/services/api-config';
import { CatalogProduct, CatalogProductDetails } from '../models/catalog-product';

@Injectable({
  providedIn: 'root',
})
export class CatalogApi {
  private readonly http = inject(HttpClient);

  getProducts(): Observable<CatalogProduct[]> {
    return this.http.get<CatalogProduct[]>(`${API_BASE_URLS.catalog}/api/products`);
  }

  getProductById(productId: string): Observable<CatalogProductDetails> {
    return this.http.get<CatalogProductDetails>(`${API_BASE_URLS.catalog}/api/products/${productId}`);
  }
}
