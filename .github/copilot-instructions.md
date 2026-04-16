# MCPServer Copilot Instructions

Apply these instructions for all work in this repository.

## Repo Defaults

- Target stack: .NET 10, C# 14, Windows-friendly paths.
- Primary solution file: `McpServer.slnx`.
- CI workflow: `.github/workflows/ci.yml`.
- CI runs on `windows-latest` and uses `Release` configuration.

## CI And Validation Workflow

When a user asks to kick CI, check CI, fix CI, or handle a failed workflow:

1. Inspect the workflow file before choosing validation commands.
2. Use GitHub CLI to inspect the exact failed run and failed steps.
3. Reproduce failures locally in the same configuration as CI.
4. Prefer the narrowest fix that addresses the actual root cause.
5. Re-run the smallest matching validation first, then the CI-equivalent command path.
6. If the fix is correct, commit, push, and re-dispatch the workflow when the user wants that done.

Use these repository-specific CI commands unless the workflow changes:

```powershell
dotnet restore .\McpServer.slnx
dotnet build .\McpServer.slnx -c Release --no-restore -v minimal
dotnet test .\tests\McpServer.UnitTests\McpServer.UnitTests.csproj -c Release --no-build -v minimal
dotnet test .\tests\McpServer.IntegrationTests\McpServer.IntegrationTests.csproj -c Release --no-build -v minimal
```

If `--no-build` is used in CI, make sure the relevant project was actually rebuilt in the same configuration before trusting a local rerun.

## GitHub Actions Commands

Use GitHub CLI for workflow operations:

```powershell
gh run view <run-id> --json status,conclusion,url,jobs,displayTitle,headSha,updatedAt
gh run view <run-id> --log-failed
gh workflow run .github/workflows/ci.yml --ref main
gh run list --workflow ci.yml --limit 3 --json databaseId,status,conclusion,url,displayTitle,headSha,headBranch,event,createdAt
```

If `gh run view --log-failed` is temporarily unavailable while a job is still finishing, fetch logs again after completion instead of guessing.

## Fixing Tests

- Match the configuration used by CI. Do not hard-code `Debug` paths in tests that can run under `Release`.
- Derive expected output locations from runtime context when possible.
- Keep integration tests resilient to working directory differences.
- Do not weaken assertions just to make CI pass.

## Commits And Pushes

- Do not commit generated caches, log files, or temporary workspace artifacts.
- When the user asks for a commit, include any necessary docs for new tools or workflow changes.
- When the user asks to update the remote, push `main` to `origin` unless they specify otherwise.

## Documentation Expectations

Update these when behavior changes materially:

- `README.md` for user-facing setup or tooling changes.
- `CHANGELOG.md` for release notes.
- `docs/architecture.md` and `docs/method-summary.md` when public behavior or major components change.

## Extension Points

Follow the existing architecture seams when extending the server:

- add new tools by implementing `IToolHandler<TRequest>` and registering the handler in Autofac plus `ToolCallRouter`
- add new resources by implementing `IResourceHandler` and registering the handler in Autofac
- add new prompts by implementing `IPromptHandler` and registering the handler in Autofac
- add new infrastructure services behind application abstractions instead of coupling protocol or host layers to concrete implementations

## Agent Behavior

- Do not stop at identifying a failed CI job if you can reproduce and fix it.
- Report the concrete failing test, step, or command and the root cause.
- After fixing and validating, report the new commit SHA, push result, and rerun URL.