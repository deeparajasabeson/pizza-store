---
name: seed-menu
description: Add, change, or remove menu data (categories, items, sizes, crusts, toppings, deals, stores) in db/seed JSON and load it idempotently into SQL Server. Use when asked to update the menu, add a pizza or topping, change seed prices, add a store, or reset local data.
---

# Update menu seed data

## Files
```
db/seed/
  stores.json
  categories.json
  sizes.json
  crusts.json
  toppings.json
  menu-items.json
  deals.json
```
Loaded by `src/PizzaStore.Infrastructure/Persistence/Seeding/MenuSeeder.cs`, which runs in Development on startup and via `dotnet run --project src/PizzaStore.Api -- seed` elsewhere.

## Rules
1. **Stable keys.** Every record has a fixed `id` (GUID) and a unique `slug`. Never change an existing id; the seeder upserts by id.
2. **Prices** are decimal strings with 2 places (`"12.99"`), never floats.
3. **References by slug** (`"category": "pizzas"`, `"defaultToppings": ["mozzarella","pepperoni"]`). The seeder resolves them and fails loudly on an unknown slug.
4. **Removing an item:** set `"isAvailable": false` instead of deleting, so historical orders and the AI's menu answers stay consistent. Delete only if the user explicitly asks and nothing references it.
5. **Images:** path relative to `web/public/menu/` (`"image": "menu/margherita.webp"`); add the file in the same change.
6. **Veg flag** required on every topping and pizza (`"isVeg": true|false`).
7. **Deals:** include `code`, `type`, `value`, `minOrder`, `validFrom`, `validTo` (ISO 8601 UTC), `orderTypes`, and `stackable`.
8. Seed data contains **no real customer data** and no secrets.

## Steps
1. Edit the JSON; keep files sorted by `category` then `name` for readable diffs.
2. Validate: `dotnet test --filter Category=Seed` (checks schema, unique ids/slugs, resolvable references, 2-decimal prices).
3. Load locally: run the API in Development (or the `seed` command) and confirm in `/api/menu`.
4. If a pricing change affects deals or totals, also follow the `pricing-rule` skill.
5. If a field was added to the JSON that the schema doesn't have, add the entity property via `add-entity` / `ef-migration` first.
6. Commit as `feat(db): <menu change>` or `chore(db): update seed prices`.

## Reset local data
```bash
dotnet ef database drop -f -p src/PizzaStore.Infrastructure -s src/PizzaStore.Api
dotnet ef database update -p src/PizzaStore.Infrastructure -s src/PizzaStore.Api
dotnet run --project src/PizzaStore.Api -- seed
```
Only against the local database — never staging or production.

## Done checklist
- [ ] No id changed, no hard deletes
- [ ] Seed tests pass
- [ ] Images present
- [ ] Menu visible at /api/menu
