import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

type OrderStatus =
  | 'Created'
  | 'InventoryReserved'
  | 'PaymentProcessing'
  | 'Paid'
  | 'ShipmentCreated'
  | 'Completed';

type TimelineEvent = {
  label: string;
  status: 'done' | 'current' | 'upcoming';
  timestamp: string;
};

@Component({
  selector: 'app-order-details-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './order-details-page.html',
  styleUrl: './order-details-page.css',
})
export class OrderDetailsPageComponent {
  protected readonly order = {
    id: 'ord-9001',
    listingTitle: 'Mechanical Keyboard Keychron K2',
    buyerName: 'Boris Panasenko',
    sellerName: 'Alex Mercer',
    amount: 89,
    currency: 'USD',
    status: 'PaymentProcessing' as OrderStatus,
  };

  protected readonly timeline: TimelineEvent[] = [
    {
      label: 'Order created',
      status: 'done',
      timestamp: '2026-04-12 10:01',
    },
    {
      label: 'Inventory reserved',
      status: 'done',
      timestamp: '2026-04-12 10:02',
    },
    {
      label: 'Payment processing',
      status: 'current',
      timestamp: '2026-04-12 10:03',
    },
    {
      label: 'Shipment created',
      status: 'upcoming',
      timestamp: 'Pending',
    },
    {
      label: 'Completed',
      status: 'upcoming',
      timestamp: 'Pending',
    },
  ];
}
