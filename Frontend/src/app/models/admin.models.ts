export interface CreateHubDto {
  name: string;
  address: string;
  contactNumber: string;
  managerName: string;
  email: string;
  isActive: boolean;
}

export interface UpdateHubDto {
  name?: string;
  address?: string;
  contactNumber?: string;
  managerName?: string;
  email?: string;
  isActive?: boolean;
}

export interface CreateServiceLocationDto {
  hubId: string;
  name: string;
  zipCode: string;
  isActive: boolean;
}

export interface UpdateServiceLocationDto {
  hubId: string;
  name: string;
  zipCode: string;
  isActive: boolean;
}

export interface ResolveShipmentDto {
  resolutionNotes: string;
}

export interface DelayShipmentDto {
  reason: string;
}

export interface ReturnShipmentDto {
  reason: string;
}

export interface ResolveExceptionDto {
  description: string;
}

export interface CreateExceptionDto {
  shipmentId: string;
  exceptionType: string;
  description: string;
  status?: string;
  resolvedAt?: string;
}

export interface UpdateExceptionDto {
  exceptionType?: string;
  description?: string;
  status?: string;
  resolvedAt?: string;
}
