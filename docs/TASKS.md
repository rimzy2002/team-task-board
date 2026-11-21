# Team Task Board — Verified Milestone Tracker

**As of November 21, 2026:** Architecture approved; **no application code or tests are claimed completed** by creating these planning files. Mark a checkbox only after actual user or tool verification; record commands, environment and results. Target submission: November 22, 2026.

| ID | Milestone | Status | Expected commit |
|---|---|---|---|
| M0 | Repo + React/Vite + ASP.NET Core foundations | LOCALLY VERIFIED | `chore: initialize frontend and backend projects` |
| M1 | DB + Firebase JWT/user upsert + React login/signup/protected routes | LOCALLY VERIFIED | `feat(auth): verify Firebase tokens and persist users` |
| M2 | Projects API/UI owner-scoped | LIVE CREATE AND POST-RESTART AUTHENTICATED LIST VERIFIED (exact browser marker unverified) | `feat(projects): add owner-scoped project management` |
| M3 | Tasks API CRUD, filters, validation, pagination | NOT STARTED | `feat(tasks): implement task CRUD filters and pagination` |
| M4 | Responsive Kanban task UI | NOT STARTED | `feat(board): build responsive task management board` |
| M5 | Firestore live task comments + rules | NOT STARTED | `feat(comments): add secure realtime task comments` |
| M6 | Overdue reporting SQL/UI | NOT STARTED | `feat(reports): add overdue task reporting` |
| M7 | Tests, schema/seed, HTTP API tests, README | NOT STARTED | `test(docs): add automated tests and setup documentation` |
| M8 | Security review, fixes, feature-branch PR, submission | NOT STARTED | Only if fixes are made |

## Template — add this after EACH milestone

### M# — [milestone name]
- Status: NOT STARTED / CODE GENERATED / LOCALLY VERIFIED / BLOCKED
- Scope completed: [exact feature list]
- Files created/modified: [paths]
- Work-executed checks: [commands, dates, actual results or 'not run']
- Local Windows checks: [commands, dates, actual results or 'pending']
- Failures / known gaps: [detail]
- Reviewed staged files and no secrets: [yes/no/pending]
- User's manual commit hash: [only once confirmed]
- User's GitHub push / PR: [only once confirmed]
- Interview notes: [2-4 things learned]
- Planning Chat review: [approved next milestone or pending]

Never claim that creating this tracker constitutes code implementation or that an unrun test passed.

### M1 — React authentication screens and protected route
- Status: LIVE FIREBASE FRONTEND VERIFIED BY USER
- Frontend scope: Firebase email/password signup, login, logout, session restoration, and protected `/` route
- Uncommitted frontend paths reviewed: `frontend/.env.example`, `frontend/package.json`, `frontend/package-lock.json`, `frontend/src/main.jsx`, `frontend/src/config/firebase.js`, `frontend/src/contexts/AuthContext.jsx`, `frontend/src/pages/AuthForm.jsx`, `frontend/src/pages/LoginPage.jsx`, `frontend/src/pages/SignupPage.jsx`, `frontend/src/pages/SignedInPage.jsx`, `frontend/src/routes/ProtectedRoute.jsx`
- Windows checks on November 21, 2026: `npm.cmd run build` passed (Vite 8.3.8), `npm.cmd ls --depth=0` passed, and the existing Vite server returned HTTP 200 at `http://127.0.0.1:5173/`. The package lock matched the manifest and Firebase values in `.env.example` were empty.
- Existing Vite configuration: A fresh headless Chrome session visiting `/` redirected to `/login` and displayed the login form.
- Firebase Auth emulator browser checks: Signup with a disposable account succeeded and redirected to `/`. Refresh restored the session. Logout redirected to `/login`, and a new visit to `/` was redirected again. An incorrect password showed the login error and stayed on `/login`. The correct password signed in and redirected to `/`. The test harness connected the browser to the loopback emulator using demo configuration.
- Live Firebase checks (user verified, November 21, 2026): The new account appeared under Firebase Authentication → Users. Refresh preserved the signed-in session. Logout redirected to `/login` and protected `/`. The wrong password displayed an error, and the correct password signed in successfully.
- Backend M1 authentication: JWT validation and MySQL user sync were implemented in Checkpoint 4. Checkpoint 6 records live-token acceptance and local user reuse after restarting the API.
- Commit scope and credential checks: see Checkpoint 7.
- Interview notes: `AuthContext` waits for Firebase session restoration; `ProtectedRoute` protects browser navigation, while the API must validate ID tokens separately
- Planning Chat review: pending review of M1 before authorization for M2

