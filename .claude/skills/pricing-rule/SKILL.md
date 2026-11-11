---
name: pricing-rule
description: Add or change how prices are calculated — pizza size/crust/topping modifiers, deals and coupon codes, combos, delivery fees, tax, rounding — with table-driven tests. Use when asked to change a price, add a deal or promo, fix a total, or adjust tax or fees.
---

# Add or change a pricing rule

All pricing is server-side. The client only displays what `/api/menu/price-quote`, the cart, and the order return.

**Not this skill:** changing a menu price, adding a topping, or adding a deal that uses an existing deal type is a data change → use `seed-menu`. Use this skill when the *calculation* changes: a new deal type, a new fee, tax behaviour, rounding, or a pricing bug.

## Inputs
- **Required:** the rule or change in plain words.
- **For a deal:** code, type (% off / fixed off / BOGO / combo / free delivery), value, min order, valid from/to, days/hours, order types, eligible items or categories, stores (default: all), stackable (default: no), per-customer limit.
Ask for anything missing that changes the result. Don't guess amounts or dates.

## Where pricing lives
- `src/PizzaStore.Domain/Pricing/` — pure calculation: `PriceCalculator`, `LineItemPrice`, `DealRule` types. No EF, no I/O, no `DateTime.Now`.
- `src/PizzaStore.Application/Pricing/` — loads menu, deals, and the store's tax rate and time zone; gets the current time from an injected `TimeProvider` (`timeProvider.GetUtcNow()`) and passes it into the calculator. Tests use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`.
- One calculator is used by price-quote, cart, checkout, and order creation, so totals always match.

## Steps
1. **Clarify** the inputs above. Write 2–3 worked examples (input → expected total) and confirm them with the user before coding.
2. **Write the failing tests first** in `tests/PizzaStore.UnitTests/Pricing/` from those examples.
3. **Domain:** add or change the `DealRule` subtype or calculator step. Keep the calculation order below.
4. **Persistence (if new fields):** update the entity and its EF configuration, then run the `ef-migration` skill.
5. **Application:** load any new data and pass it to the calculator. Return eligibility failures as reasons (see *Deals*).
6. **Seed data (if any):** add the deal or fee to `db/seed/` via the `seed-menu` skill.
7. **Frontend:** only if a new field must be displayed. Regenerate the client (`npm run gen:api`); never compute the amount in React.
8. **Verify:** `dotnet build && dotnet test`, plus `npm run lint && npm run test` if `web/` changed.

## Calculation order (keep this fixed)
1. Unit price = item base × size modifier + crust modifier + toppings (after free-topping allowances). Round to 2 dp.
2. Line total = unit price × quantity.
3. Subtotal = sum of line totals.
4. Deals: apply the **single best eligible** deal (largest discount; ties → earliest-created deal) unless deals are marked stackable. Check validity window, days/hours, min order (against the pre-discount subtotal), eligible items, store, order type (Delivery/Carryout), and per-customer usage limit. Cap the discount at the eligible subtotal.
5. Delivery fee: Delivery orders only; waived only by a free-delivery deal. Other deals never discount the fee.
6. Tax: `Store.TaxRate` × (subtotal − discount + fee if `Store.TaxesDelivery`). Round to 2 dp. Georgia stores default to 6% (`0.0600`) with delivery taxed; see *Sales tax* in CLAUDE.md. Never hard-code the rate.
7. Total = subtotal − discount + fee + tax. Never negative.

## Money rules
- `decimal` only, never `double`/`float`. Prices are stored as `decimal(10,2)`, so inputs always have at most 2 dp.
- Round with `MidpointRounding.AwayFromZero` in three places only: unit price, per-line discount allocation, and tax.
- Order-level discounts are allocated across eligible lines in proportion to line total, each rounded to 2 dp. Any leftover cent goes to the largest line, so the line discounts add up exactly to the order discount. This keeps single-item refunds correct.
- Store a pricing snapshot on the `Order`: deal code, discount amount, per-line discount, tax rate, tax amount, and delivery fee.

## Time-based deals
- Deal windows are stored in UTC (`DateTimeOffset`).
- Day-of-week and happy-hour rules (e.g. "Tuesdays only", "3–5 pm") are evaluated in the **store's local time zone** (`TimeZoneInfo` from the store's `TimeZoneId`), not the server's.
- Validity is inclusive at the start and exclusive at the end.

## Deals
- Validate codes server-side (`ValidateDealCode`); trimmed and case-insensitive.
- An expired or ineligible code returns a reason (`NotFound`, `NotStarted`, `Expired`, `MinOrderNotMet`, `NotValidForOrderType`, `NotValidAtStore`, `UsageLimitReached`), not a generic error.
- Per-customer limits apply only to signed-in customers. For guests, either block limited deals or match on verified email/phone; pick one and document it in the deal.
- Count a redemption when the order is placed, in the same transaction, so two concurrent orders can't both use a single-use code.
- The AI assistant may **check** a code, never **apply** or create one.

## Tests (required)
Table-driven xUnit `[Theory]` tests in `tests/PizzaStore.UnitTests/Pricing/`:
- each size × crust combination
- toppings with and without allowances
- every deal type: % off, fixed off, BOGO, combo, free delivery
- best-deal selection, including ties and stackable vs non-stackable
- boundaries: exactly at min order, one cent below; window start (valid) and end (invalid)
- store-local time: a Tuesday deal just before and after midnight local time, and across a DST change
- rounding: 15% off $9.99 (= $1.4985 → $1.50); a 3-line order where the discount doesn't split evenly (line discounts must add up to the order discount)
- discount larger than the eligible subtotal → capped; total = fee + tax, never negative
- Georgia 6% tax: Carryout $20.00 → tax $1.20, total $21.20; Delivery $20.00 + $3.99 fee → tax $1.44 ($1.4394 rounded), total $25.43; $20.00 with 10% off → tax $1.08 (tax is on the discounted amount); a store with a different rate (e.g. `0.0700`) uses its own rate
- every eligibility failure reason above

Integration test in `tests/PizzaStore.IntegrationTests/`: price-quote, cart, and placed-order totals agree for the same input.

## Done checklist
- [ ] Worked examples confirmed with the user
- [ ] Calculator is pure and deterministic (time injected)
- [ ] Rounding only at unit price, discount allocation, and tax
- [ ] Line discounts add up exactly to the order discount
- [ ] Day/time rules use the store's time zone
- [ ] Order stores a pricing snapshot
- [ ] Table-driven tests cover boundaries; quote/cart/order integration test passes
- [ ] `dotnet build` and `dotnet test` pass
