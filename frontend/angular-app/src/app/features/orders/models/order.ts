export type OrderItem = {
  id: string;
  productId: string;
  productVariantId: string;
  sku: string;
  productName: string;
  variantName: string;
  unitPriceAmountMinor: number;
  currency: string;
  quantity: number;
  lineTotalAmountMinor: number;
  inventoryReservationId: string | null;
};

export type OrderDetails = {
  id: string;
  customerName: string;
  customerEmail: string;
  status: string;
  totalAmountMinor: number;
  currency: string;
  createdAt: string;
  updatedAt: string;
  items: OrderItem[];
};

export type CreateOrderItemRequest = {
  productVariantId: string;
  quantity: number;
};

export type CreateOrderRequest = {
  customerName: string;
  customerEmail: string;
  items: CreateOrderItemRequest[];
};
