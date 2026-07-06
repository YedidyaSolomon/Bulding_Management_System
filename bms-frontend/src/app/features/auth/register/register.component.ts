import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { HttpErrorResponse } from '@angular/common/http';

// Custom validator: confirm password must match password
function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password        = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return password === confirmPassword ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {

  form!: FormGroup;
  loading      = false;
  hidePassword = true;
  hideConfirm  = true;
  /** Triggers the CSS shake animation on invalid submit. */
  formShake = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group(
      {
        fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
        email:    ['', [Validators.required, Validators.email]],
        password: ['', [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#^()_+\-={}|[\]\\:;"'<>,.?/`~])/)
        ]],
        confirmPassword: ['', Validators.required]
      },
      { validators: passwordMatchValidator }
    );
  }

  get fullName()        { return this.form.get('fullName')!; }
  get email()           { return this.form.get('email')!; }
  get password()        { return this.form.get('password')!; }
  get confirmPassword() { return this.form.get('confirmPassword')!; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.triggerShake();
      return;
    }

    this.loading = true;

    const request = {
      fullName: this.form.value.fullName.trim(),
      email:    this.form.value.email.trim(),
      password: this.form.value.password
    };

    this.authService.register(request)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: response => {
          if (response.success) {
            this.snackBar.open(
              'Registration successful! Please sign in.',
              undefined,
              { duration: 4000, panelClass: ['snack-success'] }
            );
            this.router.navigate(['/auth/login']);
          } else {
            this.triggerShake();
            this.snackBar.open(response.message ?? 'Registration failed.', 'Dismiss', {
              duration: 5000, panelClass: ['snack-error']
            });
          }
        },
        error: (err: HttpErrorResponse) => {
          this.triggerShake();
          const errors = err.error?.errors as string[] | null;
          const msg = errors?.join(' ') ?? err.error?.message ?? 'Registration failed. Please try again.';
          this.snackBar.open(msg, 'Dismiss', {
            duration: 6000, panelClass: ['snack-error']
          });
        }
      });
  }

  private triggerShake(): void {
    this.formShake = true;
    setTimeout(() => (this.formShake = false), 500);
  }
}
