# CLAUDE.md — Al-Mal Channel (قناة المال)

> **This file is the single source of truth for Claude Code. Read it fully before every task.**

---

## 📖 REQUIRED READING — DO THIS FIRST

**Before starting ANY work on this project, you MUST read these files:**

1. **This file** (`CLAUDE.md`) — Rules, conventions, and workflow
2. **`docs/PRD.md`** — Full Product Requirements Document (1,200+ lines). This contains ALL specifications: database schemas, API endpoints, UI layouts, AI prompts, module details, and more.
3. **`TASKS.md`** — Current task tracker with progress status

**Claude Code: At the start of every session, run:**
```bash
cat docs/PRD.md
cat TASKS.md
```

**Before working on any specific module, read the relevant PRD section:**
```bash
# Example: Before working on market dashboard
grep -A 200 "## 6. Module 2: Market Dashboard" docs/PRD.md

# Example: Before working on database entities
grep -A 500 "## 4. Database Schema" docs/PRD.md

# Example: Before working on AI features
grep -A 150 "## 13. Module 9: AI Layer" docs/PRD.md
```

**The PRD is your specification. If something is defined in the PRD, follow it exactly. Do not guess or improvise when the PRD has the answer.**

---

## 🔒 OPERATING RULES (ALWAYS ENFORCED)

### Rule 1: Plan Before You Code

Before writing ANY code, you MUST:

1. **Read the relevant section from `docs/PRD.md`** — use `cat` or `grep` to find the exact specification
2. Create a plan in the chat explaining:
   - What you will build
   - Which files you will create or modify
   - Which dependencies you will add
   - What database changes are needed
   - Expected outcome after completion
3. Wait for approval OR if running in auto-approval mode, output the plan then proceed

### Rule 2: Task Tracking in TASKS.md

Every piece of work MUST be tracked in `TASKS.md` at the project root.

**Before starting a task:**
- Find the task in `TASKS.md` and change its status from `[ ]` to `[⏳]`
- If the task doesn't exist, add it under the correct phase

**After completing a task:**
- Change status to `[✅]` and add completion date
- If a task failed or is blocked, mark as `[❌]` with reason

**Status legend:**
```
[ ]  — Not started
[⏳] — In progress
[✅] — Completed (add date: YYYY-MM-DD)
[❌] — Failed/Blocked (add reason)
[⏸️] — Paused
```

### Rule 3: Build Verification After Every Task

After completing ANY task that involves code changes, you MUST:

```bash
# Step 1: Build the entire solution
dotnet build AlMal.sln

# Step 2: If build fails, FIX ALL ERRORS before proceeding
# DO NOT move to the next task with compilation errors

# Step 3: Run tests if they exist for the affected area
dotnet test AlMal.sln --no-build
```

**If the build fails:**
1. Read the error messages carefully
2. Fix all compilation errors
3. Build again
4. Repeat until clean build (0 errors, 0 warnings if possible)
5. Only then mark the task as complete

### Rule 4: Database Migration Discipline

After ANY change to entities, DbContext, or configurations:

```bash
# Step 1: Add migration with descriptive name
dotnet ef migrations add <DescriptiveName> --project src/AlMal.Infrastructure --startup-project src/AlMal.Web

# Step 2: Review the generated migration file
# Check that Up() and Down() methods are correct

# Step 3: Apply migration to update the database
dotnet ef database update --project src/AlMal.Infrastructure --startup-project src/AlMal.Web

# Step 4: Verify no errors
```

**Migration naming convention:** `<Action>_<Entity>_<Detail>`
- Examples: `AddStockEntity`, `AddSectorRelationship`, `AddUserFollowTable`, `UpdatePostAddRepostField`

**NEVER skip database update.** Every migration must be applied immediately after creation.

### Rule 5: Git Branching & Pull Request Workflow

**EVERY task or feature group MUST be developed on a separate branch. NEVER commit directly to `main`.**

#### Branch Naming Convention

```
{type}/{phase}-{short-description}
```

**Types:**
- `feature/` — New feature or module
- `fix/` — Bug fix
- `refactor/` — Code refactoring
- `docs/` — Documentation only
- `test/` — Adding tests

**Examples:**
```
feature/p1-solution-setup
feature/p1-domain-entities
feature/p1-database-setup
feature/p1-authentication
feature/p1-base-ui-layout
feature/p2-boursa-scraper
feature/p2-market-dashboard
feature/p2-stock-detail
feature/p2-signalr-realtime
feature/p3-news-integration
feature/p3-ai-movement-explainer
feature/p4-community-posts
feature/p5-simulation-portfolio
feature/p6-whatsapp-alerts
fix/p2-scraper-timeout
refactor/p3-ai-service-interface
```

