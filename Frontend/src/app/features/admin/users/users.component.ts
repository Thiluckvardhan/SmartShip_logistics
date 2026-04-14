import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../services/user.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.css']
})
export class UsersComponent implements OnInit {
  private userService = inject(UserService);
  private notificationService = inject(NotificationService);

  users: any[] = [];
  readonly roleOptions = ['Admin', 'Customer'];
  isLoading = false;
  selectedRoles: { [userId: string]: string } = {};

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.userService.getAll().subscribe({
      next: (data) => {
        this.users = (data ?? []).map((user: any) => this.normalizeUser(user));
        this.selectedRoles = {};
        this.users.forEach(u => {
          const role = (u.role ?? '').toString().toLowerCase();
          this.selectedRoles[u.id] = role === 'admin' ? 'Admin' : 'Customer';
        });
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  updateRole(userId: string): void {
    const roleName = this.selectedRoles[userId];
    if (!roleName) return;
    this.userService.updateRole(userId, { roleName }).subscribe({
      next: () => this.notificationService.success('Role updated'),
      error: () => this.notificationService.error('Failed to update role')
    });
  }

  delete(id: string): void {
    if (!id) {
      this.notificationService.error('Unable to delete user: missing user id');
      return;
    }

    if (!confirm('Delete this user?')) return;
    this.userService.delete(id).subscribe({
      next: () => { this.notificationService.success('User deleted'); this.loadUsers(); },
      error: () => this.notificationService.error('Delete failed')
    });
  }

  private normalizeUser(user: any): any {
    return {
      id: user.id ?? user.userId ?? user.UserId ?? '',
      name: user.name ?? user.Name ?? '',
      email: user.email ?? user.Email ?? '',
      role: user.role ?? user.Role ?? 'Customer'
    };
  }
}
