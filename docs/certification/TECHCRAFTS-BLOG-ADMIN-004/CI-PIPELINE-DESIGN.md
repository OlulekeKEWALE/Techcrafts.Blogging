# TECHCRAFTS-BLOG-ADMIN-004 — GitHub Actions CI Pipeline Architecture

## 1. Pipeline Topology & Design Principles

The automated continuous integration pipeline was created at `.github/workflows/ci.yml` following least-privilege, containerized isolation, and genuine environment verification:

- **Trigger Scope:**
  - Pushes to `master` and `feature/**` branches.
  - Pull requests targeting `master`.
  - Manual triggers via `workflow_dispatch`.
- **Concurrency Management:**
  - `concurrency.group: ci-${{ github.workflow }}-${{ github.ref }}` with `cancel-in-progress: true` for pull requests to eliminate redundant runs while safeguarding master.
- **Permissions:**
  - `permissions.contents: read` strictly enforced; no elevated tokens or repository write access granted.
- **Runner Environment:**
  - `ubuntu-latest` with native Docker engine.

---

## 2. Integrated Services: SQL Server 2022 Container

Rather than using SQLite or in-memory providers that fail to test native SQL Server behavior, the workflow runs a genuine Microsoft SQL Server 2022 service container:

```yaml
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          ACCEPT_EULA: "Y"
          MSSQL_SA_PASSWORD: "Password123!"
        ports:
          - 1433:1433
```

A readiness check step polls port 1433 with netcat (`nc -z 127.0.0.1 1433`) in a bounded retry loop before test execution begins.

---

## 3. Real-Browser Playwright Provisioning

1. `actions/setup-dotnet@v4` installs .NET 10.0.x.
2. Solution is compiled in `Release` configuration.
3. Playwright Chromium is provisioned along with all necessary Linux system libraries:
   ```yaml
   - name: Install Playwright Chromium Dependencies
     run: pwsh Techcrafts.Blogging.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
   ```

---

## 4. Test Execution & Artifact Retention

- Tests are executed with `TEST_CONNECTION_STRING` pointing to `Server=127.0.0.1,1433;Database=Techcrafts_Certification_Test;User Id=sa;Password=Password123!;TrustServerCertificate=True;MultipleActiveResultSets=true;`.
- Structured TRX test logs are uploaded via `actions/upload-artifact@v4` with a 7-day retention policy.
- Playwright screenshot/trace artifacts are captured and archived.
