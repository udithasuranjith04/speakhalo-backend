using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakHalo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "UserProfiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "UserProfiles");
        }
    }
}
