# Pizza Store — Build Plan

The order for setting up the repo and building the app with Claude Code.
CLAUDE.md and the skills in `.claude/skills/` are not run as commands. Claude Code loads them automatically in every session started in this repo. Skills are triggered with `/skill-name` or by describing the task. See `.claude/skills/commands.txt` for each skill's arguments.

Tick items off as they are done so the team can see progress.

---

## Phase 0 — One-time setup (manual, in a terminal)

- [x] **Install prerequisites:** .NET 10 SDK, Node 20.19+ (CI uses Node 24), SQL Server (local instance; see below), GitHub CLI (`gh`), Claude Code. Docker is not needed: local development and integration tests use the local SQL Server instance.
- [x] **Create and clone the repo:**
  ```bash
  gh auth login
  gh repo create deeparajasabeson/pizza-store --public --clone
  cd pizza-store
  ```
- [x] **Add the guidance files:** put `CLAUDE.md` in `.claude/CLAUDE.md`, then unzip `claude-skills.zip` at the repo root (this creates `.claude/skills/` and `commands.txt`). Put this file in `docs/BUILD-PLAN.md`.
- [x] **First commit directly to `main`** (before branch protection is on):
  ```bash
  git add .claude && git commit -m "chore: add CLAUDE.md and project skills" && git push -u origin main
  ```
- [ ] **Branch protection:** GitHub → Settings → Branches → Add rule (see the settings below). *Rule exists with 1 required approval and force pushes blocked; still to do: **Require linear history** and the required status checks (Phase 1).*
- [x] **Security:** Settings → Advanced Security → enable secret scanning and push protection.
- [ ] **Database:** set up the local SQL Server connection (see below).
- [x] **Verify Claude Code:** run `claude` in the repo and ask *"What skills do you have for this project?"* All 8 should be listed.

### Branch protection rule

Branch name pattern: **`main`**

> The repo is **public** because GitHub Free only enforces branch rules on public repositories. Never commit secrets.

| Setting | Choice |
|---|---|
| Require a pull request before merging | ✅ |
| Require approvals (1) | ✅ once a teammate joins; while solo, leave it off or allow admin bypass, because you can't approve your own PR |
| Dismiss stale approvals when new commits are pushed | ✅ |
| Require review from Code Owners | ✅ once teammates are in CODEOWNERS |
| Require status checks to pass before merging | ✅ — add the CI job names `Backend (.NET)` and `Frontend (web)` |
| Require branches to be up to date before merging | ✅ |
| Require conversation resolution before merging | ✅ |
| Require linear history | ✅ (matches squash merge) |
| Allow force pushes / Allow deletions | ❌ |

### Local SQL Server

Development uses the local default instance **`DeepaSteele2025`** (Windows authentication, no password). Local apps connect to it over shared memory, so no port is needed. Integration tests use the same instance with a throwaway per-run database; see *Database* in `.claude/CLAUDE.md`.

Store the connection string in User Secrets, never in `appsettings.json` (the Api project already has User Secrets enabled):
```bash
dotnet user-secrets set "ConnectionStrings:PizzaStore" "Server=DeepaSteele2025;Database=PizzaStore;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True" --project src/PizzaStore.Api
dotnet tool restore
dotnet ef database update -p src/PizzaStore.Infrastructure -s src/PizzaStore.Api
```
- Use the machine name (or `.`), not `localhost`: `localhost` connects over TCP, which isn't enabled on this instance.
- `TrustServerCertificate=True` is for local development only.
- The `PizzaStore` database is created by `dotnet ef database update` once the first migration exists.
- To connect from WSL or another machine, enable **TCP/IP** on port **1433** in SQL Server Configuration Manager and restart the service.

---

## Workflow for every step after Phase 1

One GitHub issue → one branch → one `/ship-pr`. Start each phase in plan mode (Shift+Tab) so Claude outlines files and migrations before editing, and run `/clear` between features.

---

## Phase 1 — Scaffold

- [x] Prompt: *"Scaffold the solution exactly as described in CLAUDE.md: .NET 10 projects with references, the Vite React TS app in web/, test projects, `.github/` (ci.yml, PR template, issue templates, CODEOWNERS, dependabot), and `.gitignore`. No features yet."*
- [x] `/ship-pr` — merged once CI was green (#2, closes #1).
- [ ] Add the CI job names `Backend (.NET)` and `Frontend (web)` to the branch protection required status checks.

## Phase 2 — Menu & catalog

- [ ] `/add-entity` Store, Category, PizzaSize, Crust, Topping, MenuItem, Deal (each runs `/ef-migration`)
- [ ] `/seed-menu` — starting menu, stores, deals
- [ ] `/add-endpoint` `GET /api/menu`, `GET /api/menu/{category}`
- [ ] `/react-feature` menu pages `/menu`, `/item/:id`
- [ ] `/ship-pr`

## Phase 3 — Pricing

- [ ] `/pricing-rule` base calculator: sizes, crusts, toppings, tax
- [ ] `/add-endpoint` `POST /api/menu/price-quote`
- [ ] `/react-feature` pizza builder with live server price
- [ ] `/ship-pr`

## Phase 4 — Accounts

- [ ] Prompt: *"Set up ASP.NET Core Identity with cookie auth and Customer/StoreStaff/Admin roles per CLAUDE.md."*
- [ ] `/add-entity` Customer (saved addresses)
- [ ] `/react-feature` sign-in, sign-up, `/account`
- [ ] `/ship-pr`

## Phase 5 — Cart & checkout

- [ ] `/add-entity` Cart, CartItem, Order, OrderItem, OrderStatusHistory, Payment
- [ ] Prompt: *"Implement Order.TransitionTo with the state machine in CLAUDE.md and exhaustive transition tests."*
- [ ] `/add-endpoint` cart CRUD and `POST /api/orders` (including guest checkout)
- [ ] `/pricing-rule` deal codes and delivery fee at checkout
- [ ] `/react-feature` cart drawer and checkout, with e2e
- [ ] `/ship-pr`

## Phase 6 — Order tracking

- [ ] `/add-endpoint` `GET /api/orders/{orderNumber}/status` — polled (ETag/304)
- [ ] `/react-feature` order list and tracking page with polling, with e2e
- [ ] `/ship-pr`

## Phase 7 — AI support chat

- [ ] Prompt: *"Set up PizzaStore.AI per CLAUDE.md: IChatClient on Azure OpenAI, ICurrentUser, SupportAgent.md prompt, ChatSession persistence, SSE /api/chat/stream plus the /api/chat fallback, and a fake IChatClient for tests."*
- [ ] `/add-ai-tool` GetMenu
- [ ] `/add-ai-tool` GetStoreInfo
- [ ] `/add-ai-tool` GetMyRecentOrders
- [ ] `/add-ai-tool` GetOrderStatus
- [ ] `/add-ai-tool` ValidateDealCode
- [ ] `/add-ai-tool` EscalateToHuman
- [ ] `/add-ai-tool` RequestCancellation (last — the only write tool)
- [ ] `/react-feature` chat widget with `useChatStream`, order and menu cards
- [ ] `/ship-pr`

## Phase 8 — Admin & deploy

- [ ] `/add-endpoint` + `/react-feature` staff order board at `/admin` (advance status, store-scoped)
- [ ] Prompt: *"Create deploy.yml for Azure with OIDC and Key Vault references per CLAUDE.md."*
- [ ] `/ef-migration script` — production SQL for review
- [ ] `/ship-pr`

---

## Keeping the guidance current

If Claude keeps missing a convention, add it to CLAUDE.md or the relevant `SKILL.md`, and commit the change with `/ship-pr` so the whole team gets it.
