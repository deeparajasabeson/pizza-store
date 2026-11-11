---
name: ef-migration
description: Create, review, and apply an EF Core migration for SQL Server safely. Use whenever an entity, EF configuration, or the DbContext changes, or when asked to add/fix a migration or update the database.
---

# EF Core migration workflow

## Create
```bash
dotnet ef migrations add <PascalCaseName> -p src/PizzaStore.Infrastructure -s src/PizzaStore.Api
```
Name it for the change: `AddDealTable`, `AddOrderLastStatusChangedAt`, `IndexOrderNumber`.

## Review the generated file before applying
Open the new file under `Infrastructure/Persistence/Migrations/` and check:
- **Data loss:** any `DropColumn`, `DropTable`, `RenameColumn` shown as drop+add, or a column made non-nullable on a table with rows. If found, stop and tell the user; propose a safe sequence (add nullable → backfill with `migrationBuilder.Sql(...)` → make required in a later migration).
- **Money columns** are `decimal(10,2)`.
- **Indexes** exist for FKs and lookup columns (`OrderNumber`, `CustomerId`, `Status`).
- **Down()** genuinely reverses Up().
- No accidental changes to unrelated tables (a sign of model drift — investigate rather than commit).

## Apply locally
```bash
dotnet ef database update -p src/PizzaStore.Infrastructure -s src/PizzaStore.Api
dotnet ef migrations has-pending-model-changes -p src/PizzaStore.Infrastructure -s src/PizzaStore.Api
```
The second command must report no pending changes (CI runs it too).

## Rules
- Never edit a migration that has been merged to `main`; add a new one.
- Never use `Database.EnsureCreated()`.
- To undo an unmerged local migration: `dotnet ef database update <PreviousMigration>` then `dotnet ef migrations remove`.
- Commit the migration on its own: `feat(db): <what changed>`.
- For production, generate an idempotent script for review instead of running `update` against prod:
  `dotnet ef migrations script --idempotent -o artifacts/migrate.sql -p src/PizzaStore.Infrastructure -s src/PizzaStore.Api`
