# Codex PoC Design

**Date:** 2026-04-27
**Scope:** Prove the git-driven module pipeline end-to-end. Mockup-quality implementation; no auth, no webhooks, no runbooks.

---

## Goal

A developer pushes a full-stack module (C# backend + React frontend) to a git repo. A running Codex instance discovers it, builds it, loads it, and exposes it as a sidebar page with its own API — without restarting. Users can enable/disable modules from a store UI and compose their sidebar. Datasources are configured once in the app and shared across modules.

---

## 1. Architecture

**Single container for the PoC.**

```
┌─────────────────────────────────────────────────────────┐
│  Codex Host (ASP.NET Core .NET 8)                       │
│                                                          │
│  ┌───────────────┐   ┌────────────────────────────┐    │
│  │ Core API      │   │ Module Runtime              │    │
│  │ /api/core/*   │   │  - GitPoller (background)   │    │
│  │ - dashboards  │   │  - Builder (dotnet + vite)  │    │
│  │ - modules     │   │  - Loader (ALC per module)  │    │
│  │ - datasources │   │  - Mounted: /api/mod/{id}/  │    │
│  │ - SQLite      │   └────────────────────────────┘    │
│  └───────────────┘                                       │
│                                                          │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Static host                                      │   │
│  │  /assets/shell/          React shell             │   │
│  │  /assets/modules/{id}/   remoteEntry.js bundles  │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                       │ git pull (poll every 30s)
                       ▼
              ┌─────────────────┐
              │  modules repo   │
              │  /modules/foo/  │
              │  /modules/bar/  │
              └─────────────────┘
```

**Out of scope for PoC:** auth/multi-user, webhooks, runbooks, parent/child data explorer, module sandboxing, widget/grid layout.

---

## 2. Module Anatomy

Every module lives in `/modules/{module-id}/` in the git repo.

```
modules/
  my-k8s-explorer/
    manifest.json
    backend/
      MyK8sExplorer.csproj
      Module.cs              ← implements ICodexModule
      Controllers/
        K8sController.cs
    frontend/
      package.json
      vite.config.ts         ← Module Federation, exports remoteEntry
      src/
        Page.tsx             ← default export = the page component
```

**manifest.json:**
```json
{
  "id": "my-k8s-explorer",
  "displayName": "Kubernetes Explorer",
  "version": "1.0.0",
  "description": "Browse pods, deployments, and events",
  "icon": "kubernetes",
  "pageRoute": "/k8s",
  "requires": ["project-a-db", "k8s-api"]
}
```

### Backend SDK (`Codex.ModuleSDK` NuGet package)

```csharp
public class Module : ICodexModule
{
    // Datasource IDs this module requires (must exist + be configured)
    public IEnumerable<DataSourceRequirement> Requires => new[]
    {
        new DataSourceRequirement("project-a-db", DataSourceType.Postgres)
    };

    public void Register(IServiceCollection services) { ... }
    public void MapEndpoints(WebApplication app) { ... }
}
```

- Controllers are plain ASP.NET controllers, auto-prefixed to `/api/mod/{moduleId}/`
- Module receives configured DI clients (e.g. `NpgsqlDataSource`, `HttpClient`) for declared datasources
- Module never touches credentials directly

### Frontend SDK (`@codex/module-sdk` npm package)

```ts
// Page.tsx — only requirement is a default export
export default function Page() {
  const api = useCodexApi(); // pre-configured fetch → /api/mod/{moduleId}/
  return <div>...</div>;
}
```

- Vite Module Federation bundles to `remoteEntry.js`
- Shell imports lazily at runtime; module authors don't configure Federation directly
- SDK provides: `useCodexApi()`, navigation context, Codex design tokens

---

## 3. Git Poller & Build Pipeline

### GitPoller (`BackgroundService`)

- Polls every N seconds (configurable via `Codex:PollerIntervalSeconds`, default 30)
- Runs `git pull` on the modules repo path
- Hashes each module folder (manifest + source files)
- Compares against `ModuleRegistry` in memory
- Enqueues `BuildJob` for new/changed modules, `UnloadJob` for removed modules
- On first startup: all modules are treated as new

### Builder

Processes jobs sequentially (one at a time for PoC):

```
BuildJob(moduleId)
  1. dotnet build backend/ -o /data/modules/{id}/backend/
  2. npm ci && vite build frontend/ → /data/modules/{id}/frontend/
  3. success → enqueue LoadJob
  4. failure → status = BuildFailed, build log stored in DB
```

Build stdout/stderr is captured and surfaced in the Store UI.

### Loader

```
LoadJob(moduleId)
  1. Unload existing CollectibleAssemblyLoadContext if present
  2. Create new CollectibleAssemblyLoadContext
  3. Load DLL, find ICodexModule implementation
  4. Validate all required datasources are configured
  5. Call module.Register(services) + module.MapEndpoints(app)
  6. Copy frontend assets to wwwroot/assets/modules/{id}/
  7. Update ModuleRegistry: status = Loaded
```

If a required datasource is missing → status = `MissingDatasource`, module partially loads (backend not mounted).

### Module State Machine

```
Discovered → Building → BuildFailed
                     ↓
                   Loaded ←──────────────┐
                     ↓                   │
               MissingDatasource         │ (datasource configured)
                     ↓                   │
               (datasource added) ───────┘

Loaded → Unloading → (gone)
Loaded → Building   (module updated in git)
```

---

## 4. Dashboard & Store UI

### Shell Layout

```
┌──────────────┬──────────────────────────────────────────┐
│  Sidebar     │  Content area                             │
│              │                                           │
│  ★ K8s       │  <Active module page renders here>        │
│    PagerDuty │                                           │
│    Postgres  │                                           │
│  ──────────  │                                           │
│  + Store     │                                           │
│  + Sources   │                                           │
└──────────────┴──────────────────────────────────────────┘
```

- Sidebar shows only **enabled** modules in user-defined order
- ★ = favorite, floats to top of sidebar
- **Store** and **Sources** are built-in core pages (not modules)
- Drag-to-reorder in sidebar; changes persist immediately

### Module Store Page

Displays all discovered modules with status, version, description, and actions:

- `Enable` → adds to dashboard sidebar
- `Disable` → removes from sidebar (module stays loaded in host)
- `View build log` → shows captured build stdout/stderr
- Status indicators: `Building` / `Loaded` / `BuildFailed` / `LoadFailed` / `MissingDatasource`

### Datasources Management Page

- List all configured datasources (name, type, status)
- Create new datasource: name, type (Postgres / MongoDB / HTTP), credentials (host, port, user, password / URL)
- Edit / delete existing datasources
- Credentials stored encrypted in SQLite via ASP.NET Core Data Protection API
- Removing a datasource that modules depend on shows a warning listing affected modules

---

## 5. Data Model

**SQLite via EF Core (code-first migrations). Two core tables for PoC; path to Marten/Postgres later.**

```sql
-- Global dashboard (single row for PoC — no auth)
CREATE TABLE Dashboards (
  Id        TEXT PRIMARY KEY,  -- "default"
  PagesJson TEXT NOT NULL      -- [{moduleId, order, isFavorite}]
);

-- Module registry (runtime state, rebuilt on startup)
CREATE TABLE ModuleRegistrations (
  Id           TEXT PRIMARY KEY,
  DisplayName  TEXT NOT NULL,
  Version      TEXT NOT NULL,
  Description  TEXT,
  Icon         TEXT,
  PageRoute    TEXT NOT NULL,
  Status       TEXT NOT NULL,   -- Discovered|Building|BuildFailed|Loaded|LoadFailed|MissingDatasource
  BuildLog     TEXT,
  LoadedAt     TEXT,            -- ISO8601
  ManifestHash TEXT NOT NULL    -- poller uses this to detect changes
);

-- Datasources (first-class, independent of modules)
CREATE TABLE DataSources (
  Id              TEXT PRIMARY KEY,  -- user-defined, e.g. "project-a-db"
  Type            TEXT NOT NULL,     -- "postgres" | "mongodb" | "http"
  DisplayName     TEXT NOT NULL,
  CredentialsJson TEXT NOT NULL      -- AES-encrypted via Data Protection API
);
```

---

## 6. Sample Modules (ship with PoC)

Three modules live in `/modules/` in the repo to drive the demo:

| Module | Datasource | Purpose |
|---|---|---|
| `hello-world` | none | Validates full load pipeline. One endpoint + one page. |
| `system-info` | none | Shows host CPU/memory/uptime via .NET intrinsics. Demonstrates `useCodexApi()`. |
| `postgres-browser` | `postgres:demo` (user configures) | Lists tables and row counts. Proves datasource injection end-to-end. |

---

## 7. Repo Structure

```
codex/
  src/
    Codex.Host/              ASP.NET Core host
    Codex.ModuleSDK/         NuGet package shipped to module authors
    Codex.Shell/             React + Vite shell app
  modules/                   Demo modules (also serves as the "modules repo" for PoC)
    hello-world/
    system-info/
    postgres-browser/
  docs/
    superpowers/specs/
```

For the PoC the host points its GitPoller at the local `modules/` folder. A production deployment would point to a separate remote repo.

---

## 8. PoC Explicit Non-Goals

- Auth / multi-user (single global dashboard)
- Module sandboxing (modules run in-process with full host trust)
- Webhook ingestion
- Runbook / procedure storage
- Parent/child data relationship explorer
- Widget/grid dashboard builder
- Module marketplace / external git remote (local path only)
- Parallel builds
- Rollback on bad module load
