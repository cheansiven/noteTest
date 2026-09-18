# Notes

A small full-stack notes application: create, read, update and delete personal notes, with
search, filtering, sorting and paging. Every user only ever sees their own notes.

| Layer     | Stack                                                          |
| --------- | -------------------------------------------------------------- |
| Front-end | Vue 3 (`<script setup>`) + TypeScript + Tailwind CSS v4 + Pinia + Vue Router + Axios |
| Back-end  | ASP.NET Core 10 Web API + Dapper + JWT bearer authentication     |
| Database  | SQL Server 2022 (Docker)                                         |

---

## Quick start

### Option A — everything in Docker (recommended)

```bash
docker compose up -d --build
```

That builds and starts all three services. Open <http://localhost:5273>, create an account,
and start writing notes. First run takes a few minutes (SQL Server is emulated on Apple
Silicon, and both app images are built from source); afterwards it is seconds.

```bash
docker compose ps                # service status and health
docker compose logs -f api       # follow the API
docker compose down              # stop; add -v to also delete the database volume
```

### Option B — Docker with your code mounted (live reload)

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

Same URLs, but the source is bind-mounted instead of baked into the images:

- **Backend** runs under `dotnet watch`; saving a `.cs` file recompiles and restarts the API.
- **Frontend** runs the Vite dev server with hot module replacement; saving a `.vue` or
  `.ts` file updates the page without a reload.

Nothing needs rebuilding for a code change — only a dependency change (a new NuGet or npm
package) needs `--build` again.

Four details that make this work cleanly:

- **.NET build output goes to `/artifacts`**, a named volume outside the mount, via
  `UseArtifactsOutput`. The container's Linux binaries therefore never collide with the
  `bin/obj` folders your host tooling writes, and you can keep running `dotnet build` or
  `dotnet test` on the host at the same time.
- **`node_modules` stays in a volume**, so the container's Linux-native esbuild and rollup
  binaries are not shadowed by the macOS ones in your working copy.
- **The Vite dev server proxies `/api`** to the API container, exactly as nginx does in the
  production image, so the browser talks to a single origin in both stacks.
- **The dev images are tagged separately** (`note-test-api-dev`, `note-test-web-dev`).
  Compose names images after the project and service by default, so without this a
  `docker compose build` in one mode would overwrite the other mode's image and a later
  `up` without `--build` would quietly start the wrong one.

File watching uses polling in both containers, because bind mounts on macOS and Windows do
not deliver inotify events into a container.

### Option C — local toolchain (no containers for the apps)

Three terminals, in this order:

```bash
# 1. Database only (host port 1434)
docker compose up -d sqlserver

# 2. API  -> http://localhost:5215
cd backend/src/Notes.Api
dotnet run

# 3. Front-end -> http://localhost:5273
cd frontend
npm install
npm run dev
```

> All three options publish the same host ports, so run one at a time.

Either way, the API creates the `NotesDb` database and applies `database/schema.sql` on
start-up, so there is no manual migration step.

### Ports

| Service      | URL / port                    |
| ------------ | ----------------------------- |
| Front-end    | http://localhost:5273         |
| API          | http://localhost:5215         |
| Swagger UI   | http://localhost:5215/swagger |
| SQL Server   | `localhost,1434`              |

These are deliberately non-default so the stack can run alongside anything else already
bound to 5173, 8080 or 1433.

### How the containers fit together

| Service     | Image                            | Role                                              |
| ----------- | -------------------------------- | ------------------------------------------------- |
| `sqlserver` | `mssql/server:2022-latest`       | Database; data persists in the `mssql-data` volume |
| `api`       | built from `backend/NotesApi/Dockerfile` | .NET SDK build → ASP.NET runtime image, non-root |
| `web`       | built from `frontend/Dockerfile` | Vite build → nginx serving the static bundle       |