### M1 Checkpoint 3 — MySQL and EF Core schema
- Status: LOCALLY VERIFIED on an isolated MySQL 8.4.9 instance with a fresh data directory.
- Scope: `users`, `projects`, and `tasks`, mapped by `TeamTaskBoardContext`. Projects reference users, tasks reference projects, and the assignee is optional free text per ADR-09. The migration creates the unique Firebase UID index, the owner index, and the `(project_id, status, priority)` filter index.
- Toolchain: .NET SDK 10.0.401, `MySql.EntityFrameworkCore` 10.0.12, `Microsoft.EntityFrameworkCore.Design` 10.0.12, `dotnet-ef` 10.0.12, and transitive `MySql.Data` 26.10.0.
- Backend commands passed: `dotnet restore backend/TeamTaskBoard.slnx`, `dotnet build backend/TeamTaskBoard.slnx --no-restore` (zero warnings and errors), `dotnet ef migrations add InitialSchema`, `dotnet ef migrations script 0 InitialSchema`, and `dotnet ef migrations has-pending-model-changes` (none). The EF commands used the isolated tool installation.
- SQL review: The Oracle generator omitted the scaffolded column collations. The initial migration now specifies them directly for Firebase UID, task status, and priority. Verified the resulting forward SQL creates tables and indexes and writes migration history, without destructive statements.
- MySQL checks passed: `dotnet ef database update` applied the migration. MySQL accepted distinct case-sensitive UIDs, rejected duplicates and orphaned relationships, enforced required fields and checked task values, and reported the expected filter index and nullable due date and assignee. A second update reported the database up to date and preserved the inserted rows.
- Regression check: The API started without a database connection setting and `GET /health` returned HTTP 200 with `{"status":"ok"}`.
- The persistent development database was outside this verification. The temporary MySQL instance was stopped.

### M1 Checkpoint 4 — Firebase ID tokens and MySQL user synchronization
- Status: VERIFIED WITH LOCALLY SIGNED JWT FIXTURES on Windows with isolated MySQL 8.4.9. Genuine Firebase ID token verification was completed in Checkpoint 6.
- Scope: ASP.NET Core JWT bearer protects `GET /api/auth/me`. The API verifies RS256 and the matching Google certificate by `kid`, exact issuer and audience, expiry, issued-at time, authentication time, and the Firebase UID. The service uses the unique UID to resolve a local ID and synchronizes email only from a verified email claim. `GET /health` remains public.
- Key handling: A direct request to Google's Secure Token certificate endpoint returned HTTP 200 with public certificates and a `Cache-Control` lifetime. The fixture run separately verified certificate caching, rotation on an unfamiliar `kid`, throttled refresh, and rejection when expired keys could not be refreshed.
- Commands passed: `dotnet restore backend/TeamTaskBoard.slnx`, `dotnet build backend/TeamTaskBoard.slnx --no-restore` (zero warnings and errors), `npm.cmd run build` from `frontend`, and `git diff --check`.
- API checks passed: Production-mode Kestrel without Firebase or database settings returned 200 for `/health` and 401 for an unauthenticated `/api/auth/me` request. In the fixture run, `/api/auth/me` returned 401 for missing, malformed, expired, tampered, and wrong-project JWTs and for invalid algorithms, key IDs, and claims. Rejected requests inserted zero rows. A valid signed fixture returned 200 and created one user. Repeated requests reused the local ID. An unverified email did not replace a verified email. Twelve concurrent first requests returned the same local ID and left exactly one row for that UID.
- Database: The existing initial migration applied to a fresh isolated schema. The test MySQL instance was stopped afterwards. The persistent development database was outside this verification.
- Live-token verification is recorded in Checkpoint 6.

