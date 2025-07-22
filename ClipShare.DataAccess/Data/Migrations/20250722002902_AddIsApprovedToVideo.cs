using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClipShare.DataAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsApprovedToVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Video",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Video");
        }
    }
}