#### Workflow Per Task Group

```bash
# Step 1: Ensure main is up to date
git checkout main
git pull origin main

# Step 2: Create feature branch
git checkout -b feature/p1-solution-setup

# Step 3: Do all work on this branch (code, build, test, migrate)
# ... make changes ...
# ... dotnet build ...
# ... dotnet ef migrations add (if needed) ...
# ... dotnet ef database update (if needed) ...

# Step 4: Commit with descriptive message (see commit convention below)
git add .
git commit -m "feat(setup): create .NET 9 solution with all 7 projects"

# Step 5: Push branch to remote
git push origin feature/p1-solution-setup

# Step 6: Create Pull Request
gh pr create --title "feat(p1): Solution setup with all projects" \
  --body "## Changes
- Created AlMal.sln with 7 src projects and 3 test projects
- Set up project references per Clean Architecture
- Added .gitignore

## Checklist
- [x] dotnet build passes (0 errors)
- [x] TASKS.md updated
- [ ] Ready for review" \
  --base main

# Step 7: Update TASKS.md with PR link
# Step 8: IMMEDIATELY move to next task group (do not wait for merge)
```

#### Grouping Tasks into Branches

Not every checkbox in TASKS.md needs its own branch. Group related tasks into logical feature branches:

| Branch | TASKS.md Sections |
|--------|-------------------|
| `feature/p1-solution-setup` | 1.1 Solution Setup + 1.2 NuGet Packages |
| `feature/p1-domain-entities` | 1.3 Domain Entities |
| `feature/p1-database-setup` | 1.4 Database Setup (all configurations + migration) |
| `feature/p1-authentication` | 1.5 Authentication |
| `feature/p1-base-ui-layout` | 1.6 Base UI Layout |
| `feature/p1-infrastructure` | 1.7 Infrastructure Setup + 1.8 Database Seeding |
| `feature/p2-scraper` | 2.1 Market Data Scraper |
| `feature/p2-market-dashboard` | 2.2 Market Dashboard + 2.4 SignalR |
| `feature/p2-stock-detail` | 2.3 Stock Detail Page |
| `feature/p2-watchlist-api` | 2.5 Watchlist + 2.6 Market API + 2.7 Admin Market |
| `feature/p3-news` | 3.1 News Integration + 3.2 News Feed UI |
| `feature/p3-ai-features` | 3.3 Movement Explainer + 3.4 Disclosure Summarizer + 3.5 News Context |
| `feature/p3-admin-news` | 3.6 Admin News Management |
| `feature/p4-profiles-follow` | 4.1 User Profiles & Follow |
| `feature/p4-posts` | 4.2 Post System + 4.3 Interactions + 4.4 Badges |
| `feature/p4-community-api` | 4.5 Community API + 4.6 Admin Moderation |
| `feature/p5-academy` | 5.1 Course System + 5.2 Quiz + 5.3 Certificates |
| `feature/p5-simulation` | 5.4 Simulation Portfolio + 5.5 API |
| `feature/p5-admin-academy` | 5.6 Admin Academy |
| `feature/p6-whatsapp` | 6.1 WhatsApp + 6.2 Assistant + 6.3 Alerts |
| `feature/p6-admin-polish` | 6.4 Admin Alerts + 6.5 Performance |
| `feature/p6-cicd-deploy` | 6.6 CI/CD GitHub Actions Deployment |
| `feature/p6-uat-launch` | 6.7 UAT & Production |

#### Pull Request Template

Every PR description MUST include:

```markdown
## Summary
Brief description of what this PR adds/changes.

## Changes
- List of major changes
- New files created
- Database migrations included

## Database Migrations
- [ ] Migration added: `<MigrationName>`
- [ ] Database updated successfully
- [ ] No database changes in this PR

## Testing
- [ ] `dotnet build AlMal.sln` passes (0 errors)
- [ ] `dotnet test` passes (if applicable)
- [ ] Manual testing done
- [ ] Mobile responsive checked

## TASKS.md
- Tasks completed: list task IDs
- Tasks remaining: list any deferred tasks

## Screenshots
(if UI changes, add mobile + desktop screenshots)
```

#### Important Rules

