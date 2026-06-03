import { Routes } from '@angular/router';
import { CartPageComponent } from './features/cart/pages/cart-page/cart-page';
import { ListingsPageComponent } from './features/listings/pages/listings-page/listings-page';
import { ProductDetailsPageComponent } from './features/listings/pages/product-details-page/product-details-page';
import { OrderDetailsPageComponent } from './features/orders/pages/order-details-page/order-details-page';
import { SellerDashboardPageComponent } from './features/seller/pages/seller-dashboard-page/seller-dashboard-page';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'listings',
  },
  {
    path: 'listings',
    component: ListingsPageComponent,
  },
  {
    path: 'listings/:id',
    component: ProductDetailsPageComponent,
  },
  {
    path: 'cart',
    component: CartPageComponent,
  },
  {
    path: 'orders/:id',
    component: OrderDetailsPageComponent,
  },
  {
    path: 'seller',
    component: SellerDashboardPageComponent,
  },
  {
    path: '**',
    redirectTo: 'listings',
  },
];
