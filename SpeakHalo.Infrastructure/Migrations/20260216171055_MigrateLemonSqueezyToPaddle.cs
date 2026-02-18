using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakHalo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateLemonSqueezyToPaddle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LemonSqueezyCustomerId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "LemonSqueezySubscriptionId",
                table: "UserSubscriptions");

            migrationBuilder.RenameColumn(
                name: "StripeSubscriptionId",
                table: "UserSubscriptions",
                newName: "PaddleSubscriptionId");

            migrationBuilder.RenameColumn(
                name: "StripeCustomerId",
                table: "UserSubscriptions",
                newName: "PaddlePriceId");

            migrationBuilder.RenameColumn(
                name: "LemonSqueezyVariantId",
                table: "UserSubscriptions",
                newName: "PaddleCustomerId");

            migrationBuilder.RenameColumn(
                name: "LemonSqueezyInvoiceUrl",
                table: "OverageInvoices",
                newName: "PaddleTransactionId");

            migrationBuilder.RenameColumn(
                name: "LemonSqueezyInvoiceId",
                table: "OverageInvoices",
                newName: "PaddleInvoiceUrl");

            migrationBuilder.RenameColumn(
                name: "LemonSqueezyCustomerId",
                table: "OverageInvoices",
                newName: "PaddleCustomerId");

            migrationBuilder.AddColumn<bool>(
                name: "EnableTroubleshooting",
                table: "AgentProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableTroubleshooting",
                table: "AgentProfiles");

            migrationBuilder.RenameColumn(
                name: "PaddleSubscriptionId",
                table: "UserSubscriptions",
                newName: "StripeSubscriptionId");

            migrationBuilder.RenameColumn(
                name: "PaddlePriceId",
                table: "UserSubscriptions",
                newName: "StripeCustomerId");

            migrationBuilder.RenameColumn(
                name: "PaddleCustomerId",
                table: "UserSubscriptions",
                newName: "LemonSqueezyVariantId");

            migrationBuilder.RenameColumn(
                name: "PaddleTransactionId",
                table: "OverageInvoices",
                newName: "LemonSqueezyInvoiceUrl");

            migrationBuilder.RenameColumn(
                name: "PaddleInvoiceUrl",
                table: "OverageInvoices",
                newName: "LemonSqueezyInvoiceId");

            migrationBuilder.RenameColumn(
                name: "PaddleCustomerId",
                table: "OverageInvoices",
                newName: "LemonSqueezyCustomerId");

            migrationBuilder.AddColumn<string>(
                name: "LemonSqueezyCustomerId",
                table: "UserSubscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LemonSqueezySubscriptionId",
                table: "UserSubscriptions",
                type: "text",
                nullable: true);
        }
    }
}
