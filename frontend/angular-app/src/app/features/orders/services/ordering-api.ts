import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URLS } from '../../../core/services/api-config';
import { CreateOrderRequest, OrderDetails } from '../models/order';

@Injectable({
  providedIn: 'root',
})
export class OrderingApi {
  private readonly http = inject(HttpClient);

  getOrderById(orderId: string): Observable<OrderDetails> {
    return this.http.get<OrderDetails>(`${API_BASE_URLS.ordering}/api/orders/${orderId}`);
  }

  createOrder(request: CreateOrderRequest): Observable<OrderDetails> {
    return this.http.post<OrderDetails>(`${API_BASE_URLS.ordering}/api/orders`, request);
  }

  cancelOrder(orderId: string): Observable<OrderDetails> {
    return this.http.post<OrderDetails>(`${API_BASE_URLS.ordering}/api/orders/${orderId}/cancel`, null);
  }
}
