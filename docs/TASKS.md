# Team Task Board — Verified Milestone Tracker

**As of November 21, 2026:** Architecture approved; **no application code or tests are claimed completed** by creating these planning files. Mark a checkbox only after actual user or tool verification; record commands, environment and results. Target submission: November 22, 2026.

| ID | Milestone | Status | Expected commit |
|---|---|---|---|
| M0 | Repo + React/Vite + ASP.NET Core foundations | LOCALLY VERIFIED | `chore: initialize frontend and backend projects` |
| M1 | DB + Firebase JWT/user upsert + React login/signup/protected routes | NOT STARTED | `feat(auth): verify Firebase tokens and persist users` |
| M2 | Projects API/UI owner-scoped | NOT STARTED | `feat(projects): add owner-scoped project management` |
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

## M0 verification record

- Work (Linux): npm ci, npm run build, and dotnet build passed.
- Windows: npm ci — PASSED (19 packages installed).
- Windows: npm run build — PASSED (Vite 8.3.8).
- Windows: React browser test — PASSED (Team Task Board / React is running).
- Windows: dotnet build — PASSED (.NET 10).
- Windows: ASP.NET Core API startup — PASSED.
- Windows: GET /health — PASSED (HTTP 200, {"status":"ok"}).
- Both development servers stopped successfully.
- Git staged-file and secret review: PENDING.
- Git commit and GitHub push: PENDING.