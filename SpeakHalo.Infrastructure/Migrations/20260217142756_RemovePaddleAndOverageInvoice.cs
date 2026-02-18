using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakHalo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePaddleAndOverageInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OverageInvoices");

            migrationBuilder.DropColumn(
                name: "PaddleCustomerId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaddlePriceId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaddleSubscriptionId",
                table: "UserSubscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaddleCustomerId",
                table: "UserSubscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaddlePriceId",
                table: "UserSubscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaddleSubscriptionId",
                table: "UserSubscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OverageInvoices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ArchiveId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    AIOverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    BillingPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ImagesOverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaddleCustomerId = table.Column<string>(type: "text", nullable: true),
                    PaddleInvoiceUrl = table.Column<string>(type: "text", nullable: true),
                    PaddleTransactionId = table.Column<string>(type: "text", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentId = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    SMSOverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WhatsAppOverageCost = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverageInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OverageInvoices_MonthlyUsageArchives_ArchiveId",
                        column: x => x.ArchiveId,
                        principalTable: "MonthlyUsageArchives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OverageInvoices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OverageInvoices_ArchiveId",
                table: "OverageInvoices",
                column: "ArchiveId");

            migrationBuilder.CreateIndex(
                name: "IX_OverageInvoices_Status",
                table: "OverageInvoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OverageInvoices_UserId_BillingPeriodStart",
                table: "OverageInvoices",
                columns: new[] { "UserId", "BillingPeriodStart" });
        }
    }
}
