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

## Project Status

This project is currently in the **discovery and planning** phase.

The first step is to review the existing workflow with Project Hope, document how inventory is received, tracked, distributed, and reported, then use those findings to decide whether the right next step is an improved spreadsheet process, a lightweight database, a small web application, or another low-cost option.

## Discovery Documents

- [Executive Summary Cover Sheet](Docs/Executive%20Summary.md)
- [Executive Summary PDF](Docs/Executive%20Summary.pdf)
- [Questions to Ask](Docs/project_hope_food_bank_fillable_questions.pdf)
- [Current Inventory Spreadsheet Scan Notes](Docs/current-inventory-spreadsheet-scan.md)

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
