using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace efcore_transactions.Migrations
{
    /// <inheritdoc />
    public partial class AddParents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ParentId",
                table: "People",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_ParentId",
                table: "People",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_People_People_ParentId",
                table: "People",
                column: "ParentId",
                principalTable: "People",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_People_People_ParentId",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_ParentId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "People");
        }
    }
}