- `api` waits for `sqlserver` to pass its health check, and additionally retries the schema
  step for up to a minute — SQL Server accepts TCP connections slightly before it accepts
  logins, so a single "is it up?" gate is not enough on a cold start.
- `web` serves the SPA and **proxies `/api` to the `api` service**, so the browser talks to
  a single origin and never issues a CORS pre-flight. `VITE_API_BASE_URL` is baked in at
  image build time as `/api`.
- Vue Router uses history mode, so nginx falls back to `index.html` for unknown paths.
- Overridable without editing any file:
  `MSSQL_SA_PASSWORD` and `JWT_SECRET` (e.g. `JWT_SECRET=... docker compose up -d`).

---

## Features

### Notes

- **Create** — title (required, ≤ 200 chars) and optional content. `CreatedAt` / `UpdatedAt`
  are set by the server, never by the client.
- **Read** — a responsive card grid showing title, a content preview and the creation date;
  clicking a note opens the full content plus its created / last-updated timestamps.
- **Update** — edit title and content; `UpdatedAt` is refreshed on every save.
- **Delete** — with a confirmation dialog; the note disappears from the list immediately.

### Search, filter, sort

- **Search** across title *and* content, debounced by 300 ms. `%`, `_` and `[` are escaped,
  so searching for `50%` finds the literal text rather than matching everything.
- **Filter** by creation date (Any time / Today / Last 7 days / Last 30 days, or a custom
  `from`–`to` range) and by whether a note has content.
- **Sort** by last updated, created date, or title, each ascending or descending.
- **Paging** — 12 notes per page, with a stable tie-breaker so rows never repeat or vanish
  between pages.

### Accounts

- Register / sign in with email + password. Passwords are hashed with BCrypt.
- JWTs are stored in `localStorage`, so a reload keeps you signed in; an expired token is
  discarded before it is ever sent.
- Every `/api/notes` request derives the owner from the token — the client never sends a
  user id, and a note belonging to someone else returns `404`, not `403`, so ids cannot be
  probed.

---

## API

All `/api/notes` endpoints require `Authorization: Bearer <token>`.

| Method   | Route                | Purpose                                     |
| -------- | -------------------- | ------------------------------------------- |
| `POST`   | `/api/auth/register` | Create an account, returns a JWT            |
| `POST`   | `/api/auth/login`    | Exchange credentials for a JWT              |
| `GET`    | `/api/auth/me`       | Profile behind the current token            |
| `GET`    | `/api/notes`         | List own notes (search/filter/sort/page)    |
| `GET`    | `/api/notes/{id}`    | One note in full                            |
| `POST`   | `/api/notes`         | Create a note                               |
| `PUT`    | `/api/notes/{id}`    | Update title + content                      |
| `DELETE` | `/api/notes/{id}`    | Delete a note                               |
| `GET`    | `/health`            | Liveness probe                              |

### `GET /api/notes` query parameters

| Parameter       | Values                                  | Default      |
| --------------- | --------------------------------------- | ------------ |
| `search`        | free text, matched against title + content | –          |
| `sortBy`        | `UpdatedAt` \| `CreatedAt` \| `Title`   | `UpdatedAt`  |
| `sortDirection` | `Asc` \| `Desc`                         | `Desc`       |
| `createdFrom`   | date (inclusive)                        | –            |
| `createdTo`     | date (inclusive, stretched to end of day) | –          |
| `hasContent`    | `true` \| `false`                       | –            |
| `page`          | 1-based                                 | `1`          |
| `pageSize`      | 1–100                                   | `20`         |

Example:

```bash
TOKEN=$(curl -s -X POST http://localhost:5215/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"you@example.com","password":"Passw0rd!23"}' | jq -r .token)

curl "http://localhost:5215/api/notes?search=groceries&sortBy=Title&sortDirection=Asc" \
  -H "Authorization: Bearer $TOKEN"
```

Errors come back as RFC 7807 `ProblemDetails`.

---

## Project layout

