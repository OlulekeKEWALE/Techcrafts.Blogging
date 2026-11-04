# TECHCRAFTS-BLOG-ADMIN-004 — Migration, Deployment & Rollback Runbook

## 1. Overview & Change Scope
- **Change Target:** Administrative Blog Post Management, Optimistic Concurrency Hardening, and E2E Certification.
- **EF Core Migration Identifier:** `20261103232610_AddPostRowVersion`
- **Impacted Database:** Microsoft SQL Server 2022 (Production / Staging / Test)
- **Target Table:** `dbo.Posts`

---

## 2. Pre-Deployment Readiness & Dry-Run Checklist
1. **Database Backup:** Take a full copy-only backup of the target database:
   ```sql
   BACKUP DATABASE [Techcrafts_Blogging] 
   TO DISK = 'C:\Backups\Techcrafts_Blogging_PreAdmin004.bak' 
   WITH INIT, COMPRESSION;
   ```
2. **Schema Compatibility Assessment:**
   - The addition of `[RowVersion] timestamp NOT NULL` is strictly additive.
   - SQL Server automatically generates values for existing rows upon creation.
   - Zero column locks or blocking table rebuilds on SQL Server 2022.
3. **Generate Idempotent Migration Script (Dry-Run):**
   ```shell
   dotnet ef migrations script 20260728015404_AddTelemetryLogs 20261103232610_AddPostRowVersion \
     --project Techcrafts.Blogging.Infrastructure \
     --startup-project Techcrafts.Blogging.Web \
     --idempotent \
     --output artifacts/migration_AddPostRowVersion.sql
   ```

---

## 3. Production Deployment Execution Steps (Runbook Only)

> [!CAUTION]
> This runbook is strictly documented for authorized human release engineers. Automated deployment or unapproved staging/production execution is explicitly forbidden.

### Step 1: Pre-Maintenance Verification
- Verify database connectivity and disk capacity.
- Confirm active connection count.

### Step 2: Database Migration Application
- Apply EF Core migration to target SQL Server:
  ```shell
  dotnet ef database update 20261103232610_AddPostRowVersion \
    --project Techcrafts.Blogging.Infrastructure \
    --startup-project Techcrafts.Blogging.Web
  ```
- **Post-Migration Validation Query:**
  ```sql
  SELECT 
      COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
  FROM INFORMATION_SCHEMA.COLUMNS 
  WHERE TABLE_NAME = 'Posts' AND COLUMN_NAME = 'RowVersion';
  ```
  *Expected Output:* `RowVersion`, `timestamp`, `NO`.

### Step 3: Application Binary Deployment
- Deploy the updated ASP.NET Core 10 application binaries.
- Ensure environment variable `ConnectionStrings__DefaultConnection` is configured.
- Ensure `ASPNETCORE_ENVIRONMENT=Production` (or `Staging`).

### Step 4: Smoke Test Protocol
1. Navigate to `/account/login` and authenticate with administrator credentials.
2. Visit `/admin/posts` and confirm management table renders with existing posts and correct status badges.
3. Author a new draft post and verify persistence.
4. Modify an existing post and verify slug stability and updated timestamps.
5. Trigger a concurrent edit on a test post; verify HTTP 409 Conflict returns the side-by-side diff.
6. Verify public access to published post and verify draft post returns HTTP 404.

---

## 4. Rollback & Contingency Plan

### Fast Binary Rollback (Old Application with New Schema):
- Because the `RowVersion` column is additive and does not remove or rename existing columns, old application binaries (ADMIN-001/002) ignore the `RowVersion` column.
- Rolling back application binaries to the previous release can be performed immediately without reversing the database schema.

### Database Schema Rollback (Down Migration):
- If the schema must be reverted to pre-concurrency state:
  ```shell
  dotnet ef database update 20260728015404_AddTelemetryLogs \
    --project Techcrafts.Blogging.Infrastructure \
    --startup-project Techcrafts.Blogging.Web
  ```
- Drops `RowVersion` column (`ALTER TABLE [Posts] DROP COLUMN [RowVersion]`).
- Existing post data, tags, comments, and relationships are fully preserved.

### Catastrophic Recovery:
- In the event of catastrophic data corruption, restore the pre-deployment full backup `Techcrafts_Blogging_PreAdmin004.bak`.
