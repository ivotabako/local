import { Component, computed, inject, signal } from '@angular/core';
import { FormField, form, maxLength, min, minLength, required } from '@angular/forms/signals';
import { ButtonModule } from '@openng/optimus-ui/button';
import { CardModule } from '@openng/optimus-ui/card';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { TableModule } from '@openng/optimus-ui/table';
import { finalize } from 'rxjs';
import { Car } from './models/car';
import { AuthService } from './services/auth.service';
import { CarsService } from './services/cars.service';

@Component({
  selector: 'app-root',
  imports: [FormField, ButtonModule, CardModule, InputTextModule, TableModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly authService = inject(AuthService);
  private readonly carsService = inject(CarsService);

  protected readonly title = signal('LocalEnterprise Cars Platform');
  protected readonly cars = signal<Car[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly statusMessage = signal<string>('');
  protected readonly authError = signal<string>('');

  protected readonly loginModel = signal({
    username: 'apiadmin',
    password: 'ChangeMe_OnlyForLocalDev'
  });
  protected readonly loginForm = form(this.loginModel, (path) => {
    required(path.username, { message: 'Username is required.' });
    required(path.password, { message: 'Password is required.' });
  });

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

  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly saveButtonLabel = computed(() => (this.editingId() ? 'Update Car' : 'Create Car'));

  protected login(event: Event): void {
    event.preventDefault();

    if (this.loginForm().invalid()) {
      this.authError.set('Enter username and password.');
      return;
    }

    const { username, password } = this.loginModel();
    this.authError.set('');
    this.loading.set(true);

    this.authService
      .login(username, password)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => {
          this.statusMessage.set('Authentication successful.');
          this.loadCars();
        },
        error: () => {
          this.authError.set('Authentication failed. Check credentials and auth server.');
        }
      });
  }

  protected logout(): void {
    this.authService.logout();
    this.cars.set([]);
    this.resetCarForm();
    this.statusMessage.set('Signed out.');
  }

  protected loadCars(): void {
    if (!this.isAuthenticated()) {
      return;
    }

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

    if (!this.isAuthenticated()) {
      this.statusMessage.set('Sign in before creating or updating cars.');
      return;
    }

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
    if (!this.isAuthenticated()) {
      return;
    }

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