### M1 Checkpoint 5 — Frontend-to-API authentication
- Status: Verified with the Firebase Auth emulator, real loopback Kestrel, signed JWT fixtures, and isolated MySQL 8.4.9. See Checkpoint 6 for the persistent database and live-token status.
- Scope: Added `frontend/src/services/api.js` and an explicit **Check API connection** action on the signed-in screen. The service uses the Firebase SDK to get the current token, sends it only to the configured API origin, and forces at most one refresh after a 401. The app code stores no token. Backend CORS permits bearer GET requests from `http://127.0.0.1:5173`.
- Security review: Reviewed token signature and exact claims validation, generic failed authentication responses, signed-token logging settings, Google key expiry and rotation, and the local user service. No JWT code defect requiring a change was found. Local CORS allows only the configured Vite development origin and the Authorization header.
- Commands passed: `dotnet restore backend/TeamTaskBoard.slnx`, `dotnet build backend/TeamTaskBoard.slnx --no-restore` (zero warnings and errors), `dotnet-ef migrations has-pending-model-changes` (none), `npm.cmd run build` from `frontend`, and `git diff --check`.
- Browser checks passed: Firebase Auth emulator signup, redirect, refresh restoration, logout, protected-route redirect, wrong-password error, and valid login. Real Kestrel on port 5080 served `/health` (200) and the allowed CORS preflight (204). A different origin was denied. The browser sent bearer requests to Kestrel, received 401 for the emulator JWT, and retried once after token refresh. A fixture response verified the UI success and loading states, and a simulated connection failure showed the API error.
- Backend regression passed: Reapplied the initial migration to a fresh schema on isolated MySQL. The fixture suite checked token rejection with zero inserts, verified-email handling, stable IDs across repeated and 12 concurrent requests, cache rotation and expiry, and the same user ID and row count after restarting the API host.
- Live sign-off and restart results are recorded in Checkpoint 6.

### M1 Checkpoint 6 — Genuine Firebase end-to-end verification
- User-verified: The live Firebase account appeared under Authentication → Users. Session refresh, logout and protected-route redirect, wrong-password rejection, and correct-password login all passed.
- Codex-verified persistent database: MySQL 8.4.9 at `127.0.0.1:3307`, schema `team_task_board_dev`, in a dedicated data directory outside the repository. The runtime account requires TLS and has `SELECT`, `INSERT`, and `UPDATE` on `users` only. Local credentials are protected by Windows DPAPI.
- Migration: Reviewed the forward SQL before applying `20261121143712_InitialSchema`. Verified `users`, `projects`, `tasks`, the migration history row, and no pending EF model changes. The data directory started empty.
- Codex-verified API: `/health` returned 200. Missing, malformed, and invalid bearer requests returned 401 and left `users` at zero rows. CORS allowed the loopback Vite origin and denied a different origin. Verified that the running frontend uses live Firebase, targets port 5080, and matches the project setting supplied to the API. Restarted the real Kestrel process and confirmed /health returned 200 and the users count stayed unchanged.
- Commands and regression: `dotnet restore backend/TeamTaskBoard.slnx`, `dotnet build backend/TeamTaskBoard.slnx --no-restore`, `npm.cmd run build`, and `git diff --check` passed. The isolated signed-fixture regression confirmed wrong-project and invalid JWT rejection, no rejected-token inserts, one UID row under concurrency, signing-key expiry and rotation, and the same ID after API restart.
- Genuine Firebase acceptance (November 21, 2026): The user clicked **Check API connection** twice in Brave and reported local ID `1` both times. The running Kestrel instance on port 5080 recorded four completed `/api/auth/me` HTTP 200 responses before the latest restart. The persistent database went from zero users to one user, with local ID `1` and exactly one row for the validated Firebase UID associated with that ID. The UID was not printed.
- Restart follow-up (November 21, 2026): The user observed two new `GET http://127.0.0.1:5080/api/auth/me` responses with HTTP 200 in Brave, initiated by `frontend/src/services/api.js`. The app displayed local ID `1`. The active Kestrel process (PID `19980`) recorded the two HTTP 200 completions in `api-20261121-205438-e6713c79.out.log` at lines 72 and 80. The persistent MySQL schema still had one user, one distinct Firebase UID, and exactly one row for the UID mapped to local ID `1`. The public health check returned 200. Checkpoint 6 is complete.
- Final regression: Rebuilt the API and frontend and reran the whitespace check. A fresh signed-out browser redirected to `/login`. The restarted API returned 401 for missing, malformed, and invalid bearer requests and kept the persistent count at one. The allowed Vite origin passed preflight and a different origin was denied. The isolated JWT fixture suite passed wrong-project, expired, tampered, and invalid-claim rejection, rotated-key refresh, cache expiry, and concurrent upsert. A scan of tracked and untracked Git candidates found no credential patterns outside validated lockfile integrity hashes. `frontend/.env.local` remains Git-ignored.

