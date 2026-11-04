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
