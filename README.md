# Project Hope Lynden

## Inventory Management System

This repository supports a volunteer effort to help **Project Hope Food Bank of Lynden** evaluate and improve its inventory management process.

The current system is based on Excel spreadsheets. The goal is not to replace that process blindly, but to understand how the food bank currently works, identify the highest-value pain points, and propose a practical system that can be maintained with minimal cost.

## Project Goals

- Reduce manual inventory tracking effort.
- Improve accuracy and visibility of available food bank inventory.
- Keep the solution affordable for a small community organization.
- Prefer open-source software and low-cost storage where practical.
- Design for non-developer users and long-term maintainability.
- Preserve useful parts of the existing workflow instead of forcing unnecessary change.

## Current Architecture

- Excel-based inventory tracking.
- Additional architecture decisions are pending discovery.

## Source Layout

The application uses a conventional `src` and `tests` layout:

- `src/ProjectHopeLynden.Domain` - core business concepts and rules.
- `src/ProjectHopeLynden.Application` - application use cases and workflow orchestration.
- `src/ProjectHopeLynden.Infrastructure` - persistence and external implementation details.
- `src/ProjectHopeLynden.Web` - ASP.NET Core Razor Pages web application.
- `tests/ProjectHopeLynden.Domain.Tests` - domain tests.
- `tests/ProjectHopeLynden.Application.Tests` - application tests.
- `tests/ProjectHopeLynden.Infrastructure.Tests` - infrastructure tests.
- `tests/ProjectHopeLynden.Web.Tests` - web and Razor Page tests.

## Development Commands

```bash
dotnet restore ProjectHopeLynden.sln
dotnet build ProjectHopeLynden.sln
dotnet test ProjectHopeLynden.sln
dotnet run --project src/ProjectHopeLynden.Web/ProjectHopeLynden.Web.csproj
```

## Project Status

This project is currently in the **discovery and planning** phase.

The first step is to review the existing workflow with Project Hope, document how inventory is received, tracked, distributed, and reported, then use those findings to decide whether the right next step is an improved spreadsheet process, a lightweight database, a small web application, or another low-cost option.

## Discovery Documents

- [Executive Summary Cover Sheet](Docs/Executive%20Summary.md)
- [Executive Summary PDF](Docs/Executive%20Summary.pdf)
- [Questions to Ask](Docs/project_hope_food_bank_fillable_questions.pdf)
- [Current Spreadsheet Scan](Discovery/Spreadsheet%20Contents.pdf)
- [Option A Requirements](Docs/Option%20A%20Requirements.md)
- [Option A Architecture Decision](Docs/Architecture%20Decision%20-%20Option%20A.md)
- [Initial User Stories](Docs/User%20Stories.md)
- [Milestone 1 Candidate Stories](Docs/Milestone%201%20Stories.md)

## Discovery Findings from Current Spreadsheet Scan

The scanned spreadsheet pages show that Project Hope already has a practical inventory model in place. The current workbook is not just a list of food items; it tracks categories, item names, quantities, locations, best-by dates, commodity flags, menu item flags, weekly changes, and last-updated dates.

### Inventory Categories Observed

The scan currently shows these inventory categories:

- Dry Beans
- Noodles
- Dry Mix
- Condiments
- Snacks
- Cereals
- Produce
- Eggs
- Frozen Meat
- Frozen Miscellaneous
- Canned Vegetables
- Canned Fruit
- Soup is a MESS
- Canned Beans
- Tomatoes
- Canned Meat
- Diapers
- Wipes
- Formula

### Data Fields Observed

The repeated worksheet structure suggests the current spreadsheet is acting as the first version of the data model. Common fields include:

- Item
- Quantity
- Location
- Best-by date
- Commodity indicator
- Menu item indicator
- One-week change
- Last updated date

### Spreadsheet Abbreviations and Notes

- `LOC.` means inventory location.
- `BB` is a date field. The exact business meaning still needs to be confirmed during discovery.
- `COM.` means the item is a Commodity item with special reporting requirements. The Director must supply Commodity reporting to the Bellingham Food Bank.
- `Menu Item` is not yet understood and needs to be clarified during discovery.
- Penciled-in quantities represent the current inventory numbers.
- Some of the same food items appear as both Commodity and non-Commodity inventory. The future database must account for this distinction instead of assuming an item name is globally one or the other.

### Initial Database Implications

- Commodity status should likely be tracked at the inventory lot, stock entry, or item-instance level rather than only at the item-name level.
- The system needs to support reporting by Commodity status without losing the ability to show total available inventory for the same item.
- Item identity, category, location, Commodity status, date tracking, and current count should be modeled separately enough to support accurate reporting and operational use.
- Unknown fields such as `Menu Item` should remain part of discovery before the schema is finalized.

### Initial Observations

- Existing inventory categories should be preserved as a starting point instead of replaced blindly.
- Category names and item lists should become configurable so Project Hope can adjust them without software changes.
- Handwritten notes and corrections suggest that inventory updates may happen away from the spreadsheet and then get reconciled later.
- Color coding appears to carry operational meaning and should be documented before any replacement system is designed.
- The current spreadsheet provides a strong requirements artifact for designing an improved workflow.

## Initial Areas to Review

- How food donations and purchases are received.
- How inventory quantities are updated.
- How expiration dates, categories, and storage locations are tracked.
- How inventory leaves the food bank.
- What reports are needed for staff, volunteers, donors, grants, or compliance.
- How many people need to use the system at the same time.
- What computers, tablets, network access, and printers are available.
- What data must be protected or backed up.

## Design Principles

- Start simple.
- Minimize recurring costs.
- Avoid vendor lock-in where possible.
- Make backup and recovery part of the design from the beginning.
- Prefer solutions that Project Hope can operate without depending on one volunteer forever.
- Use incremental improvements so the organization gets value early.

## Proposed Next Steps

1. Complete the discovery meeting and capture answers to the prepared questions.
2. Document the current workflow from intake through distribution.
3. Identify the highest-risk and highest-effort parts of the current Excel process.
4. Decide on a minimal first milestone.
5. Create issues for the first implementation tasks.

## Repository Workflow

All changes should be made on feature or documentation branches and submitted through pull requests for review before merging to `main`.
