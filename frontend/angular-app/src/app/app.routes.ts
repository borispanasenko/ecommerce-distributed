import { Routes } from '@angular/router';
import { ListingsPageComponent } from './features/listings/pages/listings-page/listings-page';
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
