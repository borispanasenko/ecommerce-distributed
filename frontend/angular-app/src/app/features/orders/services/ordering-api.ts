import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { CreateOrderRequest, OrderDetails } from '../models/order';

@Injectable({
  providedIn: 'root',
})
export class OrderingApi {
  private readonly http = inject(HttpClient);

  getOrderById(orderId: string): Observable<OrderDetails> {
    return this.http.get<OrderDetails>(`http://localhost:5002/api/orders/${orderId}`);
  }

  createOrder(request: CreateOrderRequest): Observable<OrderDetails> {
    return this.http.post<OrderDetails>('http://localhost:5002/api/orders', request);
  }
}
