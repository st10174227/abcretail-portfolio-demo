# Publish the ABC Retail portfolio demo

The publication kit copies only the listed static source files into a new ABCRetail-Portfolio-Publish folder, applies this hosting profile and runs the Release build, all 23 regression groups and a Production container check before publication. It does not copy App_Data, generated builds, original Azure settings, uploads or an academic Git repository.

Repository target: st10174227/abcretail-portfolio-demo. The publisher verifies the active GitHub CLI account and exact personal origin, stages the checked source list only, uses a GitHub noreply author address and never force-pushes. A pre-existing unrelated repository is not overwritten. GitHub CLI handles repository creation because that operation is not exposed by the current connector.

## After the GitHub push

1. Wait for both OS check jobs and the production-container job to pass in GitHub Actions.
2. In Render choose New > Blueprint and connect st10174227/abcretail-portfolio-demo on main.
3. Name the Blueprint ABC Retail Demo. Keep render.yaml as the path. Review the one free web service, abcretail-demo-st10174227, before deploying.
4. Check that ASPNETCORE_ENVIRONMENT is Production, PORT and ASPNETCORE_HTTP_PORTS are 8080, HTTPS redirect port is 443 and the forwarded-header setting is enabled.
5. If Render assigns a different public hostname, update AllowedHosts to that exact hostname. Do not use a wildcard as a shortcut.
6. Wait for Live. Record the actual Render URL. Verify /health, desktop/mobile views, a complete order/file workflow and isolation in a private browser before calling hosting complete.

## Managed proxy scope

Render terminates public TLS before forwarding traffic to the container. The forwarded-header environment option is set only in the Render profile and private loopback production test, not in the Dockerfile or local launcher. This option accepts proxy forwarding headers; use it only where a trusted ingress controls those headers and direct untrusted access to the app port is prevented. It must be reviewed for a different hosting/network setup. The application retains Secure cookies and redirects unforwarded HTTP before forms are rendered.

## Demo lifecycle

This free service hosts disposable sample data, not real retail operations. Workspaces last up to 45 minutes, with at most 20 retained at once. Restarts/deployments clear in-memory records and files. Free Render instances may sleep after inactivity, delaying first access. No persistent disk or external database is required for this demonstration. Hosting-provider request logs are separate from temporary workspace data.

Do not publish LinkedIn or CV updates during this workflow; the owner is finishing the rest of the portfolio first. TableFind and UnityLync are outside the current demo work.

## Reference documentation

- https://render.com/docs/blueprint-spec
- https://render.com/docs/free
- https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/8/forwarded-headers-unknown-proxies
