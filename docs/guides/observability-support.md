# Observability and Support References

The demo uses privacy-safe server telemetry in the existing workspace-based Application Insights resource. Browser analytics are not enabled. Azure RBAC controls access to raw logs and the **El1te Platform Support** workbook.

## What a reporter should provide

Ask only for:

1. The displayed support reference, including its `ESA-` or `WEB-` prefix.
2. The approximate time and timezone.
3. The page or Admin section where the failure appeared.
4. A short description of what they were trying to do without copying form contents or private athlete information.

Do not ask for passwords, invitation links, session cookies, authentication headers, complete contact messages, athlete information, or screenshots containing private records.

## Find a reported failure

1. Open the demo Application Insights resource in Azure.
2. Open **Workbooks** and select **El1te Platform Support**.
3. Enter the reported value in **Support reference**.
4. Review the correlated operation timeline. Confirm the time, safe route template, `cloud_RoleName`, result code, dependencies, and operation ID.
5. Check **Observed releases** for the `ReleaseSha` active at that time.
6. Match that SHA to the GitHub commit and immutable CI release manifest before proposing a rollback or corrective deployment.

`ESA-` references identify unexpected failures handled by the API or Next.js proxy. `WEB-` references are sanitized Next.js production error digests and are matched to the corresponding server error record.

If the workbook has no result, widen the time range within the 30-day retention window and search Logs:

```kusto
let reference = "ESA-REPLACE_WITH_REFERENCE";
let operations =
    traces
    | where timestamp >= ago(30d)
    | where message has reference or tostring(customDimensions.ReferenceId) == reference
    | distinct operation_Id;
union requests, dependencies, exceptions, traces
| where operation_Id in (operations)
| project timestamp, itemType, cloud_RoleName, operation_Id, name, resultCode, success, message
| order by timestamp asc
```

## Alert response

Operational alerts use the same email recipient configured for grant-budget notifications:

| Alert | Demo threshold | First response |
| --- | --- | --- |
| API readiness | Two failures in 10 minutes | Check `/health/ready`, SQL availability, and the active API deployment |
| Server failures | Five 5xx responses in 10 minutes | Review failure trend, references, role, and release SHA |
| Dependency failures | Five failures in 10 minutes | Identify SQL, Blob, or HTTP dependency and verify its Azure health |
| Request latency | p95 above five seconds with at least 10 requests, sustained twice | Check B1 cold starts, dependencies, request volume, and recent releases |
| Transactional email | Three failed, bounced, suppressed, quarantined, or spam-filtered outcomes in 15 minutes | Open the workbook email section, compare the provider message ID, and confirm sender-domain authentication |

Alerts evaluate every five minutes and use Azure Monitor's stateful auto-mitigation. The action group is notified when an incident becomes active, is not notified on every evaluation while that incident remains active, and the alert auto-resolves after recovery. A later recurrence can open a new incident. A single transient B1 cold start should not trigger a notification.

Record confirmed incidents with start/end time, impact, affected safe route templates, reference IDs, release SHA, cause, resolution, and follow-up. Do not copy raw private telemetry into GitHub issues or public channels.

## Investigate an order email

1. Open the order in **Admin → Merchandise → Orders** and copy its provider message ID. A status of **Accepted by email provider** means Azure accepted the send request, not that the message reached the inbox.
2. In **El1te Platform Support**, enter the ID under **Email provider message ID**.
3. Review only timestamp, operation category, delivery status, and SMTP status. The workbook intentionally omits the recipient address.
4. Treat `Delivered` as transfer to the recipient mail system. Treat `FilteredSpam`, `Quarantined`, `Bounced`, `Suppressed`, and `Failed` as follow-up conditions.
5. Retry an application email only when its application status is Failed. For an accepted message that was filtered, use the one-time tracking-link rotation workflow and a trusted delivery channel.

## Telemetry privacy boundary

Allowed operational fields include timestamps, response status, duration, dependency category, safe route template, anonymous operation IDs, support reference, application role, and release SHA.

Never intentionally collect or add:

- Names, email addresses, phone numbers, athlete or family data.
- Contact, registration, feedback, or Admin form contents.
- Passwords, JWTs, cookies, invitation secrets, authorization headers, or request bodies.
- Uploaded file names, document names, captions, private URLs, or CMS content.
- Authenticated user IDs or record identifiers.

API request telemetry replaces dynamic URLs with route templates and removes user/session context. Next.js error instrumentation records its safe route template and digest, not the raw URL, headers, request body, or error message. Browser JavaScript telemetry remains disabled until the separate privacy-conscious analytics feature is reviewed.

## Release and rollback

Monitoring infrastructure is deployed from Bicep with the application release. The deployment workflow passes the verified immutable commit SHA to both applications and uses `BUDGET_CONTACT_EMAIL` for the alert action group.

After deployment:

1. Confirm the readiness test is green from both locations.
2. Confirm `web` and `api` roles appear separately.
3. Confirm the promoted SHA appears in **Observed releases**.
4. Confirm the four alert rules and email action group are enabled.
5. Confirm normal web-to-API traffic shares an Application Insights operation.

Rollback uses a retained, successful `main` release artifact. Database migrations remain forward-only.
