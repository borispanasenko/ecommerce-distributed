import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map, switchMap } from 'rxjs';

import { CartStore } from '../../../cart/services/cart-store';
import { CatalogProductDetails, CatalogProductVariant } from '../../models/catalog-product';
import { CatalogApi } from '../../services/catalog-api';

@Component({
  selector: 'app-product-details-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './product-details-page.html',
  styleUrl: './product-details-page.css',
})
export class ProductDetailsPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly catalogApi = inject(CatalogApi);

  protected readonly cartStore = inject(CartStore);

  protected readonly product$ = this.route.paramMap.pipe(
    map((params) => params.get('id')),
    switchMap((productId) => {
      if (!productId) {
        throw new Error('Product id route parameter is required.');
      }

      return this.catalogApi.getProductById(productId);
    }),
  );

  protected getPrimaryImage(product: CatalogProductDetails): string | null {
    return product.images.find((image) => image.isPrimary)?.url ?? product.images[0]?.url ?? null;
  }

  protected addVariantToCart(product: CatalogProductDetails, variant: CatalogProductVariant): void {
    this.cartStore.addItem({
      productId: product.id,
      productVariantId: variant.id,
      sku: variant.sku,
      productName: product.name,
      variantName: variant.name,
      unitPriceAmountMinor: variant.priceAmountMinor,
      currency: variant.currency,
    });
  }

  protected formatPrice(priceAmountMinor: number, currency: string): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
    }).format(priceAmountMinor / 100);
  }
}
