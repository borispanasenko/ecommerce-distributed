import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AddBackendCartItemRequest,
  BackendCart,
  UpdateBackendCartItemRequest,
} from '../models/backend-cart';

@Injectable({
  providedIn: 'root',
})
export class CartApi {
  private readonly http = inject(HttpClient);

  createCart(): Observable<BackendCart> {
    return this.http.post<BackendCart>('http://localhost:5005/api/carts', {});
  }

  getCartById(cartId: string): Observable<BackendCart> {
    return this.http.get<BackendCart>(`http://localhost:5005/api/carts/${cartId}`);
  }

  addItem(cartId: string, request: AddBackendCartItemRequest): Observable<BackendCart> {
    return this.http.post<BackendCart>(
      `http://localhost:5005/api/carts/${cartId}/items`,

      request,
    );
  }

  updateItem(
    cartId: string,

    productVariantId: string,

    request: UpdateBackendCartItemRequest,
  ): Observable<BackendCart> {
    return this.http.put<BackendCart>(
      `http://localhost:5005/api/carts/${cartId}/items/${productVariantId}`,

      request,
    );
  }

  removeItem(cartId: string, productVariantId: string): Observable<BackendCart> {
    return this.http.delete<BackendCart>(
      `http://localhost:5005/api/carts/${cartId}/items/${productVariantId}`,
    );
  }

  clearCart(cartId: string): Observable<BackendCart> {
    return this.http.post<BackendCart>(
      `http://localhost:5005/api/carts/${cartId}/clear`,

      {},
    );
  }
}
