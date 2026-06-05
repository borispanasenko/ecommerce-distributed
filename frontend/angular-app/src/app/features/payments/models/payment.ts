export type PaymentDetails = {
  id: string;
  orderId: string;
  amountMinor: number;
  currency: string;
  status: string;
  provider: string;
  providerReference: string | null;
  failureReason: string | null;
  createdAt: string;
  updatedAt: string;
  succeededAt: string | null;
  failedAt: string | null;
  cancelledAt: string | null;
};

export type CreatePaymentRequest = {
  orderId: string;
  amountMinor: number;
  currency: string;
  provider: string;
};

export type SucceedPaymentRequest = {
  providerReference: string;
};
