# Taskflow — Invite Email Delivery Plan

## 1. Current state (baseline)

The invitation *feature* is already fully built and shipped — this plan is not "add invites," it's "make the existing invite flow actually send an email" instead of requiring the admin to manually copy/paste a link.

Confirmed by reading the code:

| Layer | What exists today |
|---|---|
| Backend | `Invitation` entity, `POST/GET/DELETE /api/organization/invitations`, `POST /api/auth/accept-invitation`. `OrganizationService.CreateInvitationAsync` (`server/src/TaskFlow.Infrastructure/Organizations/OrganizationService.cs:33`) creates the row with a random 64-char token and a 7-day expiry, writes an activity-log entry, and returns it — no email is sent anywhere. |
| Frontend | `OrganizationSettingsScreen.tsx` builds the accept link client-side (`buildInviteLink`, using `window.location.origin`) and shows a "Copy link" button for the admin to hand-deliver themselves. `AcceptInvitationScreen.tsx` already handles the token → set-password → join flow. |
| Docs | `docs/legacy/backend-plan.md` (line 79) already flags this gap: *"token... emailed as a link (or, for the demo, surfaced directly in the API response / admin UI, since no email provider is wired up)."* |

So the only gap is delivery. Everything downstream of "invitee clicks the link" already works and needs no changes.