```
.
├── docker-compose.yml          Full stack: sqlserver + api + web
├── docker-compose.dev.yml      Overlay: mounts the source, live reload for both apps
├── database/
│   └── schema.sql              Single source of truth for the schema (idempotent)
├── backend/
│   ├── Notes.slnx
│   ├── Directory.Build.props   Shared build settings; warnings are errors
│   ├── src/
│   │   ├── Notes.Domain/           Entities, value objects, Result - no dependencies
│   │   ├── Notes.Application/      Use cases + the abstractions they need
│   │   ├── Notes.Infrastructure/   Dapper, BCrypt, JWT, schema runner
│   │   └── Notes.Api/              Controllers, composition root, Dockerfile
│   └── tests/
│       ├── Notes.Domain.UnitTests/         45 tests - invariants, no I/O
│       ├── Notes.Application.UnitTests/    23 tests - use cases with test doubles
│       ├── Notes.Infrastructure.UnitTests/ 14 tests - hashing, JWT, clock
│       └── Notes.Api.IntegrationTests/     27 tests - real API, real SQL Server
└── frontend/
    ├── Dockerfile              Vite build -> nginx
    ├── nginx.conf              SPA fallback + /api proxy to the api service
    ├── eslint.config.ts        Type-aware linting; the suite runs clean
    └── src/                    81 unit tests live beside the code as *.spec.ts
        ├── views/              LoginView, RegisterView, NotesView
        ├── components/         Cards, dialogs, toolbar, toasts, pagination
        ├── composables/        Focus trap, scroll lock, async actions, URL sync
        ├── stores/             Pinia: auth, notes, toast
        ├── services/           Typed API wrappers
        ├── lib/                Axios instance + session storage
        ├── router/             Routes and auth guards
        └── utils/              Date formatting
```

### Architecture

Dependencies point inwards — `Api → Infrastructure → Application → Domain` — and because
each layer is its own project, that direction is enforced by the compiler rather than by
convention. `Notes.Domain` references nothing at all, so a business rule cannot quietly
acquire a dependency on SQL Server or HTTP.

```
Notes.Api  ──▶ Notes.Infrastructure ──▶ Notes.Application ──▶ Notes.Domain
(HTTP, DI)     (Dapper, BCrypt, JWT)    (use cases, ports)     (rules, no deps)
```

**The domain owns its invariants.** Input becomes a value object before anything else
happens — `Email`, `NoteTitle`, `NoteContent`, `Password` — each returning a `Result` rather
than throwing. Once a `NoteTitle` exists it is non-empty and within length, so no other
layer re-checks it. Entities keep their state private-set and expose intent instead:

```csharp
note.Edit(title, content, utcNow);   // decides if anything changed, moves UpdatedAt itself
note.IsOwnedBy(userId);
user.ChangePassword(hash);
```

`Note.Edit` returns `false` when nothing actually changed, so re-saving an untouched note is
a no-op rather than a pointless `UpdatedAt` bump.

**Expected failures are values, not exceptions.** Services return `Result<T>` carrying an
`Error` with a `Code`, a description and a type (`Validation`, `NotFound`, `Conflict`,
`Unauthorized`). `ApiControllerBase` maps the type to a status code in one place, so the
translation is written once instead of per action. Exceptions are reserved for genuine
bugs, where a single `IExceptionHandler` turns them into a 500 without leaking details.

**Reads and writes take different paths.** A write loads the aggregate, calls a method on
it, and saves. A list query returns a flat projection with a trimmed content preview —
listing needs no behaviour, and shipping full note bodies for a grid would be wasteful.

**Ports keep infrastructure swappable and the tests fast.** `IPasswordHasher`,
`ITokenProvider`, `IDateTimeProvider` and the repositories are declared in
`Notes.Application` and implemented in `Notes.Infrastructure`. The injected clock is why
timestamp rules can be asserted exactly instead of with a sleep.

