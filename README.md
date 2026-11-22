# Team Task Board

Team Task Board is a full-stack internship project for managing personal project boards. Sign in, create a project, track tasks from To Do to Done, discuss tasks in real time, and see overdue work grouped by assignee.

## Stack and architecture

| Layer | Versions verified | Responsibility |
| --- | --- | --- |
| Web | Node.js 24.19.0, React 19.3.1, React Router 8.5.2, Vite 8.3.8 | Authentication screens, projects, responsive Kanban, comments and report |
| API | .NET SDK 10.0.401, ASP.NET Core, EF Core 10.0.12, Oracle MySQL EF provider 10.0.12 | JWT validation, controllers, services, DTOs and relational queries |
| Relational storage | MySQL 8.4.9 | Users, project owners, task records and overdue counts |
| Firebase | Firebase JavaScript SDK 13.4.0 | Email/password authentication and realtime Firestore comments |

React sends a Firebase ID token to ASP.NET Core. The API verifies the RS256 signature against Google's signing certificates, checks the issuer, audience, expiry and identity claims, then resolves the Firebase UID to a local user. Queries scope project and task access to that local owner. Missing and foreign resources return the same 404.

Controllers handle HTTP, services apply ownership and business rules, and DTOs expose the fields needed by the client. EF Core supplies a typed model and versioned migrations, which keeps the C# model and MySQL schema reviewable together. That fit outweighed Dapper's smaller mapping layer for this project. The report uses a parameterized SQL JOIN and GROUP BY through EF Core.

MySQL's constraints and joins fit task ownership and reporting. Firestore gives the details panel a realtime subscription without polling MySQL. It stores comments under `users/{firebaseUid}/tasks/{taskId}/comments/{commentId}`. The rules allow an authenticated owner of that namespace to read and create validated comments. The app starts a listener from an API-authorized task card and unsubscribes when leaving the panel, switching tasks or signing out.

The composite `tasks(project_id, status, priority)` index supports project-scoped filters. Overdue reporting uses the project owner index and the task index's project prefix. The due-date predicate still checks the selected tasks, and substring title search is not covered by this index.

## Repository layout

| Path | Purpose |
| --- | --- |
| [frontend/](frontend/) | React app, Firebase client and browser/rules tests |
| [backend/](backend/) | ASP.NET Core API, EF model and migrations |
| [database/](database/) | MySQL schema export and fictional seed |
| [firebase/](firebase/) | Firestore security rules, referenced by [firebase.json](firebase.json) |
| [api-tests/](api-tests/) | VS Code REST Client request examples |
| [tests/](tests/) | .NET integration runner |
| [docs/](docs/) | Assignment plan, decisions and verified milestone evidence |

## Local setup on Windows

Install Git, .NET 10 SDK, Node.js 24 LTS with npm, and MySQL 8.4. Use PowerShell 7 and two terminals opened at the repository root. For the optional browser and rules tests, install Firebase CLI and Java 21.

### 1. Configure Firebase

In your Firebase project, register a Web app and enable **Authentication → Sign-in method → Email/Password**. In a fresh checkout, copy the frontend template:

```powershell
Copy-Item frontend/.env.example frontend/.env.local
```

Fill in `VITE_FIREBASE_API_KEY`, `VITE_FIREBASE_AUTH_DOMAIN`, `VITE_FIREBASE_PROJECT_ID`, and `VITE_FIREBASE_APP_ID` from the Web app settings. Leave `VITE_API_BASE_URL=http://127.0.0.1:5080` for local development. Vite exposes its variables to the browser. Keep private credentials in server-side configuration and keep the local file out of Git.

