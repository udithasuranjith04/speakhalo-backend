using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakHalo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndustryKnowledgeTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndustryKnowledgeTemplates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Industry = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastEditedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    LastEditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndustryKnowledgeTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndustryKnowledgeTemplates_Industry_Category",
                table: "IndustryKnowledgeTemplates",
                columns: new[] { "Industry", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_IndustryKnowledgeTemplates_Industry_IsActive_SortOrder",
                table: "IndustryKnowledgeTemplates",
                columns: new[] { "Industry", "IsActive", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndustryKnowledgeTemplates");
        }
    }
}
