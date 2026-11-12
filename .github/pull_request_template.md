## Summary

<!-- What does this change do, and why? -->

## Linked issue

Closes #

## Screenshots (UI changes)

<!-- Before / after, or "N/A". -->

## Migration notes

<!-- New migrations, data backfills, or deployment order. "None" if not applicable. -->

## New packages

<!-- Each new NuGet or npm package and why it's needed. "None" if not applicable. -->

| Package | Project | Why |
|---|---|---|

## Test checklist

- [ ] `dotnet build` passes (no warnings)
- [ ] `dotnet test` passes
- [ ] `dotnet ef migrations has-pending-model-changes` reports no changes
- [ ] `npm run lint && npm run test && npm run build` pass in `web/` (frontend changes)
- [ ] New logic has unit tests; new endpoints have integration tests
- [ ] AI tools: cross-customer access and prompt-injection cases covered (AI changes)
- [ ] OpenAPI document and generated client committed if endpoints changed