**Email provider: [Brevo](https://www.brevo.com)** — free forever tier (300 emails/day, no credit card), transactional email REST API, single-sender verification (no DNS/domain setup required for a demo). Chosen over Resend (needs domain verification to email arbitrary recipients) and Gmail SMTP (ties the demo to a personal inbox, worse deliverability).

---

## 2. Design

- New `IEmailService` in `TaskFlow.Application` — keeps Infrastructure swappable and keeps Application free of any Brevo-specific types, consistent with the existing `IAuthService`/`IPasswordHasher` pattern.
- `CreateInvitationAsync` sends the email *after* the invitation row is committed, and does **not** fail the request if sending fails — the response gains an `EmailSent` flag so the admin can fall back to the existing copy-link button. A demo email provider going down shouldn't block org admins from inviting people.
- The accept-invitation URL is currently built **client-side** only (`window.location.origin`). Since the email is now composed server-side, the backend needs to know the frontend's public URL too — new `Frontend:BaseUrl` config, mirroring the existing `Cors:AllowedOrigins` pattern (set in Render, blank/localhost in dev).
- Local dev shouldn't require a Brevo account just to run `dotnet test`/`dotnet run`. A `NullEmailService` (logs the would-be email via Serilog instead of calling out) is used whenever `Email:Brevo:ApiKey` is unset — mirrors how `ApplyMigrationsOnStartup` etc. already default sensibly for dev.

---

## 3. Build plan

Each chunk is one PR/commit. Prefix: `PR-E<N>`.

### PR-E0 — Brevo account setup (manual, no code)

- Create a free Brevo account, verify a single sender email (e.g. `invites@<something>` or the account owner's own address — no DNS needed for single-sender verification)
- Generate an API key (Brevo dashboard → SMTP & API → API Keys)
- Hand the key to `/deploy`-adjacent config later (PR-E3) — not committed anywhere

### PR-E1 — `IEmailService` + Brevo implementation

- `TaskFlow.Application/Common/IEmailService.cs`:
  ```csharp
  Task<bool> SendInvitationEmailAsync(
      string toEmail, string organizationName, string inviterName,
      UserRole role, string acceptUrl, DateTime expiresAt,
      CancellationToken cancellationToken);
  ```
  Returns `bool` (sent or not) rather than throwing, so callers don't need try/catch for the expected "provider hiccup" case.
- `TaskFlow.Infrastructure/Email/BrevoEmailService.cs` — typed `HttpClient` posting to `https://api.brevo.com/v3/smtp/email` (`api-key` header, JSON body: sender, recipient, subject, htmlContent). Catches HTTP/network failures, logs via `ILogger<BrevoEmailService>`, returns `false` on any failure instead of throwing.
- `TaskFlow.Infrastructure/Email/NullEmailService.cs` — no-op implementation that logs the invite link at `Information` level; returns `true`.
- New `EmailOptions` (`Email:Brevo:ApiKey`, `Email:FromAddress`, `Email:FromName`, `Frontend:BaseUrl`), bound in `DependencyInjection.AddInfrastructure`.
- DI: `services.AddHttpClient<IEmailService, BrevoEmailService>()` when `Email:Brevo:ApiKey` is configured, else `services.AddSingleton<IEmailService, NullEmailService>()`.
- Simple inline HTML template (no templating engine needed for one email) — org name, inviter's name, role, "Accept invitation" button linking to `{Frontend:BaseUrl}/accept-invitation?token={token}`, expiry note.
- Unit tests (`TaskFlow.UnitTests/Email/`): `BrevoEmailService` against a stubbed `HttpMessageHandler` (success + failure), `NullEmailService` returns `true` and logs.

**Depends on:** nothing new — additive Application/Infrastructure pieces.

### PR-E2 — Wire sending into `CreateInvitationAsync`

- `OrganizationService.CreateInvitationAsync`: after `SaveChangesAsync`, call `emailService.SendInvitationEmailAsync(...)` using `Frontend:BaseUrl` to build the accept URL server-side; capture the returned `bool`.
- `InvitationResponse` gains `bool EmailSent`.
- `IOrganizationService`/`OrganizationService` constructor takes `IEmailService` (already DI-friendly, same pattern as `ICurrentUserService`).
- Unit tests: mock `IEmailService`, assert it's called with the right org/role/link on success; assert invitation creation still succeeds and returns `EmailSent: false` when the mock returns `false` (proves the non-blocking behavior from §2).
- Integration tests: register `NullEmailService` in the `WebApplicationFactory` test setup (no real network calls in CI) — confirm `POST /api/organization/invitations` still 201s and the row is created.

**Depends on:** PR-E1

### PR-E3 — Config wiring (local + Render)

- `appsettings.Development.json`: leave `Email:Brevo:ApiKey` unset (falls back to `NullEmailService` automatically per PR-E1's DI branch) — no local Brevo account needed for `dotnet run`/`dotnet test`. `Frontend:BaseUrl` defaults to `http://localhost:5173`.
- `render.yaml`: add
  | Key | Value |
  |---|---|
  | `Email__Brevo__ApiKey` | `sync: false` (filled in Render dashboard from PR-E0's key) |
  | `Email__FromAddress` | `sync: false` (the verified sender from PR-E0) |
  | `Email__FromName` | `"Taskflow"` |
  | `Frontend__BaseUrl` | `sync: false` (the Vercel URL — already known from `docs/legacy/deployment-plan.md` §5) |
- `docs/legacy/deployment-plan.md` §3 gets a short addendum: fill in the four new env vars alongside the existing `Jwt__Secret`/`Cors__AllowedOrigins` step, sourced from PR-E0.

**Depends on:** PR-E2, PR-E0

### PR-E4 — Frontend: reflect automatic sending

- `client/src/api/organizationApi.ts`: `Invitation`/`CreateInvitationPayload` response gains `emailSent: boolean`.
- `OrganizationSettingsScreen.tsx`:
  - Copy changes from "Create an invite link to share with the new member" → "We'll email them an invite link to join {orgName}."
  - On success: if `emailSent`, toast/banner "Invitation email sent to {email}" (copy-link button stays visible underneath as a fallback, just de-emphasized).
  - If `!emailSent`, keep today's prominent "Invitation created — share this link" box with an added warning line ("Couldn't send the email — share this link instead") so a Brevo outage never blocks onboarding a teammate.
- `client/tests/invitation.spec.ts`: update the mocked `POST /api/organization/invitations` response to include `emailSent: true`, and add a second case (or extend the existing one) asserting the fallback copy-link UI still renders when `emailSent: false`.

**Depends on:** PR-E3 (so the field genuinely reflects backend behavior, not just a UI mock)

---

## 4. Out of scope

- No email templating engine/library — one hardcoded HTML string is enough for a single email type.
- No retry/queue for failed sends — `EmailSent: false` + manual copy-link is the retry mechanism for a demo app.
- No emails for other events (task assigned, comment added, etc.) — invite-only, matching the ask.
- No unsubscribe/preferences — transactional-only, single email type, no marketing content.