For comments, create the default Cloud Firestore database if needed. Review the existing rules and [this repository's rules](firebase/firestore.rules) before publishing to a project you control. Deployment replaces the project's Firestore rules, so check for other clients and use the same project ID as Authentication.

```powershell
firebase login
firebase deploy --only firestore:rules --project YOUR_FIREBASE_PROJECT_ID
```

### 2. Create a development database and apply migrations

Use a MySQL administrator in Workbench or the MySQL CLI on your own local instance. For a fresh database:

```sql
CREATE DATABASE teamtaskboarddev CHARACTER SET utf8mb4;
CREATE USER 'teamboarddev'@'127.0.0.1' IDENTIFIED BY RANDOM PASSWORD;
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, REFERENCES
    ON teamtaskboarddev.* TO 'teamboarddev'@'127.0.0.1';
```

Keep the generated password private. The database-level DDL privileges are for running the initial migration. Use a separate account limited to the required data operations if sharing or deploying the API. Use your existing database setup if already configured.

In the API terminal, set process-local configuration. The masked input takes a MySQL connection string with the `Server`, `Port`, `Database`, `User ID`, and `Password` fields. Set the Firebase project to the same ID used by Vite. ASP.NET Core reads these environment variables directly. [backend/.env.example](backend/.env.example) lists the keys.

```powershell
$env:ConnectionStrings__TeamTaskBoard = Read-Host -MaskInput 'Local MySQL connection string'
$env:Firebase__ProjectId = Read-Host 'Firebase project ID'
dotnet tool install --global dotnet-ef --version 10.0.12
dotnet restore backend/TeamTaskBoard.slnx
dotnet ef database update --project backend/TeamTaskBoard.Api
dotnet run --project backend/TeamTaskBoard.Api --no-launch-profile -- --urls http://127.0.0.1:5080
```

Install the pinned EF CLI only if it is absent. Run the application and migration commands in the same terminal so they inherit configuration. Keep configuration values in the shell or .NET user-secrets. Check <http://127.0.0.1:5080/health> for HTTP 200. The health route checks API startup, while the signed-in **Check API connection** button verifies Firebase and MySQL together.

### 3. Start and use the frontend

In a second terminal:

```powershell
Set-Location frontend
npm.cmd ci
npm.cmd run dev
```

Open <http://127.0.0.1:5173>. Keep this exact origin because local API CORS is scoped to it. Create an account and a project, click the project's name, and create a task. Use the move control to change its status, or select **Edit**, **Delete**, or **Details**. Search by title and filter by priority. Columns load ten tasks per page independently. With Firestore configured, **Details** shows live comments. Open **Overdue report** to see unfinished tasks due before today in UTC. Blank or missing assignees display as *Unassigned*.

### SQL scripts

The recommended setup is `dotnet ef database update`. As an alternative, select a new, empty UTF-8 (`utf8mb4`) MySQL database and run [schema.sql](database/schema.sql) once in Workbench or with the MySQL CLI's `source` command. The export comes from EF migration `20261121143712_InitialSchema` and includes the migration history entry. Both paths create the same schema. Choose one for initial creation. The export is a forward setup script, so use EF to apply any later migrations.

Run [seed.sql](database/seed.sql) only on a disposable schema. It adds two fictional users with null emails, three projects, and nine tasks with dates relative to the current UTC day. It skips seeding if any application rows exist and leaves counts unchanged on a second run. Run it with one client and no concurrent writers. The fictional UID records do not correspond to sign-in accounts. Use the UI to create projects for a real test account. The integration runner uses signed fixtures to test the seed's owner groups.

## API request reference

All `/api/*` endpoints use a Firebase bearer ID token. [team-task-board.http](api-tests/team-task-board.http) contains the seven assignment endpoints and two helpers.

| Method | Route | Success |
| --- | --- | --- |
| GET | `/health` | 200, startup status |
| GET | `/api/auth/me` | 200, local user |
| GET | `/api/projects` | 200, owned projects |
| POST | `/api/projects` | 201, created project |
| GET | `/api/projects/{id}/tasks` | 200, items, page, pageSize and totalCount |
| POST | `/api/projects/{id}/tasks` | 201, created task |
| PUT | `/api/tasks/{id}` | 200, updated task |
| DELETE | `/api/tasks/{id}` | 204, empty response |
| GET | `/api/reports/overdue-by-user` | 200, assignee names and counts |

Task filters are `status`, `priority`, `search`, and `page`. PUT supplies the full task input including status and priority. The API returns 400 for invalid input, 401 for missing or invalid authentication, and 404 for missing or foreign resources.

### Use a temporary Firebase token locally

Use a disposable account in your own Firebase project. Run the following PowerShell from the repo root. It signs in over HTTPS using the Firebase Auth REST API and writes only the short-lived ID token to the ignored `api-tests/.env`. It refuses to replace an existing file. The VS Code REST Client extension reads `FIREBASE_ID_TOKEN` from that file.

```powershell
if (Test-Path api-tests/.env) { throw 'Remove your previous local token file first.' }
$webKey = Read-Host 'Firebase Web API key'
$sampleEmail = Read-Host 'Disposable test account email'
$samplePassword = Read-Host -MaskInput 'Disposable test account password'
try {
    $body = @{ email = $sampleEmail; password = $samplePassword; returnSecureToken = $true }
    $session = Invoke-RestMethod -Method Post -ContentType 'application/json' `
        -Uri "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=$webKey" `
        -Body ($body | ConvertTo-Json -Compress)
    Set-Content -LiteralPath api-tests/.env -Value "FIREBASE_ID_TOKEN=$($session.idToken)"
} finally {
    Remove-Variable samplePassword,body,session -ErrorAction SilentlyContinue
}
```

Open the file in VS Code with REST Client and use **Send Request**. Run the create project and task requests first to resolve the chained IDs. The delete example targets that disposable task. The project remains. Remove the ignored token file when done. Keep request history and tokens private.

References: [Firebase Auth REST API](https://firebase.google.com/docs/reference/rest/auth/) and [VS Code REST Client](https://github.com/Huachao/vscode-restclient).

## Repeatable verification

From the repository root:

```powershell
dotnet restore backend/TeamTaskBoard.slnx
dotnet build backend/TeamTaskBoard.slnx --no-restore
dotnet ef migrations has-pending-model-changes --project backend/TeamTaskBoard.Api --no-build
git diff --check
```

If the running Windows API locks the Debug DLL, use an isolated build output or stop just your local API before rebuilding.

The integration runner needs an isolated MySQL 8.4 server on loopback, with an account that can create and drop a fresh disposable schema. Pass a connection string with `Server=127.0.0.1`, credentials, and the fixture server port. Leave `Database` unset. It creates a uniquely named test schema and removes only that schema at teardown.

```powershell
$env:TEAMBOARD_TEST_MYSQL = Read-Host -MaskInput 'Isolated MySQL server connection string, no Database'
dotnet run --project tests/TeamTaskBoard.Integration --artifacts-path "$env:TEMP/teamboard-integration-build"
Remove-Item Env:TEAMBOARD_TEST_MYSQL
```

This suite executes both SQL files, verifies the migration history and constraints, applies signed JWT fixtures to production middleware, checks cross-owner access, pagination, validation and report totals, and exercises the nine requests from the HTTP file.

For browser and rules checks, start both emulators from the repository root. Use this demo project ID and keep the configured ports 9099 and 8080 free.

```powershell
firebase emulators:start --only auth,firestore --project demo-team-task-board
```

In another terminal from the repository root:

```powershell
Set-Location frontend
npm.cmd ci
npx.cmd playwright install --only-shell chromium
npm.cmd run build
npm.cmd run test:firestore
npm.cmd run test:browser
```

The rules suite uses the real emulators. The browser suite uses emulator sign-in and explicitly stubbed API responses to check protected access, validation, loading, and retry. It starts a local Vite server on port 5188. The .NET suite separately verifies real database behavior with signed, short-lived JWT fixtures. See [docs/TASKS.md](docs/TASKS.md) for actual evidence and each test's scope.

## Assignment coverage and known limits

| Area | Evidence |
| --- | --- |
| Firebase signin and protected routes | Implemented and tested. Live signup/login and local user sync were previously verified |
| Owner-scoped projects and task APIs, Kanban, filtering and validation | Implemented. Isolated MySQL and browser tests passed. Live project management evidence is in the tracker |
| Firestore live task comments | Implemented, with emulator rules and two same-account sessions verified |
| Overdue report | Implemented and verified on isolated MySQL with owner scoping and UTC-date boundaries |
| Submission artifacts | Schema, seed, templates, API examples, setup, reproducible tests and AI disclosure are in the repository |
| Delivery workflow | Work is on `feature/team-task-board`. Final security review, PR to `main`, and submission are pending |

Genuine Firebase Task CRUD and genuine overdue reporting against persistent MySQL remain unverified. Genuine live two-account ownership verification is untested. Live Firestore database setup, rule deployment and comments remain unverified.

Firestore rules cannot query MySQL. They enforce the Firebase UID namespace, while the application opens comments from an authorized task card. Task deletion may leave orphan comment documents. Different Firebase accounts have separate comment threads, and assignees are free text. Drag-and-drop and shared-team editing are outside this implementation.

## AI assistance disclosure

ChatGPT and Codex assisted with planning, implementation, test fixtures, debugging, code review, and documentation. The repository's milestone tracker records which checks were executed and distinguishes fixture and emulator results from live verification.
