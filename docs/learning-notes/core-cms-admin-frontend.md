# Core CMS Admin Frontend

Phase 7 completes the usable administration surface for every core CMS resource already supported by the API: Events, Coaches, Sponsors, FAQs, Content Blocks, Site Settings, and Contact Submissions. Announcement management supplied the reference pattern; the phase reused its protected Server Component loading, same-origin mutation boundary, URL filters, field errors, and confirmation dialogs.

## Common Lifecycle

1. A protected Server Component loads a no-store admin DTO or paged result.
2. The list retains backend-supported filters in search parameters.
3. A module-specific Client Component builds the exact write DTO and performs obvious local validation.
4. A same-origin mutation route reads the server-held session, forwards the request, and sanitizes API errors.
5. The API validates authorization and content, persists the record, and returns the authoritative DTO.
6. The page refreshes or moves from create to edit, where success is visible.

`form-controls.tsx`, `list-controls.tsx`, `resource-action-button.tsx`, and `use-admin-mutation.ts` remove stable repetition. Business fields and lifecycle language stay in module-specific components. This avoids both copy-paste drift and an opaque generic CRUD framework.

## Important Rules

- Event local date-time inputs are converted explicitly to UTC API values.
- Coach email is returned publicly only when both an email exists and `IsEmailPublic` is enabled.
- Coach, Sponsor, and FAQ deletion means deactivation; Event and Content Block deletion is permanent.
- Content Block keys connect records to public page sections. Editing an existing key shows a warning, and conflicts remain `409` errors.
- Site Settings has only GET and PUT paths, preserving its singleton invariant.
- Contact message data stays behind the admin layout and API authorization policy.

## Validation and Testing

Focused Vitest coverage exercises module validation, filter construction, enum/lifecycle behavior, safe errors, and the mutation allowlist. Manual testing covers authentication, CRUD/lifecycle changes, public visibility, singleton persistence, contact status transitions, and cleanup. See [Core CMS admin testing](../guides/core-cms-admin-testing.md).

Heavy visual redesign was intentionally postponed until board members can use these real workflows. Public CMS pages, media upload, rich text, email replies, portals, registration, payments, and Azure provisioning remain deferred.
