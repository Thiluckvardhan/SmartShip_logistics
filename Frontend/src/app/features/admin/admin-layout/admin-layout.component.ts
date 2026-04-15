import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';

interface NavItem {
  label: string;
  path: string;
  icon: string;
}

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-layout.component.html',
  styleUrls: ['./admin-layout.component.css']
})
export class AdminLayoutComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  currentUser$ = this.authService.currentUser$;
  sidebarCollapsed = signal(false);
  currentPath = '';

  readonly navItems: NavItem[] = [
    { label: 'Dashboard',      path: '/admin',            icon: 'grid' },
    { label: 'Shipments',      path: '/admin/shipments',  icon: 'package' },
    { label: 'Users',          path: '/admin/users',      icon: 'users' },
    { label: 'Exceptions',     path: '/admin/exceptions', icon: 'alert' },
    { label: 'Documents',      path: '/admin/documents',  icon: 'file' },
    { label: 'Hubs',           path: '/admin/hubs',       icon: 'map-pin' },
    { label: 'Locations',      path: '/admin/locations',  icon: 'navigation' },
    { label: 'Pickups',        path: '/admin/pickups',    icon: 'truck' },
  ];

  constructor() {
    this.currentPath = this.router.url;
    this.router.events.pipe(filter(e => e instanceof NavigationEnd))
      .subscribe((e) => { this.currentPath = (e as NavigationEnd).urlAfterRedirects; });
  }

  isActive(path: string): boolean {
    if (path === '/admin') return this.currentPath === '/admin';
    return this.currentPath.startsWith(path);
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  logout(): void {
    this.authService.logout().subscribe();
    this.router.navigate(['/login']);
  }

  openProfile(): void {
    this.router.navigate(['/admin/profile']);
  }

  getIcon(name: string): string {
    const icons: Record<string, string> = {
      'grid': `<i class="fa-solid fa-gauge-high" aria-hidden="true"></i>`,
      'package': `<i class="fa-solid fa-box-open" aria-hidden="true"></i>`,
      'users': `<i class="fa-solid fa-users" aria-hidden="true"></i>`,
      'alert': `<i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i>`,
      'file': `<i class="fa-solid fa-file-lines" aria-hidden="true"></i>`,
      'map-pin': `<i class="fa-solid fa-map-location-dot" aria-hidden="true"></i>`,
      'navigation': `<i class="fa-solid fa-location-arrow" aria-hidden="true"></i>`,
      'truck': `<i class="fa-solid fa-truck" aria-hidden="true"></i>`,
      'bar-chart': `<i class="fa-solid fa-chart-column" aria-hidden="true"></i>`,
    };
    return icons[name] ?? '';
  }
}
