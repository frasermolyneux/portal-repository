---
description: Mandatory regeneration of DataLib via EF Core Power Tools after any database schema change.
applyTo: 'src/XtremeIdiots.Portal.Repository.Database/**/*.sql,src/XtremeIdiots.Portal.Repository.DataLib/**/*.cs'
---
# DataLib regeneration

`src/XtremeIdiots.Portal.Repository.DataLib` is **reverse-engineered** from the
SQL Database project (`XtremeIdiots.Portal.Repository.Database`) using
[EF Core Power Tools CLI](https://www.nuget.org/packages/ErikEJ.EFCorePowerTools.Cli/).
Hand-edits to the entity classes (`AdminAction.cs`, `BanFileMonitor.cs`,
`GameServer.cs`, `PortalDbContext.cs`, etc.) **will be wiped** the next time the
generator runs.

## When to regenerate

Regenerate DataLib **immediately** after **any** of the following:

- Adding, removing, or modifying columns in `src/XtremeIdiots.Portal.Repository.Database/dbo/Tables/*.sql`.
- Adding new tables (also requires adding them to `efcpt-config.json` — see below).
- Renaming columns or changing column types / nullability.
- Adding, removing, or renaming indexes or constraints.
- Adding, removing, or modifying any other database object referenced by the
  reverse engineering (e.g. computed columns, defaults, foreign keys).

Do **not** edit `src/XtremeIdiots.Portal.Repository.DataLib/*.cs` files
manually except for hand-written `*.Partial.cs` files that explicitly opt out
of regeneration (see the [Hand-written partials](#hand-written-partials)
section below).

## Procedure

Run from the repo root:

```pwsh
# 1. Install the tool once per machine (skip if already installed).
dotnet tool install ErikEJ.EFCorePowerTools.Cli -g --version 9.*

# 2. Build the Database project so its .dacpac is up to date.
dotnet build src/XtremeIdiots.Portal.Repository.Database

# 3. Regenerate DataLib from the freshly-built dacpac.
cd src/XtremeIdiots.Portal.Repository.DataLib
efcpt ..\XtremeIdiots.Portal.Repository.Database\bin\Debug\XtremeIdiots.Portal.Repository.Database.dacpac mssql -i ".\efcpt-config.json"

# 4. Build the full solution and run the unit tests.
cd ..\..
dotnet build src/XtremeIdiots.Portal.Repository.sln
dotnet test src/XtremeIdiots.Portal.Repository.sln --filter "FullyQualifiedName!~IntegrationTests"
```

Steps 2–4 are mandatory after every regeneration: do not commit DataLib
changes without confirming the full solution still builds and tests still
pass. The Database build must succeed before efcpt can read the dacpac.

## efcpt-config.json

`src/XtremeIdiots.Portal.Repository.DataLib/efcpt-config.json` is the source
of truth for which tables get reverse-engineered. **When you add a new table
to the Database project, also add it to the `tables` array in
`efcpt-config.json`** before running efcpt. Tables not listed there will not
be generated, even though the Database build will succeed.

Other config flags worth knowing:

- `use-data-annotations: true` — entities use attributes (`[Key]`, `[Index]`,
  `[StringLength]`, `[Precision]`, `[Column]`) rather than fluent config
  inside `OnModelCreating`. Reflect this when authoring SQL: use the right
  data types and DEFAULT clauses so the generator can emit the correct
  attributes.
- `use-inflector: true` — column suffixes like `ETag` are normalised to
  `Etag` on the C# property (the SQL column name is preserved via a
  `[Column("LastPushedETag")]` attribute). Mapping code that targets the
  entity must use the C# spelling (`entity.LastPushedEtag`), not the SQL
  spelling (`entity.LastPushedETag`).
- `use-nullable-reference-types: true` — `NOT NULL` string columns become
  `string Foo { get; set; } = null!;` in the entity. For columns with a SQL
  DEFAULT that satisfies a hard runtime requirement (e.g. EF Core In-Memory
  validation in unit tests), provide a C# default in a partial class — see
  below.

## Hand-written partials

The auto-generated entity classes are declared `partial`. To add C# defaults
or behaviour that the generator cannot express, add a `<Entity>.Partial.cs`
file alongside the generated `<Entity>.cs` in
`src/XtremeIdiots.Portal.Repository.DataLib`. Files matching the
`*.Partial.cs` naming pattern are preserved across regenerations.

Existing examples:

- `GameServer.Partial.cs` — initialises `BanFileRootPath = "/"` in the
  parameterless constructor so EF Core's in-memory provider does not reject
  test-constructed `GameServer` instances on the SQL DEFAULT.

When adding a new partial, document why it exists at the top of the file —
otherwise reviewers cannot tell whether it was a deliberate exception or a
forgotten regeneration.

## Anti-patterns

These are non-negotiable:

- ❌ Editing `BanFileMonitor.cs`, `GameServer.cs`, `PortalDbContext.cs` (or
  any other auto-generated file) by hand. Use `*.Partial.cs` instead.
- ❌ Committing schema changes to `src/.../Database/dbo/Tables/*.sql`
  without re-running efcpt and committing the regenerated DataLib in the
  same change.
- ❌ Adding a new table to the Database project without also adding it to
  `efcpt-config.json`.
- ❌ Skipping the build/test step after regeneration.
