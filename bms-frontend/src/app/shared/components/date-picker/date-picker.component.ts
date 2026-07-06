import {
  Component, Input, OnInit, OnDestroy, forwardRef, ChangeDetectionStrategy,
  ChangeDetectorRef,
} from '@angular/core';
import {
  ControlValueAccessor, NG_VALUE_ACCESSOR, NG_VALIDATORS,
  Validator, AbstractControl, ValidationErrors, ReactiveFormsModule,
  FormControl,
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

/**
 * AppDatePickerComponent
 *
 * A reusable standalone wrapper around mat-datepicker that plugs directly into
 * any Reactive Form via `formControlName` / `[formControl]`.
 *
 * Inputs
 * ──────
 *   label       — mat-label text                    (default: "Date")
 *   placeholder — input placeholder                  (default: "Select a date")
 *   prefixIcon  — mat-icon name shown on the left    (default: "event")
 *   minDate     — earliest selectable Date           (default: none)
 *   maxDate     — latest selectable Date             (default: none)
 *   hint        — optional hint text beneath field   (default: "")
 *   required    — whether to show required asterisk  (default: false)
 *
 * The component honours the parent form's touched/dirty state:
 * errors show only after the user has interacted with the field or the
 * parent form calls markAllAsTouched().
 *
 * Value contract
 * ──────────────
 * • Reads  : accepts a Date object or null from the form.
 * • Writes : emits a Date object or null to the form.
 * • The caller is responsible for converting Date → ISO string before the API
 *   call (use the exported `dateToIso` utility function below).
 */

/** Convert a Date to "YYYY-MM-DDT00:00:00.000Z" for API submission. */
export function dateToIso(d: Date | null | undefined): string {
  if (!d) return '';
  const yyyy = d.getFullYear();
  const mm   = String(d.getMonth() + 1).padStart(2, '0');
  const dd   = String(d.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}T00:00:00.000Z`;
}

/** Parse an ISO string / Date into a midnight-normalised Date, or null. */
export function isoToDate(iso: string | Date | null | undefined): Date | null {
  if (!iso) return null;
  const d = typeof iso === 'string' ? new Date(iso) : new Date(iso);
  if (isNaN(d.getTime())) return null;
  d.setHours(0, 0, 0, 0);
  return d;
}

@Component({
  selector: 'app-date-picker',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatDatepickerModule,
  ],
  providers: [
    {
      provide:     NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => AppDatePickerComponent),
      multi:       true,
    },
    {
      provide:     NG_VALIDATORS,
      useExisting: forwardRef(() => AppDatePickerComponent),
      multi:       true,
    },
  ],
  templateUrl: './date-picker.component.html',
  styleUrls:  ['./date-picker.component.scss'],
})
export class AppDatePickerComponent
  implements OnInit, OnDestroy, ControlValueAccessor, Validator {

  // ── Inputs ────────────────────────────────────────────────────────────────
  @Input() label       = 'Date';
  @Input() placeholder = 'Select a date';
  @Input() prefixIcon  = 'event';
  @Input() minDate:  Date | null = null;
  @Input() maxDate:  Date | null = null;
  @Input() hint      = '';
  @Input() required  = false;

  // ── Internal form control ─────────────────────────────────────────────────
  // Owns the actual Date value; separate from the parent FormControl so we
  // can bind it to mat-datepicker without the parent needing to know the
  // datepicker implementation.
  readonly innerCtrl = new FormControl<Date | null>(null);

  isDisabled = false;

  private destroy$  = new Subject<void>();
  private onChange: (v: Date | null) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    // Propagate inner changes up to the parent form
    this.innerCtrl.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(val => {
        this.onChange(val);
        this.onTouched();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── ControlValueAccessor ──────────────────────────────────────────────────

  writeValue(value: Date | string | null): void {
    // Accept both Date objects and ISO strings (for pre-fill from existing records)
    const d = value instanceof Date ? value : isoToDate(value as string | null);
    this.innerCtrl.setValue(d, { emitEvent: false });
    this.cdr.markForCheck();
  }

  registerOnChange(fn: (v: Date | null) => void): void { this.onChange = fn; }
  registerOnTouched(fn: () => void): void               { this.onTouched = fn; }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
    isDisabled ? this.innerCtrl.disable() : this.innerCtrl.enable();
    this.cdr.markForCheck();
  }

  // ── Validator ─────────────────────────────────────────────────────────────
  // Re-exposes the inner control's own validation errors (matDatepickerMin,
  // matDatepickerMax, matDatepickerParse) to the parent form so the parent
  // can also detect them without duplicating logic.
  validate(_: AbstractControl): ValidationErrors | null {
    return this.innerCtrl.errors;
  }

  // ── Expose touched state for the template ─────────────────────────────────
  get isTouched(): boolean { return this.innerCtrl.touched; }

  markAsTouched(): void {
    this.innerCtrl.markAsTouched();
    this.cdr.markForCheck();
  }
}
