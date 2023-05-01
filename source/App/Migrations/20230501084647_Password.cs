using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace efcore_transactions.Migrations
{
    /// <inheritdoc />
    public partial class Password : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "People",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "password");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "People");
        }
    }
}
