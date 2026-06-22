# Glossary

| Term | Meaning in this project |
| --- | --- |
| API | The ASP.NET Core interface clients call over HTTP. |
| Endpoint | One HTTP method and route, such as `GET /api/auth/me`. |
| DTO | Data Transfer Object: an explicit request/response shape separate from persistence entities. |
| Entity | An EF Core domain object with identity and stored state, such as `Announcement`. |
| Repository | An abstraction/implementation that reads and writes entities or projections through EF Core. |
| Service | Application code that coordinates validation, business rules, and repositories for a use case. |
| Dependency injection | Registration and construction of services in `Program.cs` rather than manual creation in controllers. |
| DbContext | EF Core's unit for model mapping, queries, change tracking, and saving; here `El1teDbContext`. |
| EF Core | .NET object-relational mapper used to query and update SQL Server. |
| Migration | Versioned code describing a database schema transition. |
| Seed data | Initial non-sensitive data inserted for development or baseline content. |
| Authentication | Proving who the caller is, currently with a validated JWT. |
| Authorization | Deciding what an authenticated caller may do, such as the `CmsAdmin` policy. |
| JWT | Signed JSON Web Token containing identity claims and an expiration. It is a bearer credential. |
| Claim | A statement in an authenticated identity, such as user ID, email, or role. |
| Role | Broad user category: Parent, Athlete, Coach, Admin, or SuperAdmin. |
| Policy | Named ASP.NET Core authorization rules; `CmsAdmin` checks auth, role, and current active state. |
| HttpOnly cookie | A browser cookie unavailable to JavaScript; Next.js uses one to hold the web JWT session. |
| Server Component | A Next.js React component rendered on the server, suitable for protected data loading. |
| Client Component | A React component marked `"use client"` for browser state and events. |
| Route Handler | Server-side Next.js HTTP code under `app/api`, used here for login/logout and mutations. |
| Slug | URL-friendly stable text identifier, such as `summer-registration`. |
| Pagination | Splitting a collection into pages with page, page size, total count, and total pages. |
| Filtering | Restricting a query by values such as published, featured, active, category, or search text. |
| `401 Unauthorized` | Authentication is missing or invalid, despite the historical status name. |
| `403 Forbidden` | Authentication succeeded but permission is insufficient. |
| `404 Not Found` | A resource does not exist or is deliberately not visible through that route. |
| `409 Conflict` | The requested state conflicts with existing data, commonly uniqueness. |
| Unit test | Fast isolated test of a service or function using controlled dependencies. |
| Integration test | Test of cooperating runtime pieces, such as hosted auth middleware and policies. |
| Mock | Controlled substitute for a dependency. This repository also uses simple fakes in tests. |
| CORS | Browser policy and API headers governing cross-origin requests. It is not authentication. |
| Environment variable | Process configuration value; double underscores map nested .NET keys, for example `Jwt__Key`. |
| User Secrets | Development configuration stored outside the repository by the .NET secret manager. |
