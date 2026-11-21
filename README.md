# ABC Retail — personal interactive demo

Mohammed Ba Yazed's separate personal continuation of the supplied CLDV_Final web project. Visitors can manage fictional customers, products and orders, upload sample images and PDF contracts, and process a local activity queue. No account, Azure credentials, email, payment or delivery service is used.

## Live demo

[Try ABC Retail](https://abcretail-demo-st10174227.onrender.com/) — no account or password required. Use fictional details and sample files; each browser receives a temporary workspace.

## Status

Verified on 21 November 2026: all 23 regression groups passed on Windows and Linux, and the Production container checks passed in [GitHub Actions](https://github.com/st10174227/abcretail-portfolio-demo/actions/runs/44906303202). The desktop overview/product views and mobile order form passed visual review.

The hosted browser check created a fictional customer and product, recorded an order for 3 × R25.50 with the correct R76.50 total, processed the oldest queued activity first, and confirmed the public health endpoint reports ok. Hosted file upload/download, separate-browser isolation and the remaining mobile views were not exercised in that live browser check; file and isolation behaviour are covered by the automated suites. This public demonstration uses disposable sample data and performs no real retail transactions. See docs/VALIDATION.md for the earlier validation record.

## Start in PowerShell

Requires the .NET 10 SDK. The personal copy targets .NET 10 to match the owner's installed SDK and runtime. The original .NET 8 source remains unchanged. The application has no Azure or other external NuGet dependencies; the check project uses Microsoft.AspNetCore.TestHost 10.0.12.

```powershell
powershell -NoProfile -File .\scripts\Check-PersonalWorkspace.ps1
if ($LASTEXITCODE -ne 0) { throw 'Checks failed.' }
powershell -NoProfile -File .\scripts\Start-PersonalWorkspace.ps1
```

Open http://127.0.0.1:5097. Start by adding a customer and product, then create an order. The server takes customer/product identities and unit price from saved records; posted price and total fields cannot override them. Existing orders retain their recorded price when the catalogue changes. A product change on an order uses the newly selected product's saved price.

Customers/products referenced by orders cannot be deleted until those orders are removed. Edit/delete forms carry record versions so older tabs cannot silently overwrite newer changes. Images can be attached on the product form or uploaded separately to the gallery. Deleting a gallery image clears its current product association. Use samples/Sample-Contract.pdf to try the PDF workflow.

## Isolation and limits

Each browser cookie jar gets independent server-memory records and uploaded bytes. Workspaces expire after 45 minutes, with at most 20 retained workspaces. Expired workspaces are inaccessible and reclaimed on a later workspace request; restarts clear all records/files. No visitor database is created. Session and antiforgery keys are persisted outside public assets under CLDV_Final/App_Data/PersonalDemo/Keys and protected with Windows DPAPI on Windows. Cookies are separate from the owner's other projects.

Limits per workspace: 50 customers, 50 products, 100 orders, 20 files, 6 MB total file bytes, 2 MB per file. Names, descriptions, email, prices, quantities and order dates are validated. The activity log holds the latest 60 entries, evicting the oldest even if still pending. Process-next marks the oldest retained pending event as processed. This is an explicit bounded FIFO simulation, not Azure Queue Storage or a durable worker.

Upload validation checks PNG/JPEG/WebP signatures or a PDF header/end marker. It is not a full image decoder, PDF sanitizer or malware scanner. PDFs download as attachments. Files cannot be retrieved or deleted from a different workspace. Use fictional data and non-confidential sample uploads only.

## Browser review

Check desktop and phone layouts, collapsed navigation, all forms, image previews, PDF download, delete confirmations, activity processing and reset. Two private/separate browser sessions must not share changes. Local Bootstrap, JavaScript and system fonts avoid a dependency on external asset CDNs. The university workflow is preserved, but UI text clearly identifies all business actions as demonstrations.

## Container preparation

After local browser acceptance:

```powershell
docker build --file Dockerfile.demo --tag abcretail-demo:local .
if ($LASTEXITCODE -ne 0) { throw 'Docker build failed.' }
docker run --rm --name abcretail-demo-local -p 127.0.0.1:5098:8080 -e ASPNETCORE_ENVIRONMENT=Development abcretail-demo:local
```

Development is only for this local HTTP check. The hosted service uses Production, the exact AllowedHosts domain and managed-proxy forwarding configured in render.yaml. Review the proxy assumptions in docs/DEPLOY-DEMO.md before using a different hosting setup.

## Provenance

See PERSONAL_PROJECT_ORIGIN.md and docs/SOURCE-MANIFEST.json. Original findings: product/order add actions reverse ModelState checks; image pages have no matching upload/list actions; mutations lack explicit antiforgery; Azure clients are constructed directly from omitted configuration; file uploads trust submitted filenames and types; models have little validation. The personal implementation fixes these workflows without connecting to the original Azure account.

## Personal publication and hosting

Source repository: st10174227/abcretail-portfolio-demo. The live demo is deployed on Render. See docs/DEPLOY-DEMO.md for source preparation, Production container checks and Blueprint settings. Automatic deployment updates wait for CI checks to pass.
