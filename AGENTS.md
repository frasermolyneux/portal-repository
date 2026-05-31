# AGENTS.md — portal-repository

Portal Repository API + EF Core data layer + SQL Database project for the XtremeIdiots Portal. Two API hosts (V1 and V2) deployed behind Azure API Management, three published NuGet packages (`Api.Client.V1`, `Api.Client.V2`, `Api.Client.Testing`), and the Terraform that provisions APIM definitions, App Services, SQL DB, Key Vault, Storage, and App Insights.

Stack: .NET 9 (API hosts) + .NET 9 + .NET 10 multi-target (libraries, clients, tests). EF Core 9 reverse-engineered from the DACPAC via EF Core Power Tools. Versioning: Nerdbank.GitVersioning.

This file is the brief for the **GitHub Copilot coding agent** (and any other agent that follows the [agents.md](https://agents.md) convention) when it runs in a cloud runner without the local VS Code multi-root workspace context.

> If you are a human reading this in VS Code, prefer `.github/copilot-instructions.md` for project orientation. `AGENTS.md` is the agent execution brief.

---

## Required reading (read these BEFORE doing any work)

The `copilot-setup-steps.yml` workflow checks out `frasermolyneux/.github-copilot` at `./.github-copilot/` in the runner, so the paths below resolve.

1. `.github/copilot-instructions.md` — repo-specific orientation, build commands, conventions
2. `.github-copilot/.github/instructions/personal.working-preferences.instructions.md`
3. `.github-copilot/.github/copilot-instructions.md` — org-wide catalog
4. Stack-specific files — see **Stack guardrails** below
5. `docs/api-versioning.md`, `docs/api-design-v2.md`, `docs/efcore-data-lib.md`, `docs/testing.md` — repo-specific architecture

---

## Stack guardrails

### Tenant facts (always-on)
- `tenant.subscriptions`, `tenant.regions`, `tenant.identity`, `tenant.dns`, `tenant.network-topology`

### Enforceable standards
- `standards.oidc-and-secrets` — **no client secrets**
- `standards.dotnet-project` — project file / Directory.Build.props conventions
- `standards.azure-naming`, `standards.azure-tagging`, `standards.terraform-style`
- `standards.branching-and-prs`

### Patterns
- `patterns.api-client` — three-package layout (Abstractions / Client / Client.Testing)
- `patterns.versioned-apis` — namespaced controllers, APIM segment versioning, runtime OpenAPI
- `patterns.repository` — repository pattern for data access
- `patterns.nbgv-versioning` — NBGV / `version.json`
- `patterns.terraform-remote-state`
- `dotnet-nuget-library.instructions.md`, `dotnet-api-client-libraries.instructions.md`
- `datalib-regeneration.instructions.md` — **mandatory** EF Core Power Tools regeneration after any schema change (per-repo file at `.github/instructions/datalib-regeneration.instructions.md`, not under `.github-copilot/`)

### Platform consumption contracts
- `platform.workloads`, `platform.monitoring`, `platform.connectivity`

### Shared
- `shared.api-client-abstractions` — `MX.Api.Abstractions` base types
- `shared.observability-appinsights` — telemetry filtering, audit logger
- `shared.portal-core` — shared App Service Plans, App Insights, SQL Server consumed from `portal-core`

---

## Build, test, format

```pwsh
dotnet restore src/XtremeIdiots.Portal.Repository.sln
dotnet build src/XtremeIdiots.Portal.Repository.sln
dotnet test src/XtremeIdiots.Portal.Repository.sln --filter "FullyQualifiedName!~IntegrationTests"
dotnet format src/XtremeIdiots.Portal.Repository.sln --verify-no-changes

terraform -chdir=terraform fmt -check -recursive
terraform -chdir=terraform init -backend-config=backends/dev.backend.hcl
terraform -chdir=terraform validate
terraform -chdir=terraform plan -var-file=tfvars/dev.tfvars
```

---

## Do NOT

- ❌ Do not `git commit`, `git push`, force-push, rebase, or branch-mutate. Work on the assigned branch only.
- ❌ Do not introduce client secrets / connection strings. OIDC + managed identity only.
- ❌ Do not bypass `dotnet format --verify-no-changes`, `dotnet test`, `terraform fmt -check`, or `terraform validate`.
- ❌ Do not change DTOs / route shapes in `Abstractions.V1` or `Abstractions.V2` without bumping the matching API client NuGet version and updating consumers — these are published contracts.
- ❌ Do not edit `DataLib` by hand — regenerate via EF Core Power Tools after the Database project changes (see `datalib-regeneration.instructions.md`).
- ❌ Do not manage APIM API definitions in Terraform — they are imported by deploy workflows from the live App Service OpenAPI spec.
- ❌ Do not change `version.json`, `Directory.Build.props`, or `.github/workflows/` unless that is the explicit task.
- ❌ Do not add a `/api/` prefix to controller routes — routes are `v{version:apiVersion}/[controller]`. APIM owns the segment.
- ❌ Do not break the `ApiResponse` / `CollectionResult` envelope shape from `MX.Api.Abstractions`.

---

## Opening the PR

You MUST use `.github/PULL_REQUEST_TEMPLATE.md` as your PR body — do **not** write a freeform body. The org template is inherited from `frasermolyneux/.github` and GitHub pre-populates it when you open the PR. Concretely:

1. Fill `## Summary` (one line) and `Closes #<issue>`.
2. Tick the relevant `## Type of change` box.
3. Paste the **actual command output** from your Build, Tests, and Format check runs into `## Validation evidence`. Show the real summary line, not "tests passed".
4. Fill `## Risk and rollout` — blast radius, auto-deploy?, manual steps post-merge, rollback plan.
5. Tick **every** box in `## Agent attestation`.
6. Delete `## Consumer impact` only if no published contract (Abstractions / Client NuGet / Service Bus DTO / Terraform output) changed.

Complete the `## Agent attestation` section before requesting review; reviewers use it as a readiness checklist.

---

## Pre-PR checks (run before you open the PR)

- [ ] `dotnet build` succeeds (clean)
- [ ] `dotnet test ... --filter "FullyQualifiedName!~IntegrationTests"` passes
- [ ] `dotnet format ... --verify-no-changes` passes
- [ ] `terraform fmt -check -recursive` passes
- [ ] `terraform validate` + `terraform plan -var-file=tfvars/dev.tfvars` succeed and diff is intentional
- [ ] If Abstractions / Client DTOs changed, the matching V1/V2 package and consumer repos noted in PR body
- [ ] If schema changed, DataLib was regenerated via EF Core Power Tools
- [ ] No new secrets / GUIDs / connection strings
- [ ] PR body cites each acceptance criterion
- [ ] Risk/rollout section filled in

---

## Escalation

If you hit any of the conditions below, **open the PR as draft** and **apply the `needs-decision` label** instead of pushing forward to ready-for-review. Post a comment on the originating issue summarising what's blocking you and what decision is needed.

Stop and escalate when:

- A change requires a breaking V1 / V2 API contract change (also apply the `breaking-contract` label).
- The Database project requires a destructive schema migration (data loss risk).
- A `code-review` finding is **High** and cannot be resolved in-scope.
- APIM import would require manual product / policy coordination beyond the workflow.