> For an app this size the four projects are more structure than strictly necessary. They
> are here to make the boundaries explicit and checkable; the same layering could live in
> one project with folders until it needed to grow.

### Other decisions worth calling out

- **Ownership is a `WHERE` clause, not an `if`.** Reads, updates and deletes all carry
  `AND UserId = @OwnerId`, so another user's note matches zero rows — there is no window
  between checking and writing. A note you do not own answers `404`, not `403`, so ids
  cannot be probed.
- **`ORDER BY` cannot be parameterised**, so the client's sort choice is mapped through a
  whitelist dictionary rather than concatenated into SQL.
- **`LIKE` wildcards are escaped**, so searching for `50%` finds that text instead of
  matching every row.
- **Dapper maps `DateTime` to `DateTime2`.** Its default is the legacy `datetime` type,
  whose ~3.33 ms resolution silently shifts stored timestamps; the clock is also truncated
  to whole milliseconds so a write's response matches what a later read returns exactly.
- **Timestamps are UTC end to end**, tagged by a JSON converter so browsers cannot read
  them as local time, then rendered in the viewer's own zone.
- **Sign-in and sign-up are rate limited** per client address (30/minute by default,
  configurable) to blunt credential stuffing.
- **Start-up fails loudly.** JWT options are validated with `ValidateOnStart`, so a missing
  or too-short signing key stops the app immediately instead of failing on first request.

---

## Front-end notes

**State lives in the URL.** Search, filters, sort and page are mirrored into the address
bar, so a filtered view survives a reload and can be shared or bookmarked. Values coming
back out are validated rather than trusted — a hand-edited `sortBy=DROP TABLE` falls back
to the default instead of being forwarded to the API. Updates use `replace`, not `push`,
because search is debounced per keystroke and pushing each one would bury the previous
page under near-identical history entries.

**Dialogs are actually accessible.** `useFocusTrap` keeps Tab and Shift+Tab cycling inside
an open dialog and hands focus back to whatever opened it. Setting initial focus alone is
not enough: without a trap, Tab walks straight out into the page behind, which for a
keyboard-only user means operating a UI they cannot see.

**The scroll lock is reference counted.** Dialogs stack — deleting from the note detail
view leaves two open — so a plain boolean would restore scrolling as soon as the inner one
closed, while the outer one still covered the page.

**Repetitive error handling is a composable.** `useAsyncAction` wraps a user-triggered
operation with its pending flag, its success toast and a failure toast built from the API's
ProblemDetails, and returns whether it succeeded. Without it the same five-line try/catch is
copied per button, and one missed `catch` makes a failed save look like a successful one.

**Requests that have been superseded are dropped.** The notes store aborts an in-flight
list request when a newer one starts, so fast typing cannot leave stale results on screen.

> Deliberately not included: a runtime schema validator (zod) over API responses, and a
> form library. Both are reasonable at a larger size; here they would add ceremony to two
> small forms and one well-typed client.

---

## Tests

```bash
cd backend
dotnet test                                # everything (110 tests)
dotnet test tests/Notes.Domain.UnitTests   # fast, no infrastructure needed

cd ../frontend
npm test                                   # 81 unit tests, no infrastructure needed
npm run lint                               # type-aware ESLint, clean
npm run typecheck                          # vue-tsc
```

The front-end suite and the three back-end unit suites need nothing running. The integration suite boots the real API
in-process with `WebApplicationFactory` and talks to a real SQL Server, creating a uniquely
named database per run and dropping it afterwards — so start one first:

```bash
docker compose up -d sqlserver
# or point the tests elsewhere:
NOTES_TEST_SQLSERVER='Server=...;User Id=...;Password=...;TrustServerCertificate=True;' dotnet test
```

They cover what unit tests cannot reach: the SQL itself (wildcard escaping, sorting,
paging, previews), model binding, the auth middleware, status codes, and — most
importantly — that one account can never read, edit or delete another account's note.

