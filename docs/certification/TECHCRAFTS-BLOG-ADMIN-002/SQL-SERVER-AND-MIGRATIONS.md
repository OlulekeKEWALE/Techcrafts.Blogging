# TECHCRAFTS-BLOG-ADMIN-002 — SQL Server & Migrations Certification

## 1. SQL Server Environment
- **Server Version:** Microsoft SQL Server 2022 (RTM-GDR) (KB5128493) - 16.0.1210.3 (X64) Developer Edition on Windows 10/11
- **Service Name:** `MSSQLSERVER` (Running)
- **Database Client:** `sqlcmd` ODBC 17 / Microsoft.Data.SqlClient / System.Data.SqlClient
- **Isolated Test Database:** `Techcrafts_Certification_Test`

## 2. Migration Reproducibility from Empty Schema
The complete migration chain was applied to a clean database created specifically for this audit:
```bash
sqlcmd -S localhost -E -Q "CREATE DATABASE Techcrafts_Certification_Test;"
dotnet ef database update --project Techcrafts.Blogging.Infrastructure/Techcrafts.Blogging.Infrastructure.csproj --startup-project Techcrafts.Blogging.Web/Techcrafts.Blogging.Web.csproj --connection "Server=localhost;Database=Techcrafts_Certification_Test;Integrated Security=True;TrustServerCertificate=True;"
```

### Verified Applied Migrations in `__EFMigrationsHistory`:
1. `20260724220607_InitialCreate`
2. `20260724230023_AddIdentitySchema`
3. `20260725002130_AddPostReactions`
4. `20260728015404_AddTelemetryLogs`

Result: **100% reproducible with zero errors.**

## 3. Physical Table Schema & Index Verification
Inspection of SQL Server system catalog `sys.indexes` on table `dbo.Posts`:
- `PK_Posts` — Primary Key clustered on `Id` (uniqueidentifier)
- `IX_Posts_Slug` — Unique index on `Slug` (`is_unique = 1`)
- `IX_Posts_AuthorId` — Foreign key index on `AuthorId` (`is_unique = 0`)
- `IX_Posts_Status_PublishedDate` — Composite descending index on `Status, PublishedDate` (`is_unique = 0`)

## 4. Concurrency & Integrity Test Evidence
Five dedicated SQL Server integration tests executed directly against the real SQL Server 2022 instance in `Techcrafts.Blogging.Tests/SqlServerIntegrationCertificationTests.cs`:
1. `SQL_G06_UniqueSlugIndex_EnforcesUniquenessUnderConcurrentInserts`: Verified that inserting a duplicate slug throws `DbUpdateException` wrapping `SqlException` citing index `IX_Posts_Slug`.
2. `SQL_G06_SlugHelper_ResolvesCollisionsOnRealSqlServer`: Verified that concurrent creations with identical titles resolve deterministically as `cloud-architecture`, `cloud-architecture-1`, `cloud-architecture-2`.
3. `SQL_G08_PublicationIdempotencyAndLifecyle_OnRealSqlServer`: Verified publication timestamp stability on repeated calls and instantaneous revocation on unpublish.
4. `SQL_G09_AtomicDeleteWithCascadeAndDependentCleanups_OnRealSqlServer`: Verified full transactional cleanup across Comments, Reactions, Claps, Bookmarks, and PageViews without orphan records.
5. `SQL_G07_ConcurrentEdit_TracksAuditDateCorrectly`: Verified multiple edits update `UpdatedDate` correctly via `IAuditableEntity`.
