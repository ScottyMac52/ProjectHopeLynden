# User Stories

These stories are the starting backlog for Option A: a local ASP.NET Core Razor Pages application with a local SQLite database and browser-based clients.

Each story should be implemented with a TDD approach. Acceptance criteria should be expressed as tests before implementation code is written.

## Story 1: View Inventory by Category

As a volunteer, I want to view inventory grouped by category so that I can quickly find the section I am working on.

### Acceptance Criteria

- Given inventory categories exist, when I open the inventory screen, then I can select or view categories.
- Given I choose a category, when the category view loads, then only inventory entries for that category are shown.
- Given a category has no inventory entries, when I view it, then the application shows an empty but understandable view.

### TDD Notes

- Test category retrieval and ordering.
- Test filtering inventory entries by category.
- Test empty category behavior.

## Story 2: Edit Current Quantity

As a volunteer, I want to edit the current quantity for an inventory entry so that the system reflects the latest physical count.

### Acceptance Criteria

- Given an inventory entry exists, when I update its quantity, then the new quantity is saved.
- Given the quantity is changed, when the row is displayed again, then the updated quantity is shown.
- Given an invalid quantity is entered, when I save, then the system rejects the value and keeps the prior valid value.
- Given a quantity is updated, then the last-updated date is refreshed.

### TDD Notes

- Test valid quantity updates.
- Test invalid quantities.
- Test last-updated behavior.
- Test persistence through SQLite integration tests.

## Story 3: Track Commodity and Non-Commodity Inventory Separately

As the Director, I want the same item to support both Commodity and non-Commodity inventory so that reporting remains accurate without hiding total available inventory.

### Acceptance Criteria

- Given the same item name exists in both Commodity and non-Commodity stock, when inventory is displayed, then both records can be represented separately.
- Given Commodity inventory is filtered, when the report runs, then only Commodity entries are included.
- Given total inventory is requested for an item, when both Commodity and non-Commodity entries exist, then both are counted in the operational total.

### TDD Notes

- Test Commodity status at the inventory-entry level.
- Test Commodity-only aggregation.
- Test total item aggregation across Commodity and non-Commodity entries.

## Story 4: Generate Commodity Report

As the Director, I want a Commodity report so that I can supply required Commodity information to the Bellingham Food Bank.

### Acceptance Criteria

- Given Commodity inventory exists, when I generate the Commodity report, then only Commodity inventory entries are included.
- Given non-Commodity inventory exists for the same item, when I generate the Commodity report, then non-Commodity quantities are excluded.
- Given the report is generated, then it includes item name, category, quantity, location, and date fields needed for discovery validation.

### TDD Notes

- Begin with a simple report model before final export format is known.
- Add tests around inclusion and exclusion rules.
- Keep report format flexible until the Director's required format is discovered.

## Story 5: Maintain Inventory Entries

As a volunteer, I want to add and edit inventory rows so that new items or corrected information can be captured without editing a spreadsheet file directly.

### Acceptance Criteria

- Given I add a new inventory entry with valid values, then it is saved and appears in the correct category.
- Given I edit location, BB date, Commodity status, or Menu Item status, then the saved row reflects the updated values.
- Given required fields are missing, then the system prevents saving and shows a simple error.

### TDD Notes

- Test validation rules.
- Test new row creation.
- Test editing fields independently.
- Keep `Menu Item` behavior flexible until discovery defines its meaning.

## Story 6: Search Inventory

As a volunteer, I want to search for an item by name so that I can find inventory quickly without knowing the category.

### Acceptance Criteria

- Given inventory entries exist, when I search by partial item name, then matching entries are shown.
- Given multiple categories contain matches, when results are shown, then category and location are visible.
- Given no items match, then a clear empty-result message is shown.

### TDD Notes

- Test case-insensitive matching.
- Test partial matching.
- Test no-result behavior.

## Story 7: Preserve Spreadsheet-Like Workflow

As a volunteer, I want the inventory screen to feel like the existing spreadsheet so that I can use the system with minimal training.

### Acceptance Criteria

- Given the inventory screen is open, then rows and columns resemble the current spreadsheet structure.
- Given I edit an inventory row, then the editing workflow is simple and obvious.
- Given changes are saved, then the screen confirms or visibly reflects the updated row.

### TDD Notes

- Avoid brittle pixel-perfect tests.
- Test page handlers and rendered field presence.
- Use lightweight HTML assertions for key columns and actions.

## Story 8: Backup the Local Database

As the organization, I want a simple database backup process so that inventory records can be recovered if the server machine fails or the data is damaged.

### Acceptance Criteria

- Given the server has a configured backup folder, when backup runs, then a copy of the database is created.
- Given multiple backups exist, then backup file names are unique and understandable.
- Given a backup fails, then the failure is visible in logs or a simple status view.

### TDD Notes

- Test backup file creation with a temporary database.
- Test naming rules.
- Test failure handling.

## Story 9: Capture Open Discovery Fields

As the project team, I want unknown spreadsheet fields tracked explicitly so that we do not make premature schema assumptions.

### Acceptance Criteria

- Given `BB` is not fully defined, then requirements identify it as an open discovery question.
- Given `Menu Item` is not yet understood, then the first version does not hard-code business behavior around it.
- Given color coding is not yet understood, then color meanings remain an open discovery item.

### TDD Notes

- This may begin as documentation only.
- Once behavior is defined, convert each discovered rule into tests.
