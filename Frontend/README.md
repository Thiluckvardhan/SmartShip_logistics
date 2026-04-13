# SmartShip Frontend

Angular SPA for the SmartShip logistics platform. Connects to the SmartShip microservices backend via the API Gateway and supports two roles: **Customer** and **Admin**.

## Prerequisites

- Node.js 18+
- Angular CLI 17+

## Setup

```bash
git clone <repo-url>
cd Frontend
npm install
ng serve
```

The app will be available at `http://localhost:4200`.

## Environment Configuration

API base URL is configured in `src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:8000'
};
```

All HTTP service calls use `environment.apiUrl` — no hardcoded URLs. For production, update `src/environments/environment.prod.ts`.

## Build

```bash
# Development
ng build

# Production
ng build --configuration production
```

Output is placed in `dist/`.

## Run Tests

```bash
ng test --watch=false
```

## Folder Structure

```
src/
├── app/
│   ├── core/               # Auth interceptor, guards, singleton services
│   │   ├── interceptors/
│   │   ├── guards/
│   │   └── services/
│   ├── shared/             # Reusable components (navbar, spinner, notification)
│   │   └── components/
│   ├── models/             # TypeScript interfaces matching backend DTOs
│   ├── features/
│   │   ├── auth/           # Login, Register, Forgot/Reset Password
│   │   ├── dashboard/      # Customer dashboard
│   │   ├── shipments/      # Shipment list, detail, create, track
│   │   ├── tracking/       # Public tracking page
│   │   ├── documents/      # Document upload and management
│   │   └── admin/          # Admin dashboard, hubs, locations, reports
│   ├── app.routes.ts
│   ├── app.component.ts
│   └── app.config.ts
├── environments/
│   ├── environment.ts
│   └── environment.prod.ts
├── styles.css
└── index.html
```

## API Gateway

All requests are proxied through: `http://localhost:8000`

## Roles

| Role     | Access                                                      |
|----------|-------------------------------------------------------------|
| Customer | Dashboard, shipments, tracking, documents, profile          |
| Admin    | All customer pages + admin dashboard, hubs, locations, users, reports, exceptions, pickups |
