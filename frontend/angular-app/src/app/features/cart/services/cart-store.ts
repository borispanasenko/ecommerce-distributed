import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { BackendCart } from '../models/backend-cart';
import { AddCartItem, CartItem } from '../models/cart-item';
import { CartApi } from './cart-api';

@Injectable({
  providedIn: 'root',
})
export class CartStore {
  private readonly cartApi = inject(CartApi);

  private readonly cartIdStorageKey = 'marketflow.cartId';
  private readonly displayItemsStorageKey = 'marketflow.cartItems';

  private readonly cartIdSignal = signal<string | null>(null);
  private readonly itemsSignal = signal<CartItem[]>([]);

  readonly cartId = this.cartIdSignal.asReadonly();
  readonly items = this.itemsSignal.asReadonly();

  readonly totalQuantity = computed(() =>
    this.itemsSignal().reduce((total, item) => total + item.quantity, 0),
  );

  constructor() {
    this.loadLocalState();

    if (this.cartIdSignal()) {
      void this.refreshFromBackend();
    }
  }

  async addItem(itemToAdd: AddCartItem, quantity = 1): Promise<void> {
    if (quantity <= 0) {
      return;
    }

    const cartId = await this.ensureCart();

    const backendCart = await firstValueFrom(
      this.cartApi.addItem(cartId, {
        productVariantId: itemToAdd.productVariantId,
        quantity,
      }),
    );

    this.applyBackendCart(backendCart, [itemToAdd]);
  }

  async incrementItem(productVariantId: string): Promise<void> {
    const item = this.itemsSignal().find((cartItem) => cartItem.productVariantId === productVariantId);

    if (!item) {
      return;
    }

    await this.updateItemQuantity(productVariantId, item.quantity + 1);
  }

  async decrementItem(productVariantId: string): Promise<void> {
    const item = this.itemsSignal().find((cartItem) => cartItem.productVariantId === productVariantId);

    if (!item) {
      return;
    }

    if (item.quantity <= 1) {
      await this.removeItem(productVariantId);
      return;
    }

    await this.updateItemQuantity(productVariantId, item.quantity - 1);
  }

  async updateItemQuantity(productVariantId: string, quantity: number): Promise<void> {
    if (quantity <= 0) {
      await this.removeItem(productVariantId);
      return;
    }

    const cartId = await this.ensureCart();

    const backendCart = await firstValueFrom(
      this.cartApi.updateItem(cartId, productVariantId, {
        quantity,
      }),
    );

    this.applyBackendCart(backendCart);
  }

  async removeItem(productVariantId: string): Promise<void> {
    const cartId = this.cartIdSignal();

    if (!cartId) {
      this.itemsSignal.update((items) =>
        items.filter((item) => item.productVariantId !== productVariantId),
      );
      this.persistItems();
      return;
    }

    const backendCart = await firstValueFrom(
      this.cartApi.removeItem(cartId, productVariantId),
    );

    this.applyBackendCart(backendCart);
  }

  async clear(): Promise<void> {
    const cartId = this.cartIdSignal();

    if (cartId) {
      const backendCart = await firstValueFrom(this.cartApi.clearCart(cartId));
      this.applyBackendCart(backendCart);
      return;
    }

    this.itemsSignal.set([]);
    this.persistItems();
  }

  async refreshFromBackend(): Promise<void> {
    const cartId = this.cartIdSignal();

    if (!cartId) {
      return;
    }

    try {
      const backendCart = await firstValueFrom(this.cartApi.getCartById(cartId));
      this.applyBackendCart(backendCart);
    } catch (error) {
      console.warn('Failed to refresh cart from backend', error);
    }
  }

  private async ensureCart(): Promise<string> {
    const existingCartId = this.cartIdSignal();

    if (existingCartId) {
      return existingCartId;
    }

    const backendCart = await firstValueFrom(this.cartApi.createCart());

    this.cartIdSignal.set(backendCart.id);
    this.persistCartId();
    this.applyBackendCart(backendCart);

    return backendCart.id;
  }

  private applyBackendCart(backendCart: BackendCart, additionalSnapshots: AddCartItem[] = []): void {
    this.cartIdSignal.set(backendCart.id);

    const snapshots = new Map<string, AddCartItem>();

    for (const item of this.itemsSignal()) {
      snapshots.set(item.productVariantId, {
        productId: item.productId,
        productVariantId: item.productVariantId,
        sku: item.sku,
        productName: item.productName,
        variantName: item.variantName,
        unitPriceAmountMinor: item.unitPriceAmountMinor,
        currency: item.currency,
      });
    }

    for (const snapshot of additionalSnapshots) {
      snapshots.set(snapshot.productVariantId, snapshot);
    }

    const displayItems = backendCart.items.map((backendItem) => {
      const snapshot = snapshots.get(backendItem.productVariantId);

      return {
        productId: snapshot?.productId ?? '',
        productVariantId: backendItem.productVariantId,
        sku: snapshot?.sku ?? backendItem.productVariantId,
        productName: snapshot?.productName ?? 'Product variant',
        variantName: snapshot?.variantName ?? backendItem.productVariantId,
        unitPriceAmountMinor: snapshot?.unitPriceAmountMinor ?? 0,
        currency: snapshot?.currency ?? 'USD',
        quantity: backendItem.quantity,
      };
    });

    this.itemsSignal.set(displayItems);
    this.persistCartId();
    this.persistItems();
  }

  private loadLocalState(): void {
    const storage = this.getStorage();

    if (!storage) {
      return;
    }

    const cartId = storage.getItem(this.cartIdStorageKey);

    if (cartId) {
      this.cartIdSignal.set(cartId);
    }

    const rawItems = storage.getItem(this.displayItemsStorageKey);

    if (!rawItems) {
      return;
    }

    try {
      const items = JSON.parse(rawItems) as CartItem[];

      if (Array.isArray(items)) {
        this.itemsSignal.set(items);
      }
    } catch {
      storage.removeItem(this.displayItemsStorageKey);
    }
  }

  private persistCartId(): void {
    const storage = this.getStorage();

    if (!storage) {
      return;
    }

    const cartId = this.cartIdSignal();

    if (cartId) {
      storage.setItem(this.cartIdStorageKey, cartId);
    } else {
      storage.removeItem(this.cartIdStorageKey);
    }
  }

  private persistItems(): void {
    const storage = this.getStorage();

    if (!storage) {
      return;
    }

    storage.setItem(this.displayItemsStorageKey, JSON.stringify(this.itemsSignal()));
  }

  private getStorage(): Storage | null {
    try {
      return globalThis.localStorage;
    } catch {
      return null;
    }
  }
}
