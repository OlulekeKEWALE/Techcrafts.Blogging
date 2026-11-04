# TECHCRAFTS-BLOG-ADMIN-003 — Playwright Real-Browser Test Environment

## 1. Test Harness Architecture

To fulfill **Gate G11** with authentic, independently reproducible evidence, the test suite was constructed using Microsoft Playwright for .NET running real Chromium browser processes against an out-of-process Kestrel web host.

### 1.1 Technology Stack & Versions
- **Test Automation Framework:** Microsoft.Playwright 1.64.1
- **Browser Engine:** Chromium (bundled with Playwright)
- **Viewport Specification:** 1280 x 800 desktop
- **Host Server:** Kestrel on Microsoft .NET 10.0
- **Listen Address:** `http://127.0.0.1:5189`
- **Database Engine:** Microsoft SQL Server 2022 on localhost (`Techcrafts_Certification_Test`)

---

## 2. Server Fixture Lifecycle (`PlaywrightServerFixture`)

The test suite leverages `PlaywrightServerFixture` (`IAsyncLifetime`) to govern process lifecycle and test setup:

1. **Database Seeding:**
   - Verifies the SQL Server schema exists.
   - Inserts or verifies the primary author and administrator account (`admin@techcrafts.com` / `Password123!`) in `AspNetUsers`, `AspNetUserRoles`, and `Authors`.
2. **Kestrel Out-Of-Process Launch:**
   - Starts `dotnet run --no-build` in `Techcrafts.Blogging.Web`.
   - Injects environment variables:
     - `ASPNETCORE_URLS=http://127.0.0.1:5189`
     - `ASPNETCORE_ENVIRONMENT=Testing`
     - `ConnectionStrings__DefaultConnection=Server=localhost;Database=Techcrafts_Certification_Test;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true`
     - `Telemetry__Enabled=false`
3. **Health Check Polling (`WaitForServerReadyAsync`):**
   - Polls `http://127.0.0.1:5189/` with an HTTP client until HTTP 200/302 is received.
4. **Playwright Chromium Launch:**
   - Calls `Playwright.CreateAsync()`.
   - Launches Chromium in headless mode:
     ```csharp
     Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
     {
         Headless = true
     });
     ```
5. **Real-Browser Form Authentication:**
   - Contexts are generated via `CreateAdminContextAsync()`.
   - Navigates to `/account/login?ReturnUrl=%2Fadmin%2Fposts`.
   - Types credentials into `#login-email` and `#login-password`.
   - Clicks `#btn-login-submit` and awaits ASP.NET Core Identity authentication cookies to be set in the browser's storage.
6. **Teardown:**
   - Closes browser contexts and browser instance.
   - Gracefully terminates Kestrel process and associated process trees.

---

## 3. Artifact Storage

Full-resolution screenshots are automatically captured and saved to:
`E:\TechCrafts\Open Sources\Blogging\artifacts\playwright\screenshots\`