1. **NEVER push directly to `main`** — always through a PR
2. **One feature branch = one PR** — don't mix unrelated changes
3. **Keep PRs focused** — aim for reviewable size (< 50 files if possible)
4. **Always branch from latest main** — `git checkout main && git pull` before creating new branch
5. **Delete branch after merge** — keep the repo clean
6. **Do NOT wait for PR merge** — create PR then immediately start next task group
7. **If a new branch has conflicts with pending PRs** — note it in the PR description, the user will resolve during review

### Rule 6: Auto-Approval Mode Behavior

When running with `--auto-approve` or `-y` flag, Claude Code should:

1. **Still output the plan** (but don't wait for approval)
2. **Create feature branch** from latest main
3. **Execute all steps** in sequence without pausing
4. **Run build verification** automatically
5. **Run database migrations** automatically
6. **Commit changes** to the feature branch
7. **Push branch** to remote
8. **Create Pull Request** via `gh pr create`
9. **Update TASKS.md** automatically
10. **Report results** then **IMMEDIATELY continue to the next task group**

**Workflow per task group in auto-approval mode:**
```
1. cat docs/PRD.md (read relevant section for the task)
2. cat TASKS.md (check current progress)
3. git checkout main && git pull
4. git checkout -b feature/{branch-name}
5. Print plan (referencing PRD specifications)
6. Execute code changes
7. dotnet build → fix errors if any
8. dotnet ef migrations add (if DB changed)
9. dotnet ef database update (if DB changed)
10. dotnet test (if tests exist)
11. Update TASKS.md
12. git add . && git commit
13. git push origin feature/{branch-name}
14. gh pr create
15. Print summary with PR link
16. IMMEDIATELY start next task group (go back to step 1)
```

**CONTINUOUS MODE: Do NOT stop between task groups. After creating a PR, immediately move to the next feature branch. The user will review and merge PRs separately. Each new branch should be created from the CURRENT main (even if previous PRs are not yet merged). If there are merge conflicts later, the user will resolve them during PR review.**

**When starting a new branch while previous PRs are pending:**
```bash
# Always branch from latest main
git checkout main
git pull origin main
git checkout -b feature/{next-branch}
# Continue working — don't wait for previous PR merge
```

**Only stop if:**
- All tasks in the current phase are complete (end of phase)
- A critical error cannot be resolved after 3 attempts
- You need information from the user (API keys, credentials, etc.)

---

## 📁 PROJECT STRUCTURE

```
AlMal/
├── CLAUDE.md                          # THIS FILE — read first
├── TASKS.md                           # Task tracking — update always
├── docs/
│   └── PRD.md                         # Full product requirements
├── src/
│   ├── AlMal.Domain/                  # Entities, Enums, Interfaces (ZERO dependencies)
│   ├── AlMal.Application/             # DTOs, Services, Interfaces, Validators
│   ├── AlMal.Infrastructure/          # EF Core, External APIs, Caching, Identity
│   ├── AlMal.Web/                     # Public MVC website (startup project)
│   ├── AlMal.Admin/                   # Admin panel MVC
│   ├── AlMal.API/                     # REST API for Flutter
│   └── AlMal.BackgroundServices/      # Hangfire background jobs
├── tests/
│   ├── AlMal.UnitTests/
│   ├── AlMal.IntegrationTests/
│   └── AlMal.E2ETests/
└── AlMal.sln
```

### Dependency Rules (STRICT)

```
Domain        → depends on: NOTHING
Application   → depends on: Domain
Infrastructure→ depends on: Application, Domain
Web           → depends on: Application, Infrastructure
Admin         → depends on: Application, Infrastructure
API           → depends on: Application, Infrastructure
BackgroundSvc → depends on: Application, Infrastructure
Tests         → depends on: All (for testing)
```

**NEVER add a dependency from Domain to any other project.**
**NEVER add a dependency from Application to Infrastructure.**

---

## 🛠️ TECHNOLOGY & COMMANDS

### .NET 9 Solution

```bash
# Build
dotnet build AlMal.sln

# Run web app (dev)
cd src/AlMal.Web && dotnet run

# Run API (dev)
cd src/AlMal.API && dotnet run

# Run admin (dev)
cd src/AlMal.Admin && dotnet run
```

### SQL Server Connection

**Server:** `83.229.86.221,1433`
**Database:** `AlMal`
**Authentication:** SQL Server Authentication

**Connection string for appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=83.229.86.221,1433;Database=AlMal;User Id=sa;Password=P@ssw0rd@123@Codex@**;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**Connection string for Hangfire:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=83.229.86.221,1433;Database=AlMal;User Id=sa;Password=P@ssw0rd@123@Codex@**;TrustServerCertificate=True;MultipleActiveResultSets=true",
    "HangfireConnection": "Server=83.229.86.221,1433;Database=AlMal_Hangfire;User Id=sa;Password=P@ssw0rd@123@Codex@**;TrustServerCertificate=True"
  }
}
```

> **NOTE:** Create a separate `AlMal_Hangfire` database on the server for Hangfire job storage to keep it isolated from the main application database.

> **SECURITY:** These connection strings contain credentials. NEVER commit `appsettings.Development.json` or `appsettings.Production.json` to git. Add them to `.gitignore`.

### Entity Framework Core

```bash
# Add migration
dotnet ef migrations add <Name> --project src/AlMal.Infrastructure --startup-project src/AlMal.Web

