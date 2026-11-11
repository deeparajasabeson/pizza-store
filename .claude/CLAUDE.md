# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Pizza Store E-Commerce

Read this fully before making changes.

> **Status: not built yet.** Only `.claude/` exists so far (this file, `settings.json`, and the skills). The solution structure, CI workflows, CODEOWNERS, and templates below describe the **target** layout. When a task needs one of these, create it to match this document rather than assuming it exists. The folder is not yet a git repository: run `git init` and add the `origin` remote before following the git workflow. Remove this note once the solution is in place.

Step-by-step workflows live in `.claude/skills/`: `add-entity`, `add-endpoint`, `add-ai-tool`, `ef-migration`, `pricing-rule`, `react-feature`, `seed-menu`, `ship-pr` (quick reference in `.claude/skills/commands.txt`). Use the matching skill instead of improvising the steps.

## Project overview

An online ordering site for a single-brand pizza outlet chain (Pizza Hut–style): browse menu, customize pizzas, cart & checkout, order tracking, and an **AI customer-support chat** that can answer questions and look up the signed-in customer's orders.

- **Backend:** .NET 10 (ASP.NET Core Web API, C# 14), EF Core 10, SQL Server
- **Frontend:** React 19 + TypeScript + Vite
- **AI chat:** Azure OpenAI via `Microsoft.Extensions.AI` with tool/function calling, streamed to the browser with Server-Sent Events (SSE)
- **Order status updates:** HTTP polling from the client (no SignalR / WebSockets in this solution)

## Code conventions
- **C#:** nullable reference types on, warnings as errors; no `var` where the type isn't obvious from the right-hand side; `async` methods end in `Async` and take a `CancellationToken`.
- **TypeScript:** `strict` mode; explicit return types on exported functions and hooks; no `any` (use `unknown` and narrow).
- Every change ships with tests (see *Testing expectations*). CI enforces a coverage floor: **90% line coverage** for `Domain`, `Application`, and `AI` (coverlet), and Vitest coverage for `web/src/features/`. Generated code (`web/src/api/`, EF migrations) is excluded.

## Solution structure

```
PizzaStore.sln
├── src/
│   ├── PizzaStore.Api/              # ASP.NET Core host: endpoints, auth, SSE streaming, DI
│   ├── PizzaStore.Application/      # Use cases, DTOs, validators, interfaces (no EF, no HTTP)
│   ├── PizzaStore.Domain/           # Entities, enums, value objects, domain rules (no deps)
│   ├── PizzaStore.Infrastructure/   # EF Core DbContext, migrations, repos, payment, email
│   ├── PizzaStore.AI/               # Chat orchestration, system prompt, AI tools, guardrails
│   └── PizzaStore.AppHost/          # (optional) .NET Aspire orchestration for local dev
├── web/                             # React app (Vite)
│   ├── src/
│   │   ├── api/                     # Typed API client (generated from OpenAPI)
│   │   ├── features/                # menu/, cart/, checkout/, orders/, account/, chat/
│   │   ├── components/              # Shared UI components
│   │   ├── hooks/
│   │   └── routes/
│   └── vite.config.ts
├── tests/
│   ├── PizzaStore.UnitTests/
│   ├── PizzaStore.IntegrationTests/ # WebApplicationFactory + Testcontainers SQL Server
│   └── PizzaStore.AI.Tests/         # Tool-calling and guardrail tests
└── db/
    └── seed/                        # Menu seed data (JSON)
```

**Dependency rule:** `Domain` ← `Application` ← `Infrastructure` / `AI` ← `Api`. Domain and Application never reference EF Core, ASP.NET, or Azure SDKs.

## Common commands

Commands are written for bash (Git Bash on Windows). In PowerShell, replace `&&` with `; if ($?) { ... }` and use double quotes for the `docker run` password.

```bash
# Backend
dotnet restore
dotnet build
dotnet run --project src/PizzaStore.Api            # https://localhost:7001
dotnet test

# Run a single test project / class / test
dotnet test tests/PizzaStore.UnitTests
dotnet test tests/PizzaStore.UnitTests --filter "FullyQualifiedName~OrderStateMachineTests"
dotnet test --filter "FullyQualifiedName=PizzaStore.UnitTests.Pricing.PriceCalculatorTests.Applies_percent_deal"
dotnet test tests/PizzaStore.AI.Tests --filter "Category=Evals"      # eval set (xUnit [Trait("Category","Evals")])

# Coverage
dotnet test --collect:"XPlat Code Coverage"

# EF Core migrations (run from repo root)
dotnet ef migrations add <Name> -p src/PizzaStore.Infrastructure -s src/PizzaStore.Api
dotnet ef database update -p src/PizzaStore.Infrastructure -s src/PizzaStore.Api

# Frontend
cd web
npm install
npm run dev                                         # http://localhost:5173 (proxies /api)
npm run build
npm run lint
npm run test                                        # Vitest
npx vitest run src/features/cart                    # single folder/file
npx vitest run src/features/cart -t "applies deal"  # single test by name
npx vitest run --coverage
npx playwright test e2e/checkout.spec.ts            # single E2E spec
npm run gen:api                                     # Regenerate client from /openapi/v1.json

# Local SQL Server
docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Your_strong_pw1' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

Always run `dotnet build` and `dotnet test` (and `npm run lint && npm run test` for frontend changes) before declaring a task done.

## Domain model (core entities)

| Entity | Notes |
|---|---|
| `Store` | Outlet location, hours, IANA `TimeZoneId`, tax rate (and whether delivery is taxed), delivery radius and fee, active flag |
| `Category` | Pizzas, Sides, Desserts, Drinks, Deals |
| `MenuItem` | Name, description, base price, image, category, availability |
| `PizzaSize` | Small / Medium / Large / Family with price modifiers |
| `Crust` | Hand-tossed, Thin, Pan, Stuffed — price modifiers |
| `Topping` | Name, price, veg/non-veg flag, availability |
| `Deal` | Combo/coupon rules (code, % or fixed, min order, validity window) |
| `Customer` | Linked to Identity user; saved addresses |
| `Cart` / `CartItem` | Server-side cart; item stores selected size/crust/toppings |
| `Order` | Number, customer, store, type (Delivery/Carryout), status, totals, timestamps |
| `OrderItem` | **Snapshot** of name, options, and unit price at time of order |
| `OrderStatusHistory` | Every status transition with timestamp and actor |
| `Payment` | Provider reference, amount, status (never store card data) |
| `ChatSession` / `ChatMessage` | AI conversation log, linked to customer when signed in |

### Sales tax

- Stores are in **Georgia (US)**. Default sales tax rate: **6%**, stored per store as `Store.TaxRate` (`decimal(5,4)`, e.g. `0.0600`) and seeded in `db/seed/stores.json`.
- Keep the rate per store, never a hard-coded constant: Georgia's combined rate (4% state + county/local) varies by location, so a store may need a different rate.
- Tax applies to food and drinks for both Delivery and Carryout. Whether the delivery fee is taxed is `Store.TaxesDelivery` (default `true`; Georgia generally treats delivery charges as part of the taxable sale — confirm with the business's tax advisor).
- The rate used is snapshotted on the `Order`, so later rate changes never alter past orders.

### Order status state machine

```
Placed → Confirmed → Preparing → Baking → QualityCheck → OutForDelivery → Delivered
                                                     └→ ReadyForPickup → PickedUp
Placed / Confirmed → Cancelled   (customer or store)
Any paid state → Refunded        (staff only)
```

- Transitions are enforced in `Domain` (`Order.TransitionTo(...)`) — never set `Status` directly.
- Every transition writes `OrderStatusHistory` and updates `Order.LastStatusChangedAt`.
- Clients get updates by polling `GET /api/orders/{orderNumber}/status`, which returns status, ETA, and an `ETag`. The endpoint supports `If-None-Match` and returns `304 Not Modified` when nothing changed, so polling stays cheap.
- Customers can cancel only while `Placed` or `Confirmed`.

## Backend conventions

- **Minimal APIs** grouped by feature (`MapGroup("/api/menu")`, `/api/cart`, `/api/orders`, `/api/chat`, `/api/admin`). Keep endpoint handlers thin; logic lives in Application services.
- **Money:** `decimal(10,2)` in SQL, `decimal` in C#. All price calculation is server-side; never trust prices from the client.
- **Validation:** FluentValidation in Application; return RFC 7807 `ProblemDetails` for errors.
- **Async everywhere**, pass `CancellationToken` through to EF and HTTP calls.
- **EF Core:** Fluent configuration in `Infrastructure/Persistence/Configurations/`, one file per entity. No lazy loading. Use `AsNoTracking()` for reads. Add a `rowversion` concurrency token to `Order` and `Cart`.
- **IDs:** `Guid` (v7, `Guid.CreateVersion7()`) for primary keys; human-friendly `OrderNumber` (e.g., `PZ-240612-0042`) for display and chat lookups.
- **Times:** store `DateTimeOffset` in UTC; convert to store-local time only in the UI.
- **Nullable reference types** enabled; treat warnings as errors.
- **Logging:** `ILogger<T>` with structured templates; never log PII, addresses, payment refs, or full chat content at Information level.
- **Config/secrets:** `appsettings.json` for non-secrets; User Secrets locally, Azure Key Vault in deployed environments. Never commit connection strings or API keys.

## Authentication & authorization

- ASP.NET Core Identity with cookie auth for the SPA (same-site), or Entra External ID if configured.
- Roles: `Customer`, `StoreStaff`, `Admin`.
- Guest checkout allowed; guests track orders via `OrderNumber` + phone/email verification.
- `/api/admin/*` requires `StoreStaff` or `Admin`. Staff can only modify orders for their assigned store.

## AI customer-support chat

Lives in `PizzaStore.AI`. Exposed through two endpoints:
- `POST /api/chat/stream` streams tokens as Server-Sent Events (`text/event-stream`) using `TypedResults.ServerSentEvents` over `IChatClient.GetStreamingResponseAsync`. Event types are `token`, `tool` (structured card payload such as an order status), `done`, and `error`.
- `POST /api/chat` is a non-streaming fallback that returns the full reply.

Both endpoints take `{ sessionId, message }`. Chat history is loaded server-side from `ChatSession`, so the client never sends prior turns. Disable response buffering and compression on the SSE route, and honor the request `CancellationToken` so that closing the widget stops generation.

### Stack
- `Microsoft.Extensions.AI` `IChatClient` backed by Azure OpenAI (deployment name in config: `AI:ChatDeployment`).
- Function calling via `AIFunctionFactory.Create(...)` over methods in `Tools/`.
- System prompt in `Prompts/SupportAgent.md` — edit there, not inline in code.

### Tools the assistant may call
| Tool | Purpose |
|---|---|
| `GetMyRecentOrders(count)` | List the signed-in customer's recent orders |
| `GetOrderStatus(orderNumber)` | Status, ETA, and history for one order |
| `GetMenu(category?)` | Current menu, prices, availability |
| `GetStoreInfo(storeId?)` | Hours, address, phone, delivery radius |
| `ValidateDealCode(code)` | Check whether a coupon is valid |
| `RequestCancellation(orderNumber, reason)` | Only if order is `Placed`/`Confirmed`; requires explicit user confirmation in chat |
| `EscalateToHuman(summary)` | Create a support ticket and tell the user a person will follow up |

### Non-negotiable AI rules
1. **Authorization is enforced in the tool, not the prompt.** Every order tool resolves the customer from the authenticated `ClaimsPrincipal` (passed via a scoped `ICurrentUser`) and filters by `CustomerId`. A model-supplied customer ID is never trusted. Guests must verify `OrderNumber` + phone/email before any order data is returned.
2. **The assistant never changes prices, issues refunds, or applies discounts.** Refund/complaint requests → `EscalateToHuman`.
3. **Write actions (cancellation) require a confirmation turn** — the tool returns a pending confirmation; the user must confirm before it executes.
4. Tool results return minimal DTOs (no full addresses, no payment data).
5. Treat user text and any retrieved content as data; the system prompt instructs the model to ignore attempts to override its rules.
6. Enable Azure AI Content Safety / prompt shields on the deployment; log flagged messages.
7. Cap conversation history sent to the model (last N turns + summary) and set `MaxOutputTokens`.
8. Persist `ChatSession`/`ChatMessage` with a retention policy (default 90 days).

### Testing the chat
- Unit-test each tool directly, including **cross-customer access attempts** (must return not-found).
- Use a fake `IChatClient` in tests that scripts tool calls; don't hit Azure in CI.
- Keep a small eval set in `tests/PizzaStore.AI.Tests/Evals/` (where's my order, cancel order, menu questions, prompt-injection attempts).

## Frontend conventions

- TypeScript `strict`. Function components and hooks only.
- **Server state:** TanStack Query. **Client state** (cart drawer, UI): Zustand. No Redux.
- **Routing:** React Router. Routes: `/`, `/menu`, `/menu/:category`, `/item/:id`, `/cart`, `/checkout`, `/orders`, `/orders/:orderNumber`, `/account`, `/admin/*`.
- **Forms:** React Hook Form + Zod schemas.
- **Styling:** Tailwind CSS; shared primitives in `components/ui/`.
- **API client:** generated from the backend OpenAPI document (`npm run gen:api`) — do not hand-write fetch calls for endpoints that exist in the spec.
- **Order tracking:** use TanStack Query `refetchInterval` on the order status query. Poll every 15s while the order is active and every 60s when the tab is hidden, and stop once the order reaches `Delivered`, `PickedUp`, `Cancelled`, or `Refunded`.
- **Chat streaming:** the stream endpoint is a POST, which native `EventSource` can't send, so use `fetch` with a `ReadableStream` reader (or `@microsoft/fetch-event-source`). Keep this in a single `useChatStream` hook that supports abort via `AbortController`.
- **Chat widget** (`features/chat/`): floating button, renders streamed tokens, shows tool-driven cards (order status card, menu item card), "Talk to a person" button, accessible with keyboard and screen readers.
- Pizza builder shows live price from a server `/api/menu/price-quote` call, not a client-side formula.
- Accessibility: semantic HTML, labelled controls, visible focus, WCAG AA contrast.

## Testing expectations

- New Application logic → unit tests (xUnit + FluentAssertions or Shouldly, NSubstitute).
- New endpoints → integration tests via `WebApplicationFactory` against Testcontainers SQL Server.
- Order state machine → exhaustive transition tests.
- Pricing (sizes, crusts, toppings, deals, tax) → table-driven tests.
- Frontend → Vitest + React Testing Library for components; Playwright for checkout and order-tracking E2E.

## Things to avoid

- Don't put business logic in controllers/endpoints or React components.
- Don't compute order totals on the client.
- Don't call `Database.EnsureCreated()` — use migrations.
- Don't edit an existing applied migration; add a new one.
- Don't store card numbers or CVV anywhere (use the payment provider's hosted fields/tokens).
- Don't add new NuGet/npm packages without stating why in the PR description.
- Don't let the AI assistant query the database directly or build SQL — only through the defined tools.
- Don't introduce SignalR, raw WebSockets, or other persistent-connection libraries. Use SSE for chat streaming and HTTP polling for order status.

## Version control & team collaboration (GitHub)

- **Owner account:** [github.com/deeparajasabeson](https://github.com/deeparajasabeson)
- **Repository:** `deeparajasabeson/pizza-store` (update this if the repo has a different name)
- **Remote:** `origin` → `https://github.com/deeparajasabeson/pizza-store.git`
- **Default branch:** `main` (protected)

### Branching
- `main` is always deployable and protected: it requires a PR, at least 1 approving review, passing CI, and no direct pushes.
- `develop` is optional. Use it only if the team adopts release batching; otherwise PRs go straight to `main`.
- Feature branches come from `main` and follow the pattern `<type>/<issue#>-<short-desc>`:
  - `feature/42-pizza-builder`, `fix/57-cart-total-rounding`, `chore/61-upgrade-efcore`, `ai/73-cancel-order-tool`
- Keep branches short-lived. Rebase on `main` before opening or updating a PR.

### Commits
- Use [Conventional Commits](https://www.conventionalcommits.org/): `feat(cart): apply deal codes at checkout`, `fix(orders): block cancel after Preparing`, `test(ai): cross-customer access cases`.
- Scopes: `api`, `domain`, `infra`, `ai`, `web`, `db`, `ci`, `docs`.
- Each commit should be one logical change that builds. Reference the issue in the body (`Refs #42`) or close it in the PR (`Closes #42`).
- Migrations get their own commit: `feat(db): add OrderStatusHistory table`.

### Pull requests
- Open PRs with `gh pr create` using `.github/pull_request_template.md`, which covers summary, linked issue, screenshots for UI changes, migration notes, new packages with justification, and a test checklist.
- Open the PR as a draft until build, tests, and lint pass locally.
- Request review from the code owners in `.github/CODEOWNERS`:
  ```
  *                       @deeparajasabeson
  /src/PizzaStore.AI/     @deeparajasabeson
  /web/                   @deeparajasabeson
  /src/PizzaStore.Infrastructure/Persistence/Migrations/ @deeparajasabeson
  ```
  Add teammates' handles (or a team such as `@<org>/pizza-store-devs`) as they join.
- Merge with **squash merge**, using a PR title in Conventional Commit format. Delete the branch after merge.

### Issues & planning
- Track work in GitHub Issues with labels: `feature`, `bug`, `ai-chat`, `frontend`, `backend`, `database`, `good first issue`, `blocked`.
- Use a GitHub Project board (Backlog → Ready → In Progress → In Review → Done) for sprint tracking.
- Use issue templates in `.github/ISSUE_TEMPLATE/` (bug report, feature request).

### CI/CD (GitHub Actions)
- `.github/workflows/ci.yml` runs on PRs and on pushes to `main`:
  - `dotnet build` and `dotnet test` (integration tests use Testcontainers SQL Server), failing below the coverage floor in *Code conventions*
  - `npm ci`, `npm run lint`, `npm run test`, and `npm run build` in `web/`
  - A check that the EF model has no pending changes: `dotnet ef migrations has-pending-model-changes`
- `.github/workflows/deploy.yml` deploys from `main` to Azure. It authenticates with OIDC (`azure/login` federated credentials), not stored passwords.
- Dependabot (`.github/dependabot.yml`) handles NuGet, npm, and GitHub Actions updates weekly.

### Secrets
- Never commit secrets. Keep `.gitignore` covering `appsettings.*.local.json`, `.env*`, `*.user`, `bin/`, `obj/`, `node_modules/`, and `web/dist/`.
- CI secrets live in **GitHub Actions secrets / environments** (`staging`, `production` with required reviewers). App secrets live in Azure Key Vault.
- Turn on GitHub secret scanning and push protection for the repository.

### Rules for Claude with git/GitHub
- Never push directly to `main`, never force-push to shared branches, and never rewrite published history.
- Never merge a PR, delete a remote branch, or change repo settings/branch protection unless the user explicitly asks in chat.
- Before committing, show the user the diff summary and proposed commit message.
- Before pushing a new branch or opening a PR, confirm with the user. Then use `gh` and report the PR URL.
- Don't add or remove collaborators, change CODEOWNERS ownership, or modify workflow permissions without explicit instruction.
- If `gh` isn't authenticated, ask the user to run `gh auth login` themselves. Never handle tokens or passwords.

## Workflow for Claude

1. Start from an up-to-date `main` on a correctly named branch tied to a GitHub issue. For non-trivial changes, outline the plan (files to touch, migrations needed) before editing.
2. Keep changes scoped to the task; don't refactor unrelated code.
3. When adding an entity: Domain entity → EF configuration → migration → Application DTO/service → endpoint → generated client → UI → tests.
4. When adding an AI tool: implement in `PizzaStore.AI/Tools/`, enforce auth inside it, register it, document it in this file's tool table, add tests including an unauthorized-access case.
5. Run build, tests, and lint before finishing; report anything that failed.
6. Commit with a Conventional Commit message and, once the user confirms, push the branch and open a PR against `main` that links the issue.
