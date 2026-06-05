import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { CreatePaymentRequest, PaymentDetails, SucceedPaymentRequest } from '../models/payment';

@Injectable({
  providedIn: 'root',
})
export class PaymentApi {
  private readonly http = inject(HttpClient);

  createPayment(request: CreatePaymentRequest): Observable<PaymentDetails> {
    return this.http.post<PaymentDetails>('http://localhost:5004/api/payments', request);
  }

  succeedPayment(paymentId: string, request: SucceedPaymentRequest): Observable<PaymentDetails> {
    return this.http.post<PaymentDetails>(
      `http://localhost:5004/api/payments/${paymentId}/succeed`,
      request,
    );
  }
}
