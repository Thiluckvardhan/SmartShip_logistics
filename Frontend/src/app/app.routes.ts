import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent)
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent)
  },
  {
    path: 'track',
    loadComponent: () => import('./features/tracking/public-tracking/public-tracking.component').then(m => m.PublicTrackingComponent)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'shipments',
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/shipments/shipment-list/shipment-list.component').then(m => m.ShipmentListComponent)
      },
      {
        path: 'new',
        loadComponent: () => import('./features/shipments/create-shipment/create-shipment.component').then(m => m.CreateShipmentComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./features/shipments/shipment-detail/shipment-detail.component').then(m => m.ShipmentDetailComponent)
      }
    ]
  },
  {
    path: 'tracking',
    canActivate: [authGuard],
    loadComponent: () => import('./features/tracking/tracking/tracking.component').then(m => m.TrackingComponent)
  },
  {
    path: 'documents',
    canActivate: [authGuard],
    loadComponent: () => import('./features/documents/documents/documents.component').then(m => m.DocumentsComponent)
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent)
  },
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    canActivateChild: [authGuard, adminGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/admin/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent)
      },
      {
        path: 'shipments',
        loadComponent: () => import('./features/admin/admin-shipments/admin-shipments.component').then(m => m.AdminShipmentsComponent)
      },
      {
        path: 'shipments/:id',
        loadComponent: () => import('./features/admin/admin-shipment-detail/admin-shipment-detail.component').then(m => m.AdminShipmentDetailComponent)
      },
      {
        path: 'hubs',
        loadComponent: () => import('./features/admin/hubs/hubs.component').then(m => m.HubsComponent)
      },
      {
        path: 'locations',
        loadComponent: () => import('./features/admin/locations/locations.component').then(m => m.LocationsComponent)
      },
      {
        path: 'exceptions',
        loadComponent: () => import('./features/admin/exceptions/exceptions.component').then(m => m.ExceptionsComponent)
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/admin/reports/reports.component').then(m => m.ReportsComponent)
      },
      {
        path: 'users',
        loadComponent: () => import('./features/admin/users/users.component').then(m => m.UsersComponent)
      },
      {
        path: 'pickups',
        loadComponent: () => import('./features/admin/pickups/pickups.component').then(m => m.PickupsComponent)
      }
    ]
  },
  {
    path: 'access-denied',
    loadComponent: () => import('./features/access-denied/access-denied.component').then(m => m.AccessDeniedComponent)
  },
  {
    path: '**',
    loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent)
  }
];
