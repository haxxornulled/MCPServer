# AGENTS

Repository instructions for coding agents, including Codex-style agents.

## Scope

These instructions apply to the entire repository.

## Build And Test Baseline

- Solution: `McpServer.slnx`
- CI workflow: `.github/workflows/ci.yml`
- CI configuration: `Release`
- CI platform: `windows-latest`

When validating CI-related fixes, mirror the workflow exactly:

```powershell
dotnet restore .\McpServer.slnx
dotnet build .\McpServer.slnx -c Release --no-restore -v minimal
dotnet test .\tests\McpServer.UnitTests\McpServer.UnitTests.csproj -c Release --no-build -v minimal
dotnet test .\tests\McpServer.IntegrationTests\McpServer.IntegrationTests.csproj -c Release --no-build -v minimal
```

If you run a targeted test with `--no-build`, rebuild that project first in the same configuration you are testing.

## CI Failure Handling

When a user asks to kick CI, inspect a failed run, or fix CI:

1. Read `.github/workflows/ci.yml` first.
2. Inspect the exact run with `gh run view`.
3. Reproduce the failure locally in `Release`.
4. Fix the root cause, not the symptom.
5. Re-run the narrowest local validation that proves the fix.
6. Re-run the CI-equivalent command path.
7. Commit, push, and re-dispatch CI if the user wants the workflow updated remotely.

Useful commands:

```powershell
gh run view <run-id> --json status,conclusion,url,jobs,displayTitle,headSha,updatedAt
gh run view <run-id> --log-failed
gh workflow run .github/workflows/ci.yml --ref main
gh run list --workflow ci.yml --limit 3 --json databaseId,status,conclusion,url,displayTitle,headSha,headBranch,event,createdAt
git push origin main
```

## Testing Guidance

- Never assume `Debug` paths in tests that can run in CI under `Release`.
- Prefer deriving expected artifact paths from `AppContext.BaseDirectory` or the active test configuration.
- Keep integration tests independent from the caller's current working directory.
- Preserve strong assertions. Do not paper over failures by removing checks.

## Documentation Guidance

If you add or materially change tooling, workflows, or public behavior, update the relevant docs in the same change:

- `README.md`
- `CHANGELOG.md`
- `docs/architecture.md`
- `docs/method-summary.md`

## Commit Hygiene

- Do not commit generated `.lscache` files, temporary logs, or files under runtime workspace outputs.
- Keep commits focused.
- Include the fix and the minimum documentation updates needed to explain it.

## Expected Closeout

When you finish a CI-related fix, report:

- the root cause
- the local validation commands that passed
- the commit SHA
- the push result
- the rerun workflow URL and final status if available