# Update database
dotnet ef database update --project src/AlMal.Infrastructure --startup-project src/AlMal.Web

# Remove last migration (if not applied)
dotnet ef migrations remove --project src/AlMal.Infrastructure --startup-project src/AlMal.Web

# Generate SQL script (for review)
dotnet ef migrations script --project src/AlMal.Infrastructure --startup-project src/AlMal.Web
```

### Testing

```bash
# Run all tests
dotnet test AlMal.sln

# Run specific test project
dotnet test tests/AlMal.UnitTests

# Run with coverage
dotnet test AlMal.sln --collect:"XPlat Code Coverage"
```

### Package Management

```bash
# Add NuGet package to a project
dotnet add src/AlMal.Infrastructure/AlMal.Infrastructure.csproj package <PackageName>

# Restore all packages
dotnet restore AlMal.sln
```

---

## 🌐 LOCALIZATION RULES

- **ALL user-facing text MUST be in Arabic (العربية)**
- **ALL code (variables, classes, methods, comments) MUST be in English**
- **Direction:** RTL first. Use Bootstrap RTL build. Use `dir="rtl"` on `<html>`.
- **Currency:** KWD with 3 decimal places. Format: `0.345 د.ك`
- **Dates:** Gregorian calendar, formatted for Arabic locale
- **Numbers:** Use standard digits (not Arabic-Indic) unless user preference says otherwise
- **SQL Collation:** `Arabic_CI_AS`
- **String columns:** Always `NVARCHAR` (never `VARCHAR`)

---

## 🎨 UI/UX STANDARDS

### CSS Custom Properties (must be in every layout)

```css
:root {
  --clr-primary: #4F0E5E;
  --clr-accent: #FC4A04;
  --clr-positive: #22C55E;
  --clr-negative: #EF4444;
  --clr-dark: #1A1A2E;
  --clr-light: #F5F5F5;
  --font-primary: 'IBM Plex Sans Arabic', 'Noto Sans Arabic', sans-serif;
  --font-mono: 'IBM Plex Mono', monospace;
}
```

### Font Loading

```html
<link href="https://fonts.googleapis.com/css2?family=IBM+Plex+Sans+Arabic:wght@300;400;500;600;700&family=IBM+Plex+Mono:wght@400;500&display=swap" rel="stylesheet">
```

### Responsive Breakpoints

- Mobile: < 768px (single column, bottom nav visible)
- Tablet: 768px - 1024px (two columns)
- Desktop: > 1024px (full layout, sidebar visible)

### Component Naming Convention

- Views: `Views/{Controller}/{Action}.cshtml`
- Partial views: `Views/Shared/Components/{ComponentName}/_Default.cshtml`
- ViewModels: `ViewModels/{Controller}/{Action}ViewModel.cs`
- CSS: `wwwroot/css/{module}.css` (e.g., `market.css`, `community.css`)
- JS: `wwwroot/js/{module}.js` (e.g., `market.js`, `chart.js`)

---

## 🔐 SECURITY CHECKLIST (Apply to Every Feature)

- [ ] All user inputs sanitized (XSS prevention)
- [ ] All database queries via EF Core (SQL injection prevention)
- [ ] Anti-forgery tokens on all MVC forms
- [ ] `[Authorize]` attribute on protected endpoints
- [ ] Role checks on admin endpoints: `[Authorize(Roles = "Admin,SuperAdmin")]`
- [ ] Rate limiting on API endpoints
- [ ] No sensitive data in URLs or logs
- [ ] File uploads validated (type, size, content)
- [ ] CORS configured correctly
- [ ] HTTPS enforced

---

## 📊 DATABASE CONVENTIONS

### Entity Base Class

All entities should inherit from or include:

```csharp
public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

