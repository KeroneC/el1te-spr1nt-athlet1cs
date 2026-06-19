# Deployment

## Frontend

Deploy `apps/web` to Vercel. Configure:

- Root directory: `apps/web`
- Build command: `npm run build`
- Install command: `npm install`
- Environment variable: `NEXT_PUBLIC_API_BASE_URL`

## API

Deploy the ASP.NET Core API to Azure App Service or Azure Container Apps. Production deployments should use managed identity where possible and keep secrets outside appsettings files.

## Database

Use Azure SQL as the primary SQL Server provider. Apply EF Core migrations through a controlled deployment process after review.

## Uploads

Use Azure Blob Storage private containers for proof-of-age, medical, consent, and other sensitive documents. Public blob containers should not be used for private family or athlete documents.

## CI/CD

GitHub Actions currently restores, builds, and tests the backend and installs, lints, typechecks, and builds the frontend. Deployment jobs can be added after environments and secrets are configured.
