# team-task-board
A full-stack team task management application built with React, ASP.NET Core, MySQL, and Firebase, featuring secure authentication, Kanban task tracking, live comments, and overdue reporting.

**Current status:** M0 foundation completed and locally verified.
Authentication, task management, MySQL integration, and Firebase
features are planned for upcoming milestones.
## M0 local setup

Install Git, Node.js 24 LTS with npm, and the .NET 10 SDK. Open the repository
root in VS Code and use separate PowerShell terminals for the frontend and API.

Frontend (start at the repository root):

```powershell
Set-Location .\frontend
npm ci
npm run build
npm run dev
```

Open <http://127.0.0.1:5173> and look for **Team Task Board**.

API (start at the repository root):

```powershell
Set-Location .\backend
dotnet build
dotnet run --project .\TeamTaskBoard.Api\TeamTaskBoard.Api.csproj --no-build --no-launch-profile -- --urls http://127.0.0.1:5080
```

Open <http://127.0.0.1:5080/health>. Expect HTTP 200 and `{"status":"ok"}`.
The endpoint checks API startup. The two processes run independently, and M0
uses no Firebase or MySQL configuration. Both run commands bind to loopback for
local testing. The example environment files contain public, local URLs.

The M0 scaffold and this setup section were created with ChatGPT assistance.