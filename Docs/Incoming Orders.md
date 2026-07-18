# Incoming Orders

The **Incoming Orders** tab tracks food that is expected but has not yet been added to the on-hand inventory count.

## Scheduling an incoming order

1. Open **Incoming Orders** from the main navigation.
2. Select the exact inventory row that should receive the order.
3. Enter the incoming quantity and expected date.
4. Optionally enter a source or supplier and an order reference.
5. Select **Schedule incoming order**.

The inventory-row selection includes item, category, location, and Commodity status. This keeps Commodity and non-Commodity inventory separate even when the item name is the same.

The budget column from the original food-ordering spreadsheet is intentionally outside the first implementation.

## Inventory spreadsheet

Scheduled orders appear on the main inventory spreadsheet in two columns:

- **Incoming**: the total quantity from all scheduled order lines for that inventory row.
- **Expected**: the earliest scheduled arrival date for that inventory row.

Received and cancelled order lines are excluded from those columns.

## Automatic receiving

When an order reaches its expected date, the application:

1. Adds the incoming quantity to the selected inventory row.
2. Updates the inventory row's last-updated timestamp.
3. Creates a normal inventory history record showing the previous quantity, new quantity, and increase.
4. Marks the incoming order as **Received**.

The Windows service checks for due orders every 15 minutes. Application startup also processes overdue orders, so an order due while the server was shut down is received after the server starts again.

Processing is idempotent: a received or cancelled order cannot be applied to inventory a second time.

The expected date uses the Windows server's local calendar date. The recorded inventory timestamp is stored in UTC, consistent with the rest of the application.

## Changing an order

A scheduled order can be edited before receipt. Staff can change:

- inventory destination
- incoming quantity
- expected date
- source or supplier
- reference

A scheduled order can also be:

- **Received now** when it arrives early.
- **Cancelled** when it will not arrive.

Received and cancelled orders remain visible in recent activity for accountability, but they can no longer be changed.

## Operational meaning

Automatic receiving follows the requested workflow: the expected date is treated as the receipt date. Staff should reschedule or cancel an order before that date when a delivery is delayed or cancelled.

This feature does not import the attached Excel workbook. It implements the ordering workflow inside the Project Hope application so future orders are managed in the same SQLite database as inventory and history.
