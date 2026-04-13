export enum ShipmentStatus {
  Draft = 'Draft',
  Booked = 'Booked',
  PickedUp = 'PickedUp',
  InTransit = 'InTransit',
  OutForDelivery = 'OutForDelivery',
  Delivered = 'Delivered',
  Delayed = 'Delayed',
  Failed = 'Failed',
  Returned = 'Returned',
}

export interface AddressDto {
  name: string;
  phone: string;
  street: string;
  city: string;
  state: string;
  country: string;
  pincode: string;
}

export interface ShipmentItemDto {
  itemName: string;
  quantity: number;
  weight: number;
  description?: string;
}

export interface CreateShipmentDto {
  senderAddress: AddressDto;
  receiverAddress: AddressDto;
  items: ShipmentItemDto[];
}

export interface UpdateShipmentDto {
  items: ShipmentItemDto[];
}

export interface UpdateShipmentStatusDto {
  status: ShipmentStatus;
}

export interface CalculateRateDto {
  totalWeight: number;
}

export interface CreateAddressDto {
  name: string;
  phone: string;
  street: string;
  city: string;
  state: string;
  country: string;
  pincode: string;
}

export interface CreatePackageDto {
  weight: number;
  description?: string;
  itemName?: string;
  quantity?: number;
}

export interface UpdatePackageDto {
  weight?: number;
  description?: string;
  itemName?: string;
  quantity?: number;
}

export interface CreatePickupDto {
  shipmentId: string;
  pickupDate: string;
  notes?: string;
}

export interface UpdatePickupDto {
  pickupDate?: string;
  notes?: string;
}
