# Milestone 1 Candidate Stories

This document identifies the first stories that should become GitHub issues when implementation begins.

## Candidate Order

1. View Inventory by Category
2. Track Commodity and Non-Commodity Inventory Separately
3. Edit Current Quantity
4. Maintain Inventory Entries
5. Generate Commodity Report
6. Search Inventory
7. Preserve Spreadsheet-Like Workflow
8. Backup the Local Database
9. Capture Open Discovery Fields

## Recommended First Implementation Slice

The first implementation slice should prove the architecture with the smallest useful workflow:

1. Create the ASP.NET Core Razor Pages application shell.
2. Create the initial SQLite schema for categories, items, locations, and inventory entries.
3. Seed a small set of test categories and entries.
4. Display inventory by category.
5. Edit current quantity for one inventory entry.
6. Prove that the same item can exist as both Commodity and non-Commodity inventory.

## TDD Rule

Each story should start with failing tests that express the acceptance criteria before production behavior is added.
