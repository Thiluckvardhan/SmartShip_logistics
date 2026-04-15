import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../core/services/auth.service';

interface NavItem { label: string; path: string; icon: string; }

@Component({
  selector: 'app-customer-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './customer-layout.component.html',
  styleUrls: ['./customer-layout.component.css']
})
export class CustomerLayoutComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  currentUser$ = this.authService.currentUser$;
  menuOpen = signal(false);
  currentPath = '';

  readonly navItems: NavItem[] = [
    { label: 'Dashboard',        path: '/dashboard',     icon: 'home' },
    { label: 'Create Shipment',  path: '/shipments/new', icon: 'plus-circle' },
    { label: 'My Shipments',     path: '/shipments',     icon: 'package' },
    { label: 'Tracking',         path: '/tracking',      icon: 'map-pin' },
    { label: 'Documents',        path: '/documents',     icon: 'file' },
  ];

  constructor() {
    this.currentPath = this.router.url;
    this.router.events.pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(e => {
        this.currentPath = (e as NavigationEnd).urlAfterRedirects;
        this.menuOpen.set(false);
      });
  }

  isActive(path: string): boolean {
    if (path === '/dashboard') return this.currentPath === '/dashboard';
    if (path === '/shipments') return this.currentPath === '/shipments' || /^\/shipments\/[^n]/.test(this.currentPath);
    return this.currentPath.startsWith(path);
  }

  logout(): void {
    this.authService.logout().subscribe();
    this.router.navigate(['/login']);
  }

  openProfile(): void {
    this.router.navigate(['/profile']);
  }

  getIcon(name: string): string {
    const icons: Record<string, string> = {
      'home': `<i class="fa-solid fa-house" aria-hidden="true"></i>`,
      'plus-circle': `<i class="fa-solid fa-circle-plus" aria-hidden="true"></i>`,
      'package': `<i class="fa-solid fa-box" aria-hidden="true"></i>`,
      'map-pin': `<i class="fa-solid fa-location-dot" aria-hidden="true"></i>`,
      'file': `<i class="fa-solid fa-file-lines" aria-hidden="true"></i>`,
      'user': `<i class="fa-solid fa-user" aria-hidden="true"></i>`,
    };
    return icons[name] ?? '';
  }
}
