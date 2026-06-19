# Architecture

## Monorepo

The repository keeps the API, web app, documentation, and CI configuration in one place. This makes shared changes easier to review and helps the team evolve backend and frontend contracts together.

## Backend

The backend starts as a modular monolith using Clean Architecture:

- `Core`: domain entities, enums, DTOs, and repository abstractions.
- `Application`: use case services, application interfaces, and orchestration.
- `Infrastructure`: EF Core, SQL Server access, repositories, external integrations.
- `Api`: HTTP controllers, middleware, authentication setup, Swagger, CORS, health checks.

The dependency direction points inward. API and Infrastructure can depend on Application and Core, but Core does not depend on infrastructure or web concerns.

## Frontend

The web app uses Next.js App Router with TypeScript and Tailwind CSS. It is prepared for Vercel deployment and reads `NEXT_PUBLIC_API_BASE_URL` through `lib/api-client.ts`.

## Modular Monolith

The platform should remain a modular monolith until there is proven operational need for services. Modules can be separated by domain areas such as athletes, events, orders, donations, testimonials, documents, and admin. Shared contracts should stay explicit and internal boundaries should be respected before any future extraction.
