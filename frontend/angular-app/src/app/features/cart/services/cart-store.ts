import { computed, Injectable, signal } from '@angular/core';

import { AddCartItem, CartItem } from '../models/cart-item';

@Injectable({
  providedIn: 'root',
})
export class CartStore {
  private readonly itemsSignal = signal<CartItem[]>([]);

  readonly items = this.itemsSignal.asReadonly();

  readonly totalQuantity = computed(() =>
    this.itemsSignal().reduce((total, item) => total + item.quantity, 0),
  );

  addItem(itemToAdd: AddCartItem, quantity = 1): void {
    if (quantity <= 0) {
      return;
    }

    this.itemsSignal.update((items) => {
      const existingItem = items.find(
        (item) => item.productVariantId === itemToAdd.productVariantId,
      );

      if (!existingItem) {
        return [
          ...items,
          {
            ...itemToAdd,
            quantity,
          },
        ];
      }

      return items.map((item) =>
        item.productVariantId === itemToAdd.productVariantId
          ? {
              ...item,
              quantity: item.quantity + quantity,
            }
          : item,
      );
    });
  }

  incrementItem(productVariantId: string): void {
    this.itemsSignal.update((items) =>
      items.map((item) =>
        item.productVariantId === productVariantId
          ? {
              ...item,
              quantity: item.quantity + 1,
            }
          : item,
      ),
    );
  }

  decrementItem(productVariantId: string): void {
    this.itemsSignal.update((items) =>
      items
        .map((item) =>
          item.productVariantId === productVariantId
            ? {
                ...item,
                quantity: item.quantity - 1,
              }
            : item,
        )
        .filter((item) => item.quantity > 0),
    );
  }

  removeItem(productVariantId: string): void {
    this.itemsSignal.update((items) =>
      items.filter((item) => item.productVariantId !== productVariantId),
    );
  }

  clear(): void {
    this.itemsSignal.set([]);
  }
}
