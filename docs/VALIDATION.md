# Verified Windows validation — 20 November 2026

Evidence: owner-provided terminal output from the separate ABC Retail personal workspace.

- Release build: succeeded for CLDV_Final and ABCRetail.Checks.
- Regression groups: all 23 passed.
- Local application: started in Development at http://127.0.0.1:5097.
- Observed HTTP logs: overview, customer/product lists, contract/image lists, activity and bundled CSS/JavaScript returned 200.
- Antiforgery cache-header messages: no failed request was reported alongside these warnings.

The startup fix passes the temporary content root to CreateBuilder through its startup arguments instead of mutating WebApplicationBuilder.WebHost after creation.

## Passing groups

- Health checks and unmatched URLs do not allocate visitor workspaces
- Production HTTP redirects before rendering secure forms
- Fresh samples and every workspace page load without Azure settings
- Every mutating workflow requires antiforgery
- Valid customer creation persists only inside its browser workspace
- Invalid and duplicate customer forms show errors without creating records
- Editable customer content is HTML-encoded when displayed
- Customer editing detects stale versions and preserves the latest value
- Valid products save without an image and ignore forged storage fields
- Malformed, negative, oversized and overprecise prices do not persist
- Orders use saved identities and prices rather than submitted totals
- Invalid quantities, dates and unknown references preserve orders
- GET confirmation pages are read-only and linked records cannot be deleted
- Recorded order prices survive product changes and stale order edits fail
- Other browsers cannot read, change or delete private records
- Product images are stored privately and replacement removes old bytes
- Standalone image pages work and reject disguised or oversized files
- PDF contracts download as attachments and cannot cross browsers
- Queue processing changes the oldest pending event only
- Order removal releases linked records and removes product image bytes
- Reset clears one browser's records, files and events without changing another
- Workspace capacity and expiry are bounded without reusing old data
- Cumulative file quota rejects overflow without partial uploads

## Pending acceptance

Desktop overview and product captures plus the mobile order form were visually reviewed without clipping or overlapping controls. The owner then built the Linux Docker image successfully and recorded 200 responses for /, /Customer, /Product, /Order/AddOrder, /ABCRetail/ListContract, /ABCRetail/ListImages and /Demo at loopback port 5098.

Pending: expanded mobile navigation and other mobile/table views; complete manual browser form/file interaction; all 23 groups on Linux; production proxy/container and hosted verification. The publication preparation requires its production container checks to pass before GitHub publication; GitHub Actions subsequently runs the Windows/Linux suites and production-container job. Review their actual results for completion. No hosted URL was established by the local checks.

This record excludes session identifiers, key identifiers and personal machine paths from the supplied logs.
