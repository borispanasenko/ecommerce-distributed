import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URLS } from '../../../core/services/api-config';
import { CatalogProduct, CatalogProductDetails } from '../models/catalog-product';
import { ProductVariantSnapshot } from '../models/product-variant-snapshot';

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

  getProductVariantSnapshot(productVariantId: string): Observable<ProductVariantSnapshot> {
    return this.http.get<ProductVariantSnapshot>(
      `http://localhost:5001/api/products/variants/${productVariantId}/snapshot`,
    );
  }
}
