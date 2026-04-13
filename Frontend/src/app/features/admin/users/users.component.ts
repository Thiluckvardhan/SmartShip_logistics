import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../services/user.service';
import { RoleService } from '../services/role.service';
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
  private roleService = inject(RoleService);
  private notificationService = inject(NotificationService);

  users: any[] = [];
  roles: any[] = [];
  isLoading = false;
  selectedRoles: { [userId: string]: string } = {};

  ngOnInit(): void {
    this.loadUsers();
    this.loadRoles();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.userService.getAll().subscribe({
      next: (data) => {
        this.users = data;
        this.users.forEach(u => { this.selectedRoles[u.id] = u.role ?? ''; });
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  loadRoles(): void {
    this.roleService.getAll().subscribe({
      next: (data) => { this.roles = data; },
      error: () => {}
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
    if (!confirm('Delete this user?')) return;
    this.userService.delete(id).subscribe({
      next: () => { this.notificationService.success('User deleted'); this.loadUsers(); },
      error: () => this.notificationService.error('Delete failed')
    });
  }
}