### Naming Conventions

- Tables: PascalCase plural (e.g., `Stocks`, `StockPrices`)
- Columns: PascalCase (e.g., `NameAr`, `LastPrice`)
- Foreign keys: `{Entity}Id` (e.g., `StockId`, `UserId`)
- Indexes: `IX_{Table}_{Column}` (e.g., `IX_Stock_Symbol`)
- Constraints: `CK_{Table}_{Rule}` (e.g., `CK_UserFollow_NoSelfFollow`)

### EF Core Configuration

- Use Fluent API in separate configuration files: `Infrastructure/Data/Configurations/{Entity}Configuration.cs`
- NEVER use data annotations on Domain entities
- Always specify string max length
- Always specify decimal precision: `.HasPrecision(18, 3)` for KWD, `.HasPrecision(8, 4)` for percentages

---

## 🤖 AI INTEGRATION RULES

### Claude API Safety

**NEVER** allow AI to:
- Recommend buying or selling stocks
- Predict prices
- Give financial advice
- Claim certainty about market direction

**ALWAYS** include this disclaimer in every AI response displayed to users:
```
هذا تحليل تعليمي وليس نصيحة استثمارية
```

### AI Service Pattern

```csharp
// Application layer - interface
public interface IAiAnalysisService
{
    Task<MovementExplanation> ExplainMovementAsync(string symbol);
    Task<string> SummarizeDisclosureAsync(string disclosureContent);
    Task<NewsContext> GenerateNewsContextAsync(long newsArticleId);
    Task<string> AnswerMarketQuestionAsync(string question, MarketContext context);
}

// Infrastructure layer - implementation uses Anthropic SDK
// Model: claude-sonnet-4-20250514
// Max tokens: 1024 for summaries, 2048 for explanations
// Temperature: 0.3 (factual, consistent)
```

---

## 📋 CODING STANDARDS

### C# Conventions

- Use `var` when type is obvious
- Use nullable reference types (`string?`, `int?`)
- Use `async/await` for all I/O operations
- Use `CancellationToken` on all async methods
- Use `ILogger<T>` for logging (Serilog)
- Use FluentValidation for input validation
- Use AutoMapper for DTO mapping
- Never throw exceptions for flow control — use Result pattern

### File Organization per Feature

When implementing a feature, create/modify files in this order:

1. **Domain:** Entity classes, Enums, Interfaces
2. **Application:** DTOs, Service interfaces, Validators, AutoMapper profiles
3. **Infrastructure:** Repository implementations, EF configurations, External API clients
4. **Web/API:** Controllers, Views, ViewModels, JS/CSS
5. **Tests:** Unit tests for services, Integration tests for API

### Git Commit Convention

Use conventional commits. Multiple commits per branch are fine — keep them logical.

```
feat(module): short description
fix(module): short description
refactor(module): short description
docs: short description
test(module): short description
chore: short description
```

Examples:
```
feat(domain): add all market data entities
feat(infrastructure): configure EF Core with all entity configurations
feat(auth): implement JWT authentication with refresh tokens
feat(market): add stock detail page with 6 tabs
fix(auth): resolve JWT refresh token expiration issue
refactor(infrastructure): extract scraper into IMarketDataProvider
docs: update TASKS.md with Phase 2 progress
test(portfolio): add simulation trade validation tests
chore: add NuGet packages for Phase 1
```

**Branch + Commit + PR example flow:**
```bash
git checkout -b feature/p1-domain-entities
# ... create entity files ...
git add .
git commit -m "feat(domain): add all market data entities (Stock, Sector, StockPrice, etc.)"
# ... create enum files ...
git add .
git commit -m "feat(domain): add all enums (UserType, AlertType, Sentiment, etc.)"
git push origin feature/p1-domain-entities
gh pr create --title "feat(p1): Domain entities and enums" --body "..."
```

---

## ⚡ PERFORMANCE GUIDELINES

### Caching Strategy

```
Redis Key Patterns:
  market:indices              → TTL: 15s (during market hours)
  market:stock:{symbol}       → TTL: 15s (during market hours)
  market:gainers              → TTL: 30s
  market:losers               → TTL: 30s
  market:heatmap              → TTL: 30s
  ai:movement:{symbol}        → TTL: 30min
  ai:disclosure:{id}          → TTL: 24h
  news:feed:page:{n}          → TTL: 5min
  user:profile:{id}           → TTL: 10min
```

### Database Query Rules

