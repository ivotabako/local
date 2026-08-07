export interface Car {
  id: string;
  make: string;
  model: string;
  year: number;
  vin: string;
}

export interface UpsertCarRequest {
  make: string;
  model: string;
  year: number;
  vin: string;
}
