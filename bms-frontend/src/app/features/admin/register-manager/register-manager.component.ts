import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/models/auth.models';

function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const pw  = control.get('password')?.value;
  const cpw = control.get('confirmPassword')?.value;
  return pw === cpw ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-register-manager',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './register-manager.component.html',
  styleUrls: ['./register-manager.component.scss'],
})
export class RegisterManagerComponent implements OnInit {
  form!: FormGroup;
  loading       = false;
  hidePassword  = true;
  hideConfirm   = true;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router,
    private snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group(
      {
        fullName: ['', [Validators.required, Validators.minLength(2)]],
        email:    ['', [Validators.required, Validators.email]],
        password: ['', [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#^()_+\-={}|[\]\\:;"'<>,.?/`~])/),
        ]],
        confirmPassword: ['', Validators.required],
      },
      { validators: passwordMatchValidator },
    );
  }

  get fullName()        { return this.form.get('fullName')!; }
  get email()           { return this.form.get('email')!; }
  get password()        { return this.form.get('password')!; }
  get confirmPassword() { return this.form.get('confirmPassword')!; }

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.loading = true;

    /**
     * Uses the admin assign-role flow:
     * 1. Register the user (becomes Viewer automatically)
     * 2. Then assign Manager role via /auth/assign-role
     *
     * If the backend adds a dedicated admin-create-user endpoint
     * this can be replaced with a single call.
     */
    const { fullName, email, password } = this.form.value;

    this.http.post<ApiResponse<{ userId?: string; id?: string }>>(`${environment.apiUrl}/auth/register`, { fullName, email, password })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: res => {
          if (res.success) {
            // Attempt to assign Manager role immediately
            const userId = (res.data as any)?.userId ?? (res.data as any)?.id;
            if (userId) {
              this.http.post(`${environment.apiUrl}/auth/assign-role`, { userId, role: 'Manager' }).subscribe();
            }
            this.snackBar.open('Manager account created successfully.', undefined, {
              duration: 4000, panelClass: ['snack-success'],
            });
            this.router.navigate(['/admin/users']);
          } else {
            this.snackBar.open(res.message ?? 'Failed to create account.', 'Dismiss', {
              duration: 5000, panelClass: ['snack-error'],
            });
          }
        },
        error: (err: HttpErrorResponse) => {
          const errors = err.error?.errors as string[] | null;
          const msg = errors?.join(' ') ?? err.error?.message ?? 'Failed to create Manager account.';
          this.snackBar.open(msg, 'Dismiss', { duration: 6000, panelClass: ['snack-error'] });
        },
      });
  }
}
