import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { SpinnerComponent } from './shared/components/spinner/spinner.component';
import { NotificationComponent } from './shared/components/notification/notification.component';

const AUTH_ROUTES = ['/login', '/register', '/forgot-password', '/reset-password', '/track', '/admin', '/dashboard', '/shipments', '/tracking', '/documents', '/profile'];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, NavbarComponent, SpinnerComponent, NotificationComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  private router = inject(Router);
  isAuthPage = false;

  constructor() {
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd)
    ).subscribe((e) => {
      const nav = e as NavigationEnd;
      this.isAuthPage = AUTH_ROUTES.some(r => nav.urlAfterRedirects.startsWith(r));
    });
  }
}
