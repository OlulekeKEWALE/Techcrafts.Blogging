# TECHCRAFTS-BLOG-ADMIN-003 — Technical Findings & Defect Remediations

During the TECHCRAFTS-BLOG-ADMIN-003 execution pass, five notable technical defects and edge cases were identified, root-caused, and completely remediated.

---

## Finding 1: EasyMDE Hidden Textarea Blocking Native Chromium Form Submission
- **Severity:** High (Functional blocker in real browsers)
- **Root Cause:** In `Create.cshtml` and `Edit.cshtml`, the `<textarea id="markdown-editor" ... required>` had the HTML5 `required` attribute. EasyMDE's initialization hid the textarea (`display: none;`) and rendered a CodeMirror iframe editor in its place. In real Chromium, clicking the form submit button caused the browser's native constraint validation to abort submission with the console error:
  `"An invalid form control with name='content' is not focusable."`
- **Remediation:** Removed the native `required` attribute from the hidden textarea, hooked EasyMDE's change event to keep the textarea value synchronized (`easyMDE.codemirror.on('change', () => textarea.value = easyMDE.value());`), and relied on ASP.NET Core server-side ModelState validation for required content enforcement.

---

## Finding 2: SetupMiddleware Latching Premature Failure on Startup Check
- **Severity:** High (Operational & testing failure)
- **Root Cause:** In `Techcrafts.Blogging.Web/Middlewares/SetupMiddleware.cs`, the middleware declared static booleans `_isDatabaseConfigured` and `_hasCheckedDatabase`. During initial startup under heavy load or cold start, if the first connection check threw an exception or was cancelled, `_hasCheckedDatabase` was latched to `true`, permanently locking the entire application into redirecting every request to `/Setup`.
- **Remediation:** Updated `SetupMiddleware.InvokeAsync` so `_hasCheckedDatabase = true` is only assigned when `CanConnectAsync` returns `true`. If an exception occurs during startup, the check is retried on subsequent requests rather than permanently locking into `/Setup`.

---

## Finding 3: HttpClient Cold-Start Timeout During Server Ready Check
- **Severity:** Medium (Test runner flakiness)
- **Root Cause:** In `PlaywrightBrowserE2ETests.cs`, `WaitForServerReadyAsync` initialized an `HttpClient` with a 2-second timeout. Because EF Core model initialization and decimal precision warnings on cold start took ~7 seconds, the client aborted the connection, triggering a `TaskCanceledException` inside `CanConnectAsync`.
- **Remediation:** Increased `client.Timeout` to 15 seconds and total readiness deadline to 45 seconds, providing ample overhead for cold-start JIT and EF Core model compilation.

---

## Finding 4: Concurrency Conflict Resolution Overwrite Flow Requiring Refreshed Token
- **Severity:** High (Concurrency specification compliance)
- **Root Cause:** When an administrator encounters an optimistic concurrency conflict, selecting "Overwrite With My Version" must submit the newly observed concurrency token so that the overwrite operation succeeds against the newly incremented rowversion in SQL Server.
- **Remediation:** Updated `UpdatePostCommandHandler` to return `NewRowVersion` as part of `UpdatePostResult.ConcurrencyConflict`. In `Conflict.cshtml`, the "Overwrite With My Version" form carries `Model.NewRowVersion` in a hidden field, ensuring the overwrite transaction is accepted by EF Core.

---

## Finding 5: Test Assembly Cross-Talk on Shared SQL Server Database
- **Severity:** Medium (CI/Test stability)
- **Root Cause:** xUnit runs test classes in parallel by default. When real-browser Playwright tests, EF Core WebApplicationFactory security tests, and SQL Server cascade integration tests ran simultaneously against the local test database `Techcrafts_Certification_Test`, concurrent inserts and deletes caused foreign key and count race conditions.
- **Remediation:** Added `[assembly: CollectionBehavior(DisableTestParallelization = true)]` to `Techcrafts.Blogging.Tests/EntityTestExtensions.cs`, ensuring tests execute sequentially and deterministically.
