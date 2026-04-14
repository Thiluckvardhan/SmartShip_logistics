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
      'home': `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>`,
      'plus-circle': `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="16"/><line x1="8" y1="12" x2="16" y2="12"/></svg>`,
      'package': `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="16.5" y1="9.4" x2="7.5" y2="4.21"/><path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 002 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/></svg>`,
      'map-pin': `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0118 0z"/><circle cx="12" cy="10" r="3"/></svg>`,
      'file': `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>`,
      'user': `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>`,
    };
    return icons[name] ?? '';
  }
}
