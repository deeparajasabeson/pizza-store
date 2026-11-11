---
name: add-endpoint
description: Add or change a Minimal API endpoint with validation, authorization, ProblemDetails errors, OpenAPI metadata, and integration tests. Use when asked to add, expose, or modify an /api route.
---

# Add a Minimal API endpoint

## Steps
1. **Place it** in the feature's endpoint file, e.g. `src/PizzaStore.Api/Endpoints/OrderEndpoints.cs`, inside its `MapGroup("/api/<feature>")`.
2. **Keep the handler thin** — bind the request, call one Application service method, map the result. No business logic, no `DbContext` in the handler.
3. **Authorization**
   - Apply `.RequireAuthorization(...)` with the right policy (`Customer`, `StoreStaff`, `Admin`).
   - Ownership checks (customer owns the order, staff belongs to the store) live in the Application service using `ICurrentUser`, not just on the route.
   - Return 404, not 403, when a customer asks for someone else's resource.
4. **Validation** — FluentValidation validator for the request; failures return `ValidationProblem`.
5. **Results** — use `TypedResults` (`Ok`, `Created`, `NotFound`, `Conflict`, `ValidationProblem`) so OpenAPI is accurate. Concurrency conflicts (`DbUpdateConcurrencyException`) → 409.
6. **OpenAPI** — `.WithName(...)`, `.WithSummary(...)`, `.Produces...` where `TypedResults` isn't enough. Name drives the generated client method.
7. **Caching for polled reads** — for status-style endpoints, return an `ETag` and honor `If-None-Match` with 304 (see order status in CLAUDE.md).
8. **Never accept prices or totals from the client.**
9. **Tests** in `tests/PizzaStore.IntegrationTests/` using `WebApplicationFactory` + Testcontainers:
   - happy path
   - validation failure (400)
   - unauthenticated (401)
   - other customer's resource (404)
10. **Regenerate the client**: run the API, then `cd web && npm run gen:api`.

## Done checklist
- [ ] Handler under ~15 lines
- [ ] Auth + ownership tested
- [ ] OpenAPI name set and client regenerated
