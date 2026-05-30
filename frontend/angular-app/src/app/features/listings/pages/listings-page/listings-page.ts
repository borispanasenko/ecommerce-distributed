import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

type Listing = {
  id: string;
  title: string;
  price: number;
  currency: string;
  sellerName: string;
  condition: 'New' | 'Used';
  location: string;
};

@Component({
  selector: 'app-listings-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './listings-page.html',
  styleUrl: './listings-page.css',
})
export class ListingsPageComponent {
  protected readonly listings: Listing[] = [
    {
      id: 'lst-1001',
      title: 'Mechanical Keyboard Keychron K2',
      price: 89,
      currency: 'USD',
      sellerName: 'Alex Mercer',
      condition: 'Used',
      location: 'Kyiv',
    },
    {
      id: 'lst-1002',
      title: 'Logitech MX Master 3S',
      price: 74,
      currency: 'USD',
      sellerName: 'Nadia Stone',
      condition: 'Used',
      location: 'Lviv',
    },
    {
      id: 'lst-1003',
      title: 'Dell 27" Monitor',
      price: 210,
      currency: 'USD',
      sellerName: 'Victor Lane',
      condition: 'New',
      location: 'Warsaw',
    },
  ];
}
