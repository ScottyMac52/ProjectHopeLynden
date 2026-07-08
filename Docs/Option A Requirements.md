# Option A Requirements

## Selected Architecture

Option A is a local web application installed on one designated Windows machine at Project Hope. Client machines access the application through a browser on the local network.

```text
Client browser
  -> ASP.NET Core Razor Pages application
  -> SQLite database stored locally on the server machine
```

## Purpose

The purpose of the system is to replace or improve the current spreadsheet-based inventory process without making the workflow difficult for staff or volunteers. The application should feel familiar to people who already use the spreadsheet while improving data accuracy, reporting, backup, and long-term maintainability.

## Primary Goals

- Preserve the existing spreadsheet mental model.
- Keep the user interface dirt simple.
- Avoid login, authentication, authorization, or role management for the first version.
- Allow inventory items and current counts to be edited directly from a spreadsheet-like screen.
- Support Commodity and non-Commodity inventory for the same item name.
- Support required Commodity reporting for the Director.
- Keep deployment simple and affordable.
- Use a TDD-driven implementation style so assumptions are captured as executable tests.

## Non-Goals for the First Version

- Public internet hosting.
- Cloud dependency.
- Complex user accounts or permissions.
- Mobile-first workflow.
- Barcode scanning.
- Multi-site inventory.
- Advanced forecasting or purchasing automation.
- Replacing discovery with premature schema decisions.

## Users and Access Model

The first version assumes trusted local users inside the food bank environment. Anyone with access to the local application URL can use the system.

The application should not require login screens, passwords, user roles, or permission management. This keeps the system simple and avoids unnecessary support burden.

## Deployment Requirements

### Server Install

The server install should:

- Install the ASP.NET Core application on one designated Windows machine.
- Store the SQLite database on the local disk of that machine.
- Configure the application to run automatically when the machine starts.
- Provide a predictable local URL for staff and volunteers.
- Provide a documented backup location.
- Avoid direct client access to the SQLite database file.

### Client Access

Client machines should access the application through a browser. A separate client installer should not be required for Option A.

## User Interface Requirements

The user interface should look and behave like a simple spreadsheet:

- Category-based inventory views.
- Rows for inventory entries.
- Columns for item, quantity, location, BB date, Commodity status, Menu Item status, one-week change, and last-updated date.
- Simple inline editing or row editing.
- Clear save behavior.
- Minimal navigation.
- Large readable controls.
- No technical terminology unless it already exists in the current workflow.

## Data Requirements

The current spreadsheet suggests the following core data concepts.

### Category

Represents inventory groupings such as Dry Beans, Snacks, Cereals, Canned Meat, Tomatoes, and Diapers.

### Item

Represents the named food or supply item. Item names alone do not determine Commodity status.

### Inventory Entry or Lot

Represents a countable inventory record for an item. Commodity status must be stored here or at a similar stock-entry level because the same item name can exist as both Commodity and non-Commodity inventory.

Likely fields:

- Item
- Category
- Quantity
- Location
- BB date
- Commodity status
- Menu Item status
- Last updated date
- One-week change or history-derived change

### Location

Represents where inventory is stored. The spreadsheet abbreviation `LOC.` means location.

### Commodity Status

The spreadsheet abbreviation `COM.` means the item is a Commodity item. Commodity items have special reporting requirements. The Director must provide Commodity reporting to the Bellingham Food Bank.

Commodity status cannot be modeled only on the item name because the same item can appear as both Commodity and non-Commodity inventory.

### BB Date

`BB` is a date field. The exact business meaning still needs to be confirmed during discovery.

### Menu Item

The meaning of `Menu Item` is not yet known and must remain an open discovery question before final schema decisions are made.

### Current Counts

The penciled-in numbers on the scanned spreadsheet represent the current inventory numbers.

## Reporting Requirements

The first reporting requirement is Commodity reporting. The system should support reports that allow the Director to supply required Commodity information to the Bellingham Food Bank.

The first version should also support simple operational inventory views, including total quantity by item and category. Reports should be exportable or printable if practical.

## Backup Requirements

The database must be easy to back up and restore. The first version should include a simple documented backup strategy, such as scheduled copies of the SQLite database to a known backup folder.

Backup behavior should be designed before the system is used for production inventory.

## Testing and TDD Requirements

Development should follow a TDD approach. Each user story should include acceptance criteria before implementation begins.

Recommended test layers:

- Domain unit tests for inventory rules and Commodity handling.
- Application/service tests for use cases such as editing counts and generating reports.
- SQLite integration tests for persistence behavior.
- Razor Page handler tests for page-level behavior.
- Minimal browser or HTML contract tests only where they protect important workflow assumptions.

Coverage should be tight enough to validate developer assumptions. Domain and application logic should have the highest coverage expectations. UI rendering tests should focus on critical behavior rather than brittle visual details.

## Open Discovery Questions

- What does `BB` mean in Project Hope's workflow?
- What does `Menu Item` mean?
- What does each color code mean?
- What exact Commodity report format does the Director provide to the Bellingham Food Bank?
- How many people may update inventory at the same time?
- Which machine should host the server install?
- What backup location is available?
- Should historical count changes be stored explicitly, or is current quantity enough for the first milestone?
