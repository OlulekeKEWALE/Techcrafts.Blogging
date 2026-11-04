# TECHCRAFTS-BLOG-ADMIN-002 — Release and Rollback Strategy

## 1. Zero-Downtime Deployment Prerequisites
- .NET 10.0 runtime (`Microsoft.AspNetCore.App 10.0.x`) installed on host or container image.
- SQL Server 2022 instance running and accessible with migration execution privileges.
- Connection string configured in environment variable `ConnectionStrings__DefaultConnection` or secure key vault.

## 2. Migration Execution Sequence
1. Backup target database before deployment:
   ```sql
   BACKUP DATABASE [Techcrafts.Blogging] TO DISK = 'NUL:' WITH NOFORMAT; -- verify VSS or full backup
   ```
2. Apply additive EF Core migrations:
   ```bash
   dotnet ef database update --project Techcrafts.Blogging.Infrastructure/Techcrafts.Blogging.Infrastructure.csproj --startup-project Techcrafts.Blogging.Web/Techcrafts.Blogging.Web.csproj
   ```
3. Deploy updated application binary (`Release` build).
4. Automated health check verification:
   - Hit `GET /` to verify public listing returns 200 OK.
   - Hit `GET /admin` to verify redirect to authentication.

## 3. Rollback Plan
- **Application Rollback:** Revert deployment artifact to previous container revision or binary release.
- **Database Rollback:** Because the migrations in this release are strictly non-destructive (new indices and existing table schema alignment), rolling back the application layer does not require rolling back historical database tables.
- **Data Integrity Preservation:** Any posts drafted or edited during canary testing remain intact in `dbo.Posts` with valid `Status`, `Slug`, and `AuthorId` associations.
