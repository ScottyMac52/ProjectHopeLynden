# Inventory Trend Reporting

The Inventory Trends page reports two separate datasets because an inventory level and a count event are not the same thing.

## End-of-day inventory

For each date represented in count history, the report:

1. Finds the latest count at or before the end of that date for every inventory entry.
2. Uses only the final count when an entry was corrected more than once on the same date.
3. Carries an entry's latest known quantity forward until a newer count replaces it.
4. Applies item, category, location, and Commodity classification from the selected historical count.
5. Sums those latest entry quantities by the requested item or category grouping.

This dataset answers: **How much matching inventory was known to be available at the end of that date?**

## Daily count activity

For each date, the report separately groups the count-history events that were actually recorded that day. It shows:

- the number of count events
- the combined quantity change when every event has a known previous quantity
- `Unknown` movement when one or more included events are baseline records without a known previous quantity

This dataset answers: **What counting activity and recorded movement occurred on that date?**

Keeping activity separate prevents repeated corrections from being added together and mistaken for additional food.

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
