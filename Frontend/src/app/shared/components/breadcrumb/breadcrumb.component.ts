import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { filter, map } from 'rxjs/operators';

export interface Breadcrumb {
  label: string;
  url: string;
}

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './breadcrumb.component.html',
  styleUrls: ['./breadcrumb.component.css']
})
export class BreadcrumbComponent implements OnInit {
  private router = inject(Router);

  breadcrumbs: Breadcrumb[] = [];

  ngOnInit(): void {
    this.breadcrumbs = this.buildBreadcrumbs(this.router.url);

    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      map((event: NavigationEnd) => event.urlAfterRedirects)
    ).subscribe(url => {
      this.breadcrumbs = this.buildBreadcrumbs(url);
    });
  }

  private buildBreadcrumbs(url: string): Breadcrumb[] {
    const cleanUrl = url.split('?')[0];
    const segments = cleanUrl.split('/').filter(s => s.length > 0);

    if (segments.length === 0) return [];

    return segments.map((segment, index) => ({
      label: this.capitalize(segment),
      url: '/' + segments.slice(0, index + 1).join('/')
    }));
  }

  private capitalize(segment: string): string {
    return segment.charAt(0).toUpperCase() + segment.slice(1).replace(/-/g, ' ');
  }
}