- Always use `.AsNoTracking()` for read-only queries
- Always use pagination (never load all records)
- Use `Include()` sparingly — prefer projection with `Select()`
- Add indexes for any column used in `WHERE` or `ORDER BY`
- Use `IQueryable` in repositories, materialize in services

### SignalR Guidelines

- Use Redis backplane for scaling: `services.AddSignalR().AddStackExchangeRedis()`
- Group clients by sector and stock symbol
- Only push changed data (diff updates), not full snapshots
- Max message size: 32KB
- Reconnection: automatic with exponential backoff on client

---

## 🚨 ERROR HANDLING PATTERN

### API Error Response Format

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "رسالة الخطأ بالعربية",
    "details": [
      { "field": "email", "message": "البريد الإلكتروني مطلوب" }
    ]
  }
}
```

### API Success Response Format

```json
{
  "success": true,
  "data": { ... },
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8
  }
}
```

### Exception Handling

- Use global exception middleware in Web/API
- Log all exceptions with Serilog (structured logging)
- Never expose stack traces to users
- Return Arabic error messages for all user-facing errors

---

## 📌 QUICK REFERENCE — Common Tasks

### GitHub Repository

**Repo:** `https://github.com/codexkw/AlMal`

```bash
# Clone
git clone https://github.com/codexkw/AlMal.git
cd AlMal
```

### Infrastructure

| Component | Details |
|-----------|---------|
| **GitHub Repo** | `https://github.com/codexkw/AlMal` |
| **SQL Server** | `83.229.86.221,1433` (sa auth) |
| **Main Database** | `AlMal` |
| **Hangfire Database** | `AlMal_Hangfire` (create separately) |
| **Web (Public)** | `https://almal.codexkw.co` → IIS: `AlMal-Web` |
| **Admin Panel** | `https://almal-admin.codexkw.co` → IIS: `AlMal-Admin` |
| **REST API** | `https://almal-api.codexkw.co` → IIS: `AlMal-API` |

### "I need to start a new feature"

1. **Read PRD first:** `grep -A 200 "## {relevant section}" docs/PRD.md`
2. `git checkout main && git pull origin main`
3. `git checkout -b feature/{phase}-{description}`
4. Output plan (referencing PRD specs)
5. Execute (code → build → migrate → test → commit)
6. `git push origin feature/{branch-name}`
7. `gh pr create --title "..." --body "..."`
8. Update `TASKS.md`
9. **Immediately start next task group**

### "I need to add a new entity"

1. Create entity class in `AlMal.Domain/Entities/`
2. Add `DbSet<Entity>` to `AlMalDbContext`
3. Create `EntityConfiguration.cs` in `Infrastructure/Data/Configurations/`
4. Run: `dotnet ef migrations add Add{Entity} --project src/AlMal.Infrastructure --startup-project src/AlMal.Web`
5. Run: `dotnet ef database update --project src/AlMal.Infrastructure --startup-project src/AlMal.Web`
6. Build: `dotnet build AlMal.sln`
7. Commit: `git add . && git commit -m "feat(domain): add {Entity} entity with EF config"`
8. Update `TASKS.md`

### "I need to add a new API endpoint"

1. Create DTO in `AlMal.Application/DTOs/`
2. Create/update service interface in `AlMal.Application/Interfaces/`
3. Implement service in `AlMal.Application/Services/` or `AlMal.Infrastructure/`
4. Create controller action in `AlMal.API/Controllers/`
5. Add FluentValidation validator if needed
6. Build: `dotnet build AlMal.sln`
7. Test endpoint with Swagger
8. Commit: `git add . && git commit -m "feat(api): add {endpoint} endpoint"`
9. Update `TASKS.md`

### "I need to add a new page (MVC)"

1. Create ViewModel in `AlMal.Web/ViewModels/`
2. Create/update controller in `AlMal.Web/Controllers/`
3. Create View in `AlMal.Web/Views/{Controller}/{Action}.cshtml`
4. Add CSS in `wwwroot/css/` if needed
5. Add JS in `wwwroot/js/` if needed
6. Build: `dotnet build AlMal.sln`
7. Test in browser
8. Commit: `git add . && git commit -m "feat(web): add {page} page"`
9. Update `TASKS.md`

### "My PR was merged, starting next feature"

1. `git checkout main`
2. `git pull origin main`
3. `git branch -d feature/{old-branch}` (delete old local branch)
4. `git checkout -b feature/{next-branch}`
5. Continue with next task group

---

*Last updated: March 2026 | Refer to `docs/PRD.md` for full specifications*
