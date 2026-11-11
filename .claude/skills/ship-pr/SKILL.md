---
name: ship-pr
description: Take finished work from a local branch to a GitHub pull request on deeparajasabeson/pizza-store — verify, commit with Conventional Commits, push, and open the PR with gh. Use when asked to commit, push, open a PR, or "ship" a change.
---

# Ship a change as a pull request

Follow "Version control & team collaboration" in CLAUDE.md. Never push to `main`, never force-push shared branches, never merge.

## Steps
1. **Check the branch.** `git branch --show-current`. If on `main`, create `<type>/<issue#>-<short-desc>` from an up-to-date `main` (`git fetch origin && git switch -c ... origin/main`). Ask for the issue number if unknown.
2. **Verify locally** — all must pass; report failures instead of continuing:
   ```bash
   dotnet build && dotnet test
   dotnet ef migrations has-pending-model-changes -p src/PizzaStore.Infrastructure -s src/PizzaStore.Api
   cd web && npm run lint && npm run test && npm run build
   ```
3. **Scan for secrets** — `git diff --cached` and the working diff must not contain connection strings, keys, or `.env` values. Stop if found.
4. **Show the user** the `git status` / diff summary and the proposed commit message(s). Wait for approval.
5. **Commit** — Conventional Commits with scope (`api`, `domain`, `infra`, `ai`, `web`, `db`, `ci`, `docs`). Migrations in their own commit. Body references the issue (`Refs #42`).
6. **Rebase** on `origin/main` and re-run tests if anything changed.
7. **Confirm** with the user, then push: `git push -u origin <branch>`.
8. **Open the PR** as a draft if anything is unfinished:
   ```bash
   gh pr create --base main --title "<conventional title>" --body-file <filled template> [--draft]
   ```
   Fill `.github/pull_request_template.md`: summary, `Closes #<issue>`, screenshots for UI, migration notes, new packages + reason, test checklist.
9. **Report** the PR URL and CI status (`gh pr checks`).

## If `gh` isn't authenticated
Ask the user to run `gh auth login` themselves. Never handle tokens.