---

## Configuration

`backend/NotesApi/appsettings.json`:

```jsonc
{
  "ConnectionStrings": { "NotesDb": "Server=localhost,1434;Database=NotesDb;..." },
  "Jwt": { "Issuer": "NotesApi", "Audience": "NotesApp", "Secret": "...", "ExpiryMinutes": 720 },
  "Cors": { "AllowedOrigins": [ "http://localhost:5273", "http://localhost:4273" ] }
}
```

`frontend/.env` (used by `npm run dev` only — the Docker image bakes in `/api` instead):

```
VITE_API_BASE_URL=http://localhost:5215/api
```

Under Docker these files are overridden by environment variables in `docker-compose.yml`
(`ConnectionStrings__NotesDb` points at the `sqlserver` service, `Jwt__Secret`, and the
allowed CORS origins), so the same images run unchanged against a different database or
signing key:

```bash
MSSQL_SA_PASSWORD='An0ther_Str0ng_Pass!' JWT_SECRET='a-real-secret-at-least-32-bytes-long' \
  docker compose up -d --build
```

> **Note:** the committed `Jwt:Secret` and SA password are development placeholders. For
> anything beyond local use, supply them from environment variables, user-secrets
> (`Jwt__Secret=...`) or a secrets manager.

---

## Verification

**Automated tests — 191, all passing.**

| Suite | Count | Covers |
| --- | --- | --- |
| `Notes.Domain.UnitTests` | 45 | Value-object rules, entity invariants, `Result` semantics |
| `Notes.Application.UnitTests` | 23 | Use cases against test doubles, including a controllable clock |
| `Notes.Infrastructure.UnitTests` | 14 | BCrypt salting/verification, JWT claims and expiry, clock precision |
| `Notes.Api.IntegrationTests` | 28 | Real API + real SQL Server: SQL behaviour, auth, status codes, isolation |
| front-end (Vitest) | 81 | Stores, composables, the HTTP error mapper, session storage, components |

**Manual and end-to-end checks.**

- **UI (Playwright, real browser):** 22 checks — auth guard, register → notes flow, empty
  state, creating notes, title validation, search (including the literal `50%` case),
  sorting both directions, content and date filters, opening a note, editing, deleting,
  session persistence across reload, account isolation, sign-out, and no horizontal
  overflow at 390 px. Re-run unchanged after the backend rewrite: the frontend needed no
  edits, which is the evidence that the API contract was preserved.
- **Docker stack:** nginx SPA fallback (`/notes` → 200), the `/api` proxy reaching the API
  container, Swagger on the published port, cache headers, the shipped bundle compiling in
  `baseURL: "/api"`, and notes surviving a full `docker compose down && up -d`.
- **Dev stack (mounted source):** the same browser suite passes against it; editing a `.cs`
  file on the host was picked up by `dotnet watch` in ~4s, and editing a `.vue` file updated
  the running page with **0 navigations** (true HMR, not a reload). Verified that the
  container writes its build output to `/artifacts` and leaves the host's `bin`/`obj`
  untouched, so a host build still succeeds while the container runs.
- **Rate limiting:** 40 rapid sign-in attempts produced 30 × `401` then 10 × `429`.
- **Health:** `/health` now probes SQL Server and reports per-check status.

```bash
cd backend  && dotnet test     # 110 tests, 0 warnings (warnings are errors)
cd frontend && npm test && npm run lint && npm run build   # 81 tests, clean lint, bundle
docker compose up -d --build   # all three services report healthy
```

---

## Possible next steps

- Refresh tokens (the current JWT simply expires after 12 hours).
- Tags or folders, plus full-text search once the note count grows.
- Commit the Playwright end-to-end suite as a project rather than the one-off script used
  here; the Vitest unit suite is in place.
- Optimistic concurrency on update (a `RowVersion` column plus `If-Match`), so two editors
  cannot silently overwrite each other.