### M1 Checkpoint 7 — Final Git review and handoff
- Status: M1 verification is complete. The user authorized the final commit and push of the reviewed scope on `feature/team-task-board`. The commit hash and push confirmation are recorded in the final handoff report.
- Final checks (November 21, 2026): Reviewed the tracked diff and 21 new files, covering 29 M1 paths. `dotnet build backend/TeamTaskBoard.slnx`, `npm.cmd run build`, `npm.cmd ls --depth=0`, `dotnet-ef migrations has-pending-model-changes`, and `git diff --check` passed. No pending EF model changes.
- Security checks: The live API returned 401 for missing, malformed, and invalid bearer tokens. Persistent MySQL stayed at one user, and CORS allowed only the intended Vite origin in the preflight checks. The isolated suite passed wrong-project and expired token rejection, key rotation and cache expiry, and concurrent user upsert. Scanned 17 tracked and 21 untracked files for credential patterns and unexpected sensitive files. No findings, and `frontend/.env.local` remains Git-ignored.

### M2 — Owner-scoped project management
- Scope: Authenticated `GET /api/projects` lists the current owner's projects newest ID first, and `POST /api/projects` creates a project for that owner and returns 201. Both use the validated Firebase principal and existing `LocalUserService`. The response excludes ownership and Firebase identifiers. Server-generated timestamps are UTC. The protected `/` and `/projects` routes show the project list and creation form.
- Validation: Trimmed name required (up to 200 characters), optional trimmed description (up to 2,000 characters), with blank descriptions stored as null. Malformed JSON, invalid field types, and unknown fields such as `ownerId` or `firebaseUid` return 400 Problem Details.
- Files created: `backend/TeamTaskBoard.Api/Controllers/ProjectsController.cs`, `backend/TeamTaskBoard.Api/DTOs/ProjectDtos.cs`, `backend/TeamTaskBoard.Api/Services/ProjectService.cs`, `frontend/src/pages/ProjectsPage.jsx`, `frontend/src/pages/projects.css`.
- Files modified: `backend/TeamTaskBoard.Api/Program.cs`, `frontend/src/main.jsx`, `frontend/src/pages/SignedInPage.jsx`, `frontend/src/services/api.js`, `docs/TASKS.md`.
- Commands on November 21, 2026: `dotnet restore backend/TeamTaskBoard.slnx`, `dotnet build backend/TeamTaskBoard.slnx --no-restore`, `dotnet-ef migrations has-pending-model-changes`, `npm.cmd run build`, and `git diff --check` passed. The first backend build hit a DLL lock from the running local API. Stopping that process, rebuilding, and restarting the API resolved it. No migration was needed.
- Backend evidence: Used real ASP.NET Core middleware with two signed fixture identities and a fresh isolated MySQL 8.4.9 schema (`task_board_m1_041b043543`). Missing/invalid tokens returned 401. Verified empty lists, 201 creation, deterministic ordering, 400 validation, both directions of owner isolation, direct owner-ID and UTC timestamp checks, and persistence after API restart. SQL-like text was stored literally. Three fixture projects were retained in the isolated schema.
- Browser evidence: Headless Chrome with the Firebase Auth emulator and per-browser Projects API response fixtures passed protected-route, signup/login/logout/refresh and incorrect-password tests, 375px layout, loading/empty states, validation, pending submission, immediate list update, and retry. After a lost POST response the page reloaded the list without retrying the write.
- Live creation (November 21, 2026): The intended Kestrel process on port 5080 (PID `19452`) logged one `POST /api/projects` HTTP 201 (`api-20261121-213329-66e936f2.out.log`, line 40). MySQL `team_task_board_dev` had exactly one `M2 live smoke` row and one project total. The project ID is `1`. Its `owner_id` is `1`, matching the local user mapped to the live Firebase test account in M1. Both UTC timestamps were populated and equal. The test project was retained.
- Live restart and auth regression: Restarted Kestrel (PID `12228`). `/health` returned 200. A direct MySQL comparison confirmed the same ID, owner, and timestamps before and after restart. GET and POST `/api/projects` and GET `/api/auth/me` returned 401 with missing or invalid tokens on the restarted process. The isolated M1 JWT and M2 suite passed again, including key rotation, expiry rejection, two-way project isolation, direct owner checks, and unauthorized ownership fields.
- Browser restart follow-up: The user reported that `M2 live smoke` remained visible after a hard refresh. A uniquely tagged `/health` request returned 200 and appeared in the active API log. The served Vite client targets `127.0.0.1:5080`, with `no-store` for project requests. The active log showed no matching authenticated `GET /api/projects` HTTP 200. Browser reporting is recorded, and the server-side correlation is pending. MySQL independently confirmed exactly one matching row with project ID `1` and `owner_id = 1`.
- Fresh Network follow-up: The browser fields provided were placeholders. A tagged preflight (204) and unauthenticated GET (401) against `/api/projects` on port 5080 both appeared in the active PID `12228` log. The Vite route and configured API destination were verified again. The live owner GET remains pending a matching HTTP 200. The persistent database still has one project, ID `1` with `owner_id = 1`.
- Marked browser follow-up: The user reported `{ project1Visible: true }` from the signed-in client's request with marker `m2-owner-75d23345bf98`. The API logs contained only the separate `-probe` request, which returned 401 as expected. A fresh tagged health request returned 200 and appeared in the active API log. There is still no matching authenticated GET 200. MySQL again confirmed exactly one `M2 live smoke` row and a valid user join for project ID `1`, owner ID `1`. The browser result is user-reported and live log correlation remains pending.
- Correlation diagnostics: Port 5080 still belongs to Kestrel PID `12228`, with HTTP request logging enabled. The log recorded a new tagged unauthenticated GET (401). The client uses `no-store`, the controller sets `NoStore`, and no service worker registration was found in the application source. No proxy was found in WinHTTP, Windows manual proxy or PAC settings, or Brave's startup flags. These checks did not establish the source of the browser's reported 200. A development-only correlation build outside the repository passed a loopback probe for allowlisted marker logging and a matching process/instance response header. Automatic review blocked switching port 5080 to that build, so the ordinary API stayed active. The fallback browser check uses marker `1109d315863f46daa2c4d517ceb6dd1b` and reports Fetch URL, status, type, and service-worker control without displaying credentials. Backend/frontend builds, EF model consistency, and isolated ownership/authentication regressions passed again.
- Verification decision (November 21, 2026): The active port 5080 listener was still Kestrel PID `12228`. Its post-restart log `api-20261121-214050-fa323550.out.log` now contains three authenticated `GET /api/projects` HTTP 200 completions at lines 80, 88, and 96. None has a query string or matches either browser diagnostic marker. MySQL still has one local user (ID `1`), one distinct Firebase UID, and one project, `M2 live smoke` (ID `1`, `owner_id = 1`). The GET endpoint requires authorization and resolves the caller to a local user before filtering by that owner's ID. The exact Brave request and its transfer source remain unverified. No root cause for the missing tagged request was established, and no application code was changed. Supported browser access to the signed-in tab was unavailable. This limits individual request attribution. Live creation, authenticated listing after restart, and isolated two-owner verification support the scoped M2 commit. Live two-account isolation remains UNTESTED.
- Final review (November 21, 2026): Backend restore/build, frontend build, EF model consistency, and `git diff --check` passed. Reran the isolated JWT and M2 API suite, including owner isolation, validation, database ownership, and persistence. Reviewed all ten M2 paths, including five untracked files. The scoped scan found no credential patterns, and the staging area remained empty. A genuine second live Firebase account has not been tested.
- Security: Existing JWT negative, key rotation, local-user upsert and `/api/auth/me` fixture regressions passed. Project ownership is server-derived, the query is parameterized, and the working-tree scan found no credentials or environment/migration changes. No staging, commit or push performed.
- Git handoff: The user accepted the unverified exact Brave request correlation and untested live two-account isolation. They authorized committing and pushing only the ten reviewed M2 paths to `feature/team-task-board`.
- Interview notes: The controller handles HTTP and passes the verified principal to the service. A DTO defines the allowed input and response fields. The service uses `DbContext` to query MySQL by the resolved local owner and save new rows. React loads with abortable requests and updates the list after a successful POST.

## M0 verification record

- Work (Linux): npm ci, npm run build, and dotnet build passed.
- Windows: npm ci — PASSED (19 packages installed).
- Windows: npm run build — PASSED (Vite 8.3.8).
- Windows: React browser test — PASSED (Team Task Board / React is running).
- Windows: dotnet build — PASSED (.NET 10).
- Windows: ASP.NET Core API startup — PASSED.
- Windows: GET /health — PASSED (HTTP 200, {"status":"ok"}).
- Both development servers stopped successfully.
- Git staged-file and basic secret checks: PASSED.
- Git commit: 3203a5f (chore: initialize frontend and backend projects).
- GitHub push: PASSED (feature/team-task-board).
- Final Git working tree: CLEAN.
- M0 Foundation: LOCALLY VERIFIED AND PUSHED.