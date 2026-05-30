import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

type SellerListing = {
  id: string;
  title: string;
  price: number;
  currency: string;
  views: number;
  status: 'Active' | 'Reserved' | 'Sold';
};

@Component({
  selector: 'app-seller-dashboard-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './seller-dashboard-page.html',
  styleUrl: './seller-dashboard-page.css',
})
export class SellerDashboardPageComponent {
  protected readonly sellerName = 'Alex Mercer';

  protected readonly stats = {
    activeListings: 8,
    reservedListings: 2,
    completedOrders: 14,
  };

  protected readonly listings: SellerListing[] = [
    {
      id: 'sl-01',
      title: 'Mechanical Keyboard Keychron K2',
      price: 89,
      currency: 'USD',
      views: 31,
      status: 'Reserved',
    },
    {
      id: 'sl-02',
      title: 'Dell 27" Monitor',
      price: 210,
      currency: 'USD',
      views: 12,
      status: 'Active',
    },
    {
      id: 'sl-03',
      title: 'Logitech MX Keys',
      price: 68,
      currency: 'USD',
      views: 46,
      status: 'Sold',
    },
  ];
}
