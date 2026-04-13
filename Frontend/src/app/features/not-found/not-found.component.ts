import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div style="text-align:center;padding:4rem 2rem">
      <h1 style="font-size:3rem;color:#64748b">404</h1>
      <h2>Page Not Found</h2>
      <p>The page you're looking for doesn't exist.</p>
      <a routerLink="/dashboard" style="color:#2563eb">Go to Dashboard</a>
    </div>
  `
})
export class NotFoundComponent {}
