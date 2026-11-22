# Team Task Board — Approved Implementation Plan

## Scope and success

Full Stack Developer Intern assignment; target submission November 22, 2026. Priority: functioning, secure, explainable core app. Original four-page PDF is source of truth. Required: React, ASP.NET Core Web API C#, MySQL, Firebase Authentication, Cloud Firestore comments, GitHub, README and scripts.

## Functional acceptance checklist

- [ ] Firebase email/password signup, login, logout, protected routes.
- [ ] User creates and lists own projects only.
- [ ] User creates, edits, deletes and changes task status among To Do, In Progress, Done.
- [ ] Tasks filter by priority and search by title.
- [ ] Task API supports status, priority, search, page and selected bonus totalCount.
- [ ] Task form rejects blank title/invalid priority/new past due date; due date optional.
- [ ] Task details panel subscribes to live Firestore comments with secure rules.
- [ ] Overdue-by-user report counts non-Done tasks before current UTC date by assignee, restricted to project owner.
- [ ] Responsive frontend; loading/empty/error states on all API flows.
- [ ] Correct 200, 201, 204, 400, 401 and 404, consistent errors; no cross-owner resource leaks.
- [ ] Relational tables, keys and constraints, filter index, JOIN/GROUP BY report SQL.
- [ ] `schema.sql`, `seed.sql`, `.env.example`, seven-endpoint `.http` collection, README + AI disclosure.
- [ ] Feature branch with meaningful commits and pull request to `main`.

**A checklist item is checked only once actually tested**, not when merely generated.

## Component boundaries

React/Vite client (Firebase Auth and Firestore SDK) -> ASP.NET Core JWT-protected REST API -> EF Core -> MySQL. Browser also connects to Firestore under UID-scoped rules. API validates own resources and never accepts owner ID from clients. Firestore cannot prove MySQL task existence; note limitation.

## Backend API (all Bearer authenticated)

| Method | Route | Result |
|---|---|---|
| GET | `/api/projects` | Own project list, 200 |
| POST | `/api/projects` | Create, 201 with DTO |
| GET | `/api/projects/{id}/tasks` | status/priority/search/page, `items` and `totalCount`, 200 |
| POST | `/api/projects/{id}/tasks` | Create task, 201 with DTO |
| PUT | `/api/tasks/{id}` | Full update including status, 200 |
| DELETE | `/api/tasks/{id}` | Delete, 204 |
| GET | `/api/reports/overdue-by-user` | Owner-scoped report, 200 |

Bad input 400, missing/invalid/expired token 401, nonexistent/foreign 404. Standardized Problem Details-style error JSON for exception, validation and auth flows.

## MySQL data model

`users(id PK, firebase_uid UNIQUE NOT NULL, email NULL, created_at_utc NOT NULL)`;
`projects(id PK, owner_id NOT NULL FK users.id, name NOT NULL, description NULL, created_at_utc NOT NULL, updated_at_utc NOT NULL)`;
`tasks(id PK, project_id NOT NULL FK projects.id, title NOT NULL, description NULL, status NOT NULL, priority NOT NULL, due_date DATE NULL, assignee NULL, created_at_utc NOT NULL, updated_at_utc NOT NULL)`.

IDs BIGINT; project/task time columns DATETIME(6); statuses ToDo/InProgress/Done; priorities Low/Medium/High; index `(project_id,status,priority)`; projects owner index. No separate assignee user relationship; the overdue SQL joins projects and groups by assignee and filters `owner_id`.

## JWT / Firestore / frontend

Token checking: RS256, valid Google Firebase Secure Token signing keys with rotation/cache, issuer `https://securetoken.google.com/{projectId}`, audience projectId, nonempty subject, exp/iat; 401 on failure. Safe local-user upsert. React uses `getIdToken`, and at most one forced refresh after appropriate 401. Firestore path: `users/{uid}/tasks/{taskId}/comments/{commentId}`, rule UID equality plus content/author/timestamp validation, onSnapshot cleanup, no auto delete cleanup.

React pages: Login, Signup, Projects, Board, Overdue Report, NotFound. Board includes KanbanColumn, TaskCard, TaskForm, TaskDetailsPanel, TaskComments, toolbar. Use AuthContext/useAuth, useProjects, useTasks(AbortController), useTaskComments, useOverdueReport; separate API service layer. Independent pagination per column (proposed 10/page), mobile-readable UI.

## Milestone dependency graph

M0 foundation -> M1 Firebase auth + MySQL users & login/signup -> M2 Projects -> M3 Tasks API -> M4 Board -> M5 Comments -> M6 Report -> M7 tests/docs -> M8 security review, PR, submission.

Each milestone must produce scoped files, executed vs local tests, expected Git commit, updated tasks tracker, short report. Never advance automatically. If time constrains implementation, prioritize mandated core and document gaps.

## Deliverables / limitations / interview

Folders `/frontend`, `/backend`; `database/schema.sql` generated from EF Core migrations plus `seed.sql`, `api-tests/team-task-board.http`, `firebase/firestore.rules`, environment examples, `/docs` and root README/AGENTS. Use `.gitignore` and .NET user-secrets for credentials, never share keys. README must explain EF provider and index choice, setup under ten minutes with prerequisites, tested status, known gaps and AI use.

Known limits: no shared team editing, assignee free text, UID-scoped Firestore cannot prove MySQL task existence, orphan comments on task delete, no drag/drop, possible JWT revocation delay, `%term%` search not indexed. Interview topics: end-to-end task save, Firebase JWT, ownership, index EXPLAIN, Firestore vs MySQL, React stale requests, 100k tasks, live change to status/filter.
