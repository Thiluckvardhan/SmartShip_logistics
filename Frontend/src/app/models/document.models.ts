export interface UploadDocumentDto {
  shipmentId: string;
  file: File;
}

export interface UpdateDocumentDto {
  shipmentId: string;
  file?: File;
}

export interface CreateDeliveryProofDto {
  signerName: string;
  notes?: string;
  file: File;
}
