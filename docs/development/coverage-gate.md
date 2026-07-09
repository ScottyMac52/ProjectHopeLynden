# Coverage Gate

ProjectHope pull requests use a dedicated coverage gate in addition to the shared CI test workflow.

The gate requires:

- 80% line coverage
- 80% branch coverage

Generated and host-bootstrap files are excluded from the gate so the reported percentage measures hand-written application behavior instead of EF migration scaffolding or ASP.NET generated output.

Excluded paths:

- `Persistence/Migrations/*.cs`
- `Program.cs`
- `Startup/DatabaseInitializationExtensions.cs`
- generated Razor `Pages/*.cshtml` output

The gate runs with `coverlet.runsettings`, merges Cobertura output with ReportGenerator, and fails the pull request when the merged totals are below the threshold.
