# Public CMS API

Phase 3 exposes published website content through anonymous, read-only endpoints under `/api/public`.

## Endpoints

- `GET /api/public/site-settings`
- `GET /api/public/content-blocks`
- `GET /api/public/content-blocks/{key}`
- `GET /api/public/announcements?featured=true&page=1&pageSize=10`
- `GET /api/public/announcements/{slug}`
- `GET /api/public/events?eventType=Meet&featured=true&upcomingOnly=true&page=1&pageSize=10`
- `GET /api/public/events/{slug}`
- `GET /api/public/coaches`
- `GET /api/public/sponsors`
- `GET /api/public/faqs?category=Registration`
- `POST /api/public/contact-submissions`

Announcement and event collections default to page 1 with 10 records per page. Page size is capped at 50. Detail routes return `404` for missing, unpublished, future, or expired announcements and for missing or unpublished events.

Only active coaches, sponsors, and FAQs are returned. Coach email addresses are omitted unless the coach explicitly allows public display. Contact submissions are write-only through the public API and are always stored with `New` status.

## Contact Example

```json
{
  "name": "Jordan Parent",
  "email": "jordan@example.com",
  "phone": "555-0100",
  "inquiryType": "Registration",
  "message": "Please send registration information."
}
```

Successful submissions return `201 Created` with only the generated identifier and a confirmation message. Invalid required fields, email addresses, messages, or inquiry types return `400 Bad Request`.

Swagger groups these operations under `Public CMS` when the API runs in the Development environment.
