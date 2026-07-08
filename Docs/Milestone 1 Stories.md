# Milestone 1 Candidate Stories

This document identifies the first stories that should become GitHub issues when implementation begins.

## Candidate Order

1. View Inventory by Category
2. Track Commodity and Non-Commodity Inventory Separately
3. Edit Current Quantity
4. Store Historical Inventory Counts
5. Maintain Inventory Entries
6. Generate Commodity Report
7. Generate Inventory Trend Reports
8. Search Inventory
9. Preserve Spreadsheet-Like Workflow
10. Backup the Local Database
11. Capture Open Discovery Fields

## Recommended First Implementation Slice

The first implementation slice should prove the architecture with the smallest useful workflow:

1. Create the ASP.NET Core Razor Pages application shell.
2. Create the initial SQLite schema for categories, items, locations, inventory entries, and inventory count history.
3. Seed a small set of test categories and entries.
4. Display inventory by category.
5. Edit current quantity for one inventory entry.
6. Store a historical count record when quantity changes.
7. Prove that the same item can exist as both Commodity and non-Commodity inventory.
8. Prove that Commodity and non-Commodity history remains separate for the same item.

## TDD Rule

Each story should start with failing tests that express the acceptance criteria before production behavior is added.
