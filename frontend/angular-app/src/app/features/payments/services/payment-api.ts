import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URLS } from '../../../core/services/api-config';
import { CreatePaymentRequest, PaymentDetails, SucceedPaymentRequest } from '../models/payment';

@Injectable({
  providedIn: 'root',
})
export class PaymentApi {
  private readonly http = inject(HttpClient);

  createPayment(request: CreatePaymentRequest): Observable<PaymentDetails> {
    return this.http.post<PaymentDetails>(`${API_BASE_URLS.payment}/api/payments`, request);
  }

  succeedPayment(paymentId: string, request: SucceedPaymentRequest): Observable<PaymentDetails> {
    return this.http.post<PaymentDetails>(
      `${API_BASE_URLS.payment}/api/payments/${paymentId}/succeed`,
      request,
    );
  }
}
