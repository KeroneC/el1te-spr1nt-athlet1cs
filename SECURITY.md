# Security Policy

## Sensitive Data

This platform may eventually store or process information about minors, parents, coaches, emergency contacts, medical notes, proof-of-age documents, orders, donations, testimonials, and feedback. Do not commit real athlete, parent, medical, payment, or identity data.

## Secrets

Never commit secrets, including:

- JWT signing keys
- SQL Server credentials
- Stripe or PayPal keys
- Azure Storage keys or SAS tokens
- SMTP or messaging credentials
- Production CORS origins that expose private infrastructure

Use ASP.NET Core user secrets, environment variables, GitHub Actions secrets, Vercel environment variables, or Azure Key Vault.

## Reporting

For now, report security issues privately to the repository owner. Do not open public issues containing exploit details, credentials, private family data, or document URLs.

## Baseline Expectations

- Use least-privilege authorization for parent, coach, admin, and public workflows.
- Keep uploads private by default.
- Avoid logging tokens, passwords, medical notes, emergency contact details, or document URLs.
- Validate DTOs at API boundaries.
- Restrict production CORS to known frontend domains.
- Add audit trails before production workflows involving minors, payments, documents, or consent.
