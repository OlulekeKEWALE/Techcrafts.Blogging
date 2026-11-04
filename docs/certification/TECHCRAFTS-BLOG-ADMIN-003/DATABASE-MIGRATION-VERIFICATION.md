# TECHCRAFTS-BLOG-ADMIN-003 — Database Migration & Schema Verification

## 1. Migration Specification

To persist the optimistic concurrency token in Microsoft SQL Server without altering or resetting existing blog posts, a dedicated EF Core code-first migration was generated and applied:

- **Migration Identifier:** `20261103232610_AddPostRowVersion`
- **Target File:** `Techcrafts.Blogging.Infrastructure/Migrations/20261103232610_AddPostRowVersion.cs`

### 1.1 Migration Code
```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Techcrafts.Blogging.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Posts",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Posts");
        }
    }
}
```

---

## 2. Direct SQL Server Database Inspection

The migration was applied to the live SQL Server 2022 instance hosting `Techcrafts_Certification_Test`. Verification was executed via `sqlcmd`:

```sql
SELECT 
    TABLE_NAME, 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Posts' AND COLUMN_NAME = 'RowVersion';
```

### Result:
```text
TABLE_NAME  COLUMN_NAME  DATA_TYPE  IS_NULLABLE
----------  -----------  ---------  -----------
Posts       RowVersion   timestamp  NO
```
*Note: In Microsoft SQL Server, `rowversion` is the synonym for the physical `timestamp` binary storage type, which automatically increments an 8-byte monotonically increasing binary counter on every INSERT and UPDATE operation.*

---

## 3. Data Integrity & Non-Destructive Proof

Verification of row preservation across existing seeded blog posts:
```sql
SELECT Id, Title, RowVersion FROM dbo.Posts;
```

### Verification Evidence:
```text
Id                                   Title                                                  RowVersion
------------------------------------ ------------------------------------------------------ ------------------
1628A18F-1B21-4E60-B14E-08DF3A09CDAB The Evolution of Software Architecture in the Cloud... 0x00000000000007D1
```

- Existing rows retained all original primary keys, foreign keys, slugs, content, tags, and timestamps.
- SQL Server automatically generated valid initial 8-byte binary tokens (`0x00000000000007D1`) for all existing records.
- Zero data loss or corruption occurred.
