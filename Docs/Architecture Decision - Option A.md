# Architecture Decision: Option A

## Decision

Use a local ASP.NET Core Razor Pages web application with SQLite. The application will be installed on one designated Windows machine at Project Hope and accessed by client machines through a browser on the local network.

## Rationale

This option best matches the discovery goals:

- Dirt-simple browser UI.
- No client installer.
- No login, authentication, authorization, or role management in the first version.
- Local data ownership.
- Low recurring cost.
- Server-rendered HTML that can look and behave like the current spreadsheet.
- A single server process owns database access, avoiding multiple clients opening the database file directly.

## Technology Direction

- .NET 10
- ASP.NET Core Razor Pages
- Server-rendered HTML
- SQLite local database
- Optional htmx for small inline-editing interactions
- Automated tests around domain rules, application services, persistence, and page handlers

## Alternatives Considered

### Separate .NET Desktop Client and Local Server

This remains viable but requires client installation and updates on each workstation.

### Node.js Server and .NET Desktop Client

This is technically possible but introduces two implementation stacks and more support complexity.

### Direct Spreadsheet Replacement Only

An improved spreadsheet may be useful during transition, but it does not solve reporting, validation, backup, and concurrent update concerns as cleanly as a small local web application.
