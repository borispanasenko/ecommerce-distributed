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

export type CatalogProductBrand = {
  id: string;
  name: string;
  slug: string;
};

export type CatalogProductCategory = {
  id: string;
  name: string;
  slug: string;
};

export type CatalogProductImage = {
  id: string;
  url: string;
  altText: string | null;
  sortOrder: number;
  isPrimary: boolean;
};

export type CatalogProductDetails = {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  brand: CatalogProductBrand | null;
  status: string;
  categories: CatalogProductCategory[];
  variants: CatalogProductVariant[];
  images: CatalogProductImage[];
};