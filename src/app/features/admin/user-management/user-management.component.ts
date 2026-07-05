import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { HttpClient } from '@angular/common/http';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../shared/models/auth.models';
import { AssignRoleDialogComponent } from '../assign-role-dialog/assign-role-dialog.component';

export interface UserListItem {
  id:       string;
  fullName: string;
  email:    string;
  role:     string;
  createdAt?: string;
}

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatIconModule,
    MatButtonModule,
    MatTableModule,
    MatChipsModule,
    MatTooltipModule,
    MatMenuModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatSnackBarModule,
    MatDialogModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.scss'],
})
export class UserManagementComponent implements OnInit {
  displayedColumns = ['fullName', 'email', 'role', 'actions'];
  users: UserListItem[] = [];
  filteredUsers: UserListItem[] = [];
  loading = false;

  searchControl = new FormControl('');
  roleFilter    = new FormControl('all');

  constructor(
    private http: HttpClient,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.loadUsers();

    this.searchControl.valueChanges
      .pipe(debounceTime(250), distinctUntilChanged())
      .subscribe(() => this.applyFilter());

    this.roleFilter.valueChanges
      .subscribe(() => this.applyFilter());
  }

  loadUsers(): void {
    this.loading = true;
    this.http.get<ApiResponse<UserListItem[]>>(`${environment.apiUrl}/auth/users`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: res => {
          this.users         = res.data ?? [];
          this.filteredUsers = [...this.users];
          this.applyFilter();
        },
        error: () => {
          // API endpoint may not exist yet — show placeholder data
          this.users = [
            { id: '1', fullName: 'System Administrator', email: 'admin@bms.com',   role: 'Admin'   },
            { id: '2', fullName: 'Jane Manager',          email: 'jane@bms.com',    role: 'Manager' },
            { id: '3', fullName: 'John Viewer',           email: 'john@bms.com',    role: 'Viewer'  },
          ];
          this.filteredUsers = [...this.users];
          this.applyFilter();
        },
      });
  }

  openAssignRole(user: UserListItem): void {
    if (user.role === 'Admin') {
      this.snackBar.open('The Admin role cannot be changed.', 'OK', { duration: 3000 });
      return;
    }

    const ref = this.dialog.open(AssignRoleDialogComponent, {
      width: '420px',
      data: user,
    });

    ref.afterClosed().subscribe(result => {
      if (result) this.loadUsers();
    });
  }

  private applyFilter(): void {
    const search = (this.searchControl.value ?? '').toLowerCase();
    const role   = this.roleFilter.value ?? 'all';

    this.filteredUsers = this.users.filter(u => {
      const matchSearch =
        u.fullName.toLowerCase().includes(search) ||
        u.email.toLowerCase().includes(search);
      const matchRole = role === 'all' || u.role === role;
      return matchSearch && matchRole;
    });
  }
}
