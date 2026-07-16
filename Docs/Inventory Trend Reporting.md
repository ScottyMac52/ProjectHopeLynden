# Inventory Trend Reporting

The Inventory Trends page reports two separate datasets because an inventory level and an operational count event are not the same thing.

## End-of-day inventory

For each date on which the selected item or category has a recorded count, the report:

1. Finds the latest count at or before the end of that date for every matching inventory entry.
2. Uses only the final count when an entry was corrected more than once on the same date.
3. Carries an entry's latest known quantity forward until a newer count replaces it.
4. Applies item, category, location, and Commodity classification from the selected historical count.
5. Sums those latest entry quantities by the requested item or category grouping.

Dates caused only by activity in an unrelated item or category are not emitted. This avoids repeated, unchanged rows that do not represent activity for the selected dataset.

This dataset answers: **How much matching inventory was known to be available at the end of that date?**

## Imported starting counts

The spreadsheet values used to initialize the application are inventory baselines. They establish the first known quantity for an entry, but they do not prove that an operational increase or decrease occurred inside this application.

The seeder therefore stores one baseline count at the source row's recorded date. Earlier versions created a synthetic prior count exactly seven days earlier when a previous spreadsheet value was available. Startup seeding now recognizes and removes those exact synthetic pairs, leaving the imported current count as the baseline without manufacturing a movement date.

## Daily count activity

The activity dataset includes only records with a known previous quantity. Those records are created by operational quantity updates and show:

- the number of update events
- the combined net quantity change

Imported baseline counts have no known previous quantity and are excluded from activity. They remain part of end-of-day inventory snapshots.

This dataset answers: **What recorded inventory movement occurred on that date?**

Keeping activity separate prevents repeated corrections and imported starting values from being mistaken for additional food.

## Historical classification

Every new count-history row stores immutable snapshots of:

- item ID and name
- category ID and name
- location ID and name
- Commodity status

Later edits to an inventory entry therefore do not rewrite the classification of earlier count records.

The migration for this correction backfills existing history from each row's currently linked inventory entry. That is the best information available, but classifications that changed before this migration cannot be reconstructed because the earlier schema did not retain them.

## Filters

- Item-name input is trimmed and compared without regard to capitalization.
- Category filtering uses the category stored with the historical count.
- Commodity filtering uses the Commodity status stored with the historical count.
- Snapshot filtering is applied after selecting each entry's latest record for the date, so an entry that later changes classification does not remain in the old classification indefinitely.
