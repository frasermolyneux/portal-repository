# EF Core Power Tools

This guide explains how to use [EF Core Power Tools CLI](https://www.nuget.org/packages/ErikEJ.EFCorePowerTools.Cli/#readme-body-tab) to reverse engineer a database into the `data-lib` project.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) installed
- The `ErikEJ.EFCorePowerTools.Cli` tool installed globally

## Installation

Install the EF Core Power Tools CLI globally:

```pwsh
dotnet tool install ErikEJ.EFCorePowerTools.Cli -g --version 9.*
```

## Usage

1. **Build the Database Project**

   Ensure the database project is built so the `.dacpac` file exists. From the `portal-repository` root:

   ```pwsh
   dotnet build src/database
   ```

2. **Navigate to the Data Library Folder**

   Change directory to the `data-lib` project:

   ```pwsh
   cd C:\Git\gh-frasermolyneux\portal-repository\src\data-lib
   ```

3. **Run EF Core Power Tools**

   Use the following command to reverse engineer the database from the `.dacpac` file:

   ```pwsh
   efcpt ..\database\bin\Debug\XtremeIdiots.Portal.Repository.Database.dacpac mssql -i ".\efcpt-config.json"
   ```

   - Adjust the path to the `.dacpac` if your build configuration or output path differs.
   - The `efcpt-config.json` file should be configured for your project’s needs.

## Notes

- Make sure to review and commit any generated or updated files as appropriate.
- For more options and advanced usage, see the