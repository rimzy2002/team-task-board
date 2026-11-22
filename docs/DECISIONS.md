# Team Task Board — Architecture Decisions (approved planning baseline)

**Status:** Approved for milestone-based implementation on November 21, 2026. This does not authorize coding future milestones without an explicit task. **Deadline:** November 22, 2026.

| ID | Decision | Rationale / limitation |
|---|---|---|
| ADR-01 | React + Vite, JavaScript/JSX | Small, approachable SPA; Next.js not required |
| ADR-02 | ASP.NET Core Web API C# with controllers, services, DTOs | Explicit assignment requirement and explainable layering |
| ADR-03 | EF Core code-first + migrations; export `schema.sql` | Aligns database schema with C# model; don't run duplicate migration path |
| ADR-04 | MySQL 8.4 LTS target | Relational storage required by assignment |
| ADR-05 | .NET 10 / EF Core 10 + candidate Oracle `MySql.EntityFrameworkCore` 10.x | Supportable baseline; VERIFY package compatibility and generated migration SQL at M1; provider selection may be revisited only with evidence and user approval |
| ADR-06 | Firebase email/password Authentication; JWT bearer middleware, no Admin SDK | Validate actual Google signing keys, RS256, issuer, audience, lifetime and subject; normal JWT verification doesn't imply immediate revocation checks |
| ADR-07 | Upsert local users by UNIQUE Firebase UID | Stable identity bridge; handle race condition |
| ADR-08 | Project owner is only authorization scope; foreign resource -> 404 | Matches explicit rule; no shared team membership or roles |
| ADR-09 | Manager = project owner; assignee = trimmed free-text | Simple overdue grouping; spelling variants may split groups |
| ADR-10 | Firestore: `users/{uid}/tasks/{taskId}/comments/{commentId}` with UID-based rules | Owner-scoped live comments, subscribe only after API task authorization; Firestore cannot validate MySQL task existence |
| ADR-11 | No automatic Firestore comment cleanup after task deletion | Orphan comments documented as known gap |
| ADR-12 | Task status control (not drag/drop) | Mandatory status movement first; optional bonus omitted |
| ADR-13 | Independent per-column server-side task pagination and `totalCount` | Avoid incomplete columns; provisional fixed page size 10 |
| ADR-14 | Optional due date SQL DATE, UTC timestamp columns; overdue cutoff = UTC calendar day | Consistent report; reject past newly changed dates, allow existing overdue unchanged |
| ADR-15 | Index `tasks(project_id,status,priority)` | Efficient project/status filters; explain limitations for priority-only, leading-wildcard search |
| ADR-16 | 201 with created DTO; 204 on deletion; consistent Problem Details-style errors | No extra single-resource GET endpoints required |
| ADR-17 | xUnit service test and React component test; count pagination bonus | After mandatory core; skip Docker, deployment, SSR, Google login, drag/drop |
| ADR-18 | Work implements one milestone; user performs reviewed manual Git commit/push; PR feature -> main | Original work, verifiable small commits, no automatic account changes |

**Escalation rule:** If the original assignment PDF conflicts with this summary, stop and ask Planning Chat to resolve the conflict. Current repository and actual test results supersede optimistic status claims.
