import { Component, computed, inject, signal } from '@angular/core';
import { FormField, form, maxLength, min, minLength, required } from '@angular/forms/signals';
import { ButtonModule } from '@openng/optimus-ui/button';
import { CardModule } from '@openng/optimus-ui/card';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { finalize } from 'rxjs';
import { Car } from './models/car';
import { CarsService } from './services/cars.service';

@Component({
  selector: 'app-cars-page',
  standalone: true,
  imports: [FormField, ButtonModule, CardModule, InputTextModule],
  template: `
    <section class="page-card">
      <p-card header="Cars CRUD">
        <div class="actions">
          <button pButton type="button" (click)="loadCars()" [disabled]="loading()">Refresh</button>
        </div>

        <form class="form-grid car-form" novalidate (submit)="saveCar($event)">
          <label>
            Make
            <input pInputText [formField]="carForm.make" />
          </label>
          <label>
            Model
            <input pInputText [formField]="carForm.model" />
          </label>
          <label>
            Year
            <input pInputText type="number" [formField]="carForm.year" />
          </label>
          <label>
            VIN
            <input pInputText [formField]="carForm.vin" />
          </label>
          <div class="button-row">
            <button pButton type="submit" [disabled]="saving() || carForm().invalid()">{{ saveButtonLabel() }}</button>
            <button pButton type="button" severity="secondary" (click)="resetCarForm()">Reset</button>
          </div>
        </form>

        <div class="table-shell">
          <table>
            <thead>
              <tr>
                <th>Make</th>
                <th>Model</th>
                <th>Year</th>
                <th>VIN</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (car of cars(); track car.id) {
                <tr>
                  <td>{{ car.make }}</td>
                  <td>{{ car.model }}</td>
                  <td>{{ car.year }}</td>
                  <td>{{ car.vin }}</td>
                  <td>
                    <div class="row-actions">
                      <button pButton type="button" size="small" (click)="editCar(car)">Edit</button>
                      <button pButton type="button" size="small" severity="danger" (click)="deleteCar(car)">Delete</button>
                    </div>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="5">No cars available.</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        @if (statusMessage()) {
          <p class="status">{{ statusMessage() }}</p>
        }
      </p-card>
    </section>
  `,
  styles: [
    `
      .page-card { width: min(78rem, 100%); margin: 0 auto; }
      .actions, .button-row, .row-actions { display: flex; gap: 0.65rem; flex-wrap: wrap; }
      .form-grid { display: grid; gap: 0.9rem; }
      .form-grid label { display: grid; gap: 0.45rem; font-weight: 600; color: #1b4249; }
      .car-form { margin-bottom: 1rem; grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr)); align-items: end; }
      .table-shell { overflow-x: auto; }
      table { width: 100%; border-collapse: collapse; }
      th, td { padding: 0.8rem 0.65rem; border-bottom: 1px solid #d5e0de; text-align: left; }
      .status { margin-top: 1rem; color: #114a56; font-weight: 600; }
    `
  ]
})
export class CarsPageComponent {
  private readonly carsService = inject(CarsService);

  protected readonly cars = signal<Car[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly statusMessage = signal('');

  protected readonly carModel = signal({
    make: '',
    model: '',
    year: new Date().getFullYear(),
    vin: ''
  });

  protected readonly carForm = form(this.carModel, (path) => {
    required(path.make, { message: 'Make is required.' });
    maxLength(path.make, 80, { message: 'Make cannot exceed 80 characters.' });
    required(path.model, { message: 'Model is required.' });
    maxLength(path.model, 80, { message: 'Model cannot exceed 80 characters.' });
    required(path.year, { message: 'Year is required.' });
    min(path.year, 1886, { message: 'Year must be 1886 or later.' });
    required(path.vin, { message: 'VIN is required.' });
    minLength(path.vin, 5, { message: 'VIN must be at least 5 characters.' });
    maxLength(path.vin, 40, { message: 'VIN cannot exceed 40 characters.' });
  });

  protected readonly saveButtonLabel = computed(() => (this.editingId() ? 'Update Car' : 'Create Car'));

  constructor() {
    this.loadCars();
  }

  protected loadCars(): void {
    this.loading.set(true);
    this.carsService
      .list()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (items) => {
          this.cars.set(items);
          this.statusMessage.set(`Loaded ${items.length} car(s).`);
        },
        error: () => {
          this.statusMessage.set('Failed to load cars. Verify API URL and token scope.');
        }
      });
  }

  protected saveCar(event: Event): void {
    event.preventDefault();

    if (this.carForm().invalid()) {
      this.statusMessage.set('Please fill all required car fields.');
      return;
    }

    const payload = this.carModel();
    const request = {
      make: payload.make.trim(),
      model: payload.model.trim(),
      year: Number(payload.year),
      vin: payload.vin.trim().toUpperCase()
    };

    this.saving.set(true);
    const id = this.editingId();
    const action = id ? this.carsService.update(id, request) : this.carsService.create(request);

    action.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.statusMessage.set(id ? 'Car updated.' : 'Car created.');
        this.resetCarForm();
        this.loadCars();
      },
      error: (error) => {
        const apiError = error?.error?.error;
        this.statusMessage.set(apiError ?? 'Failed to save car.');
      }
    });
  }

  protected editCar(car: Car): void {
    this.editingId.set(car.id);
    this.carForm.make().value.set(car.make);
    this.carForm.model().value.set(car.model);
    this.carForm.year().value.set(car.year);
    this.carForm.vin().value.set(car.vin);
    this.statusMessage.set(`Editing ${car.make} ${car.model}.`);
  }

  protected deleteCar(car: Car): void {
    this.saving.set(true);
    this.carsService
      .delete(car.id)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.statusMessage.set(`Deleted ${car.make} ${car.model}.`);
          if (this.editingId() === car.id) {
            this.resetCarForm();
          }
          this.loadCars();
        },
        error: () => {
          this.statusMessage.set('Delete failed.');
        }
      });
  }

  protected resetCarForm(): void {
    this.editingId.set(null);
    this.carModel.set({
      make: '',
      model: '',
      year: new Date().getFullYear(),
      vin: ''
    });
  }
}