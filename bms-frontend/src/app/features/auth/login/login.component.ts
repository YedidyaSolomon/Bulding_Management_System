import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-login',
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
    MatCheckboxModule,
    MatSnackBarModule
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {

  form!: FormGroup;
  loading      = false;
  hidePassword = true;
  sessionExpired = false;
  /** Triggers the CSS shake animation on invalid submit. */
  formShake = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      email:    ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });

    // Show session-expired banner if redirected from auth guard / error interceptor
    this.route.queryParams.subscribe(params => {
      this.sessionExpired = params['reason'] === 'session_expired';
    });
  }

  get email()    { return this.form.get('email')!; }
  get password() { return this.form.get('password')!; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.triggerShake();
      return;
    }

    this.loading = true;

    this.authService.login(this.form.value)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: response => {
          if (response.success) {
            const returnUrl = this.route.snapshot.queryParams['returnUrl'] ?? '/dashboard';
            this.router.navigateByUrl(returnUrl);
          } else {
            this.triggerShake();
            this.snackBar.open(response.message ?? 'Login failed.', 'Dismiss', {
              duration: 4000,
              panelClass: ['snack-error']
            });
          }
        },
        error: (err: HttpErrorResponse) => {
          this.triggerShake();
          const msg = err.error?.message ?? 'Invalid email or password.';
          this.snackBar.open(msg, 'Dismiss', {
            duration: 5000,
            panelClass: ['snack-error']
          });
        }
      });
  }

  private triggerShake(): void {
    this.formShake = true;
    setTimeout(() => (this.formShake = false), 500);
  }
}
