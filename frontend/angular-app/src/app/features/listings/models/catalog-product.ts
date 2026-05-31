export type CatalogProductVariant = {
  id: string;
  sku: string;
  name: string;
  priceAmountMinor: number;
  currency: string;
  isActive: boolean;
};

export type CatalogProduct = {
  id: string;
  name: string;
  slug: string;
  brandName: string | null;
  status: string;
  primaryImageUrl: string | null;
  categories: string[];
  variants: CatalogProductVariant[];
};