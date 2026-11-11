---
name: react-feature
description: Build or change a React feature in web/ (pages, components, hooks) using the generated API client, TanStack Query, Zustand, React Hook Form + Zod, Tailwind, and tests. Use for any frontend UI work — menu, pizza builder, cart, checkout, order tracking, account, admin, or the chat widget.
---

# Build a React feature

## Structure
```
web/src/features/<feature>/
  components/      # feature-specific components
  hooks/           # useXxx hooks wrapping TanStack Query
  routes/          # route-level pages
  <feature>.test.tsx
```
Shared primitives go in `web/src/components/ui/`.

## Steps
1. **Regenerate the client** if the backend changed: `npm run gen:api`. Never hand-write `fetch` for an endpoint in the OpenAPI spec.
2. **Data hooks** — wrap generated calls in TanStack Query hooks with stable query keys (`['orders', orderNumber]`). Invalidate related keys after mutations (e.g. cart → `['cart']`).
3. **Client state** (drawer open, selected tab) → Zustand store in the feature. Server data never goes in Zustand.
4. **Forms** — React Hook Form + Zod schema; show field errors and map server `ProblemDetails.errors` back onto fields.
5. **Prices** — display what the server returns. The pizza builder calls `/api/menu/price-quote`; never compute totals in the browser.
6. **Order tracking** — `refetchInterval`: 15s active, 60s when the tab is hidden, `false` once status is terminal.
7. **Chat** — reuse `useChatStream` (fetch + ReadableStream, `AbortController`). Don't add SignalR or WebSockets.
8. **States** — every data view handles loading (skeleton), empty, and error.
9. **Accessibility** — semantic elements, labels on every control, visible focus, keyboard support, `aria-live="polite"` for streamed chat text and status changes.
10. **Tests** — Vitest + React Testing Library for components (mock the API layer with MSW). Add or update a Playwright test if the change touches checkout or order tracking.
11. **Verify** — `npm run lint && npm run test && npm run build`.

## Done checklist
- [ ] No hand-written fetch for spec'd endpoints
- [ ] No client-side price math
- [ ] Loading / empty / error states
- [ ] Keyboard and screen-reader usable
