---
name: add-entity
description: Add a new domain entity end to end (Domain → EF config → migration → DTO/service → endpoint → client → UI → tests). Use when asked to add a new table, model, or business concept such as Coupon, LoyaltyPoints, or DeliveryZone.
---

# Add a domain entity end to end

Follow the dependency rule in CLAUDE.md: Domain ← Application ← Infrastructure/AI ← Api.

## Steps

1. **Clarify first.** Confirm the entity name, fields (types, nullability), relationships, and who can read/write it (Customer, StoreStaff, Admin). Ask if any of these are unclear.
2. **Domain** — `src/PizzaStore.Domain/Entities/<Entity>.cs`
   - `Guid Id` initialized with `Guid.CreateVersion7()`.
   - Private setters; mutate through methods that enforce invariants.
   - Money is `decimal`; times are `DateTimeOffset` (UTC).
3. **EF configuration** — `src/PizzaStore.Infrastructure/Persistence/Configurations/<Entity>Configuration.cs`
   - Table name, keys, `HasPrecision(10,2)` for money, max lengths, indexes, FKs with explicit delete behavior.
   - Add a `rowversion` concurrency token if users can edit it concurrently.
   - Register `DbSet<Entity>` in `AppDbContext`.
4. **Migration** — follow the `ef-migration` skill.
5. **Application**
   - DTOs in `Application/<Feature>/Dtos/`, mapped by hand (no AutoMapper).
   - A FluentValidation validator for every create/update request.
   - Service interface + implementation; pass `CancellationToken` through.
6. **API** — follow the `add-endpoint` skill.
7. **Frontend** — run `npm run gen:api`, then the `react-feature` skill if UI is needed.
8. **Tests** — unit tests for invariants and validators; integration test per endpoint including an unauthorized case.
9. **Verify** — `dotnet build`, `dotnet test`, `cd web && npm run lint && npm run test`.

## Done checklist
- [ ] No EF/ASP.NET references in Domain or Application
- [ ] Migration reviewed and committed on its own
- [ ] OpenAPI client regenerated
- [ ] Authorization enforced server-side
