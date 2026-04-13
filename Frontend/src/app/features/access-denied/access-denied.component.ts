import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-access-denied',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div style="text-align:center;padding:4rem 2rem">
      <h1 style="font-size:3rem;color:#dc2626">403</h1>
      <h2>Access Denied</h2>
      <p>You don't have permission to view this page.</p>
      <a routerLink="/dashboard" style="color:#2563eb">Go to Dashboard</a>
    </div>
  `
})
export class AccessDeniedComponent {}
