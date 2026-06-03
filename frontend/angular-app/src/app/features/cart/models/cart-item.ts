export type CartItem = {
  productId: string;
  productVariantId: string;
  sku: string;
  productName: string;
  variantName: string;
  unitPriceAmountMinor: number;
  currency: string;
  quantity: number;
};

export type AddCartItem = Omit<CartItem, 'quantity'>;
