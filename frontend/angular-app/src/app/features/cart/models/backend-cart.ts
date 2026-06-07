export type BackendCartItem = {
  id: string;
  productVariantId: string;
  quantity: number;
  createdAt: string;
  updatedAt: string;
};

export type BackendCart = {
  id: string;
  createdAt: string;
  updatedAt: string;
  items: BackendCartItem[];
};

export type AddBackendCartItemRequest = {
  productVariantId: string;
  quantity: number;
};

export type UpdateBackendCartItemRequest = {
  quantity: number;
};
