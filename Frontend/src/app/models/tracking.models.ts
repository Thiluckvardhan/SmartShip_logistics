export interface CreateTrackingEventDto {
  eventId: number;
  trackingNumber: string;
  status: string;
  location: string;
  description?: string;
  timestamp: string;
}

export interface UpdateTrackingEventDto {
  eventId: number;
  trackingNumber: string;
  status: string;
  location: string;
  description?: string;
  timestamp: string;
}

export interface UpdateTrackingStatusDto {
  status: string;
  location?: string;
  description?: string;
}

export interface CreateTrackingLocationDto {
  trackingNumber: string;
  latitude: number;
  longitude: number;
  timestamp: string;
}
