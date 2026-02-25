using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakHalo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFirstPaymentAtToSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstPaymentAt",
                table: "UserSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaddleCancelUrl",
                table: "UserSubscriptions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaddleCanceledAt",
                table: "UserSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaddleCurrentPeriodEnd",
                table: "UserSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaddleCustomerId",
                table: "UserSubscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaddlePriceId",
                table: "UserSubscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaddleSubscriptionCreatedAt",
                table: "UserSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaddleSubscriptionId",
                table: "UserSubscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaddleSubscriptionStatus",
                table: "UserSubscriptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaddleUpdatePaymentMethodUrl",
                table: "UserSubscriptions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReason",
                table: "UserSubscriptions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "UserSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AIOverageRate",
                table: "PricingPlans",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ImageOverageRate",
                table: "PricingPlans",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaddlePriceId",
                table: "PricingPlans",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaddleProductId",
                table: "PricingPlans",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SMSOverageRate",
                table: "PricingPlans",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TierSlug",
                table: "PricingPlans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WhatsAppOverageRate",
                table: "PricingPlans",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "TrialInvites",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PricingPlanId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TrialDays = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRedeemed = table.Column<bool>(type: "boolean", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RedeemedByUserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedByAdminId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrialInvites_PricingPlans_PricingPlanId",
                        column: x => x.PricingPlanId,
                        principalTable: "PricingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PaddleCustomerId",
                table: "UserSubscriptions",
                column: "PaddleCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PaddleSubscriptionId",
                table: "UserSubscriptions",
                column: "PaddleSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingPlans_TierSlug",
                table: "PricingPlans",
                column: "TierSlug",
                unique: true,
                filter: "\"TierSlug\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TrialInvites_ExpiresAt",
                table: "TrialInvites",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrialInvites_IsRedeemed",
                table: "TrialInvites",
                column: "IsRedeemed");

            migrationBuilder.CreateIndex(
                name: "IX_TrialInvites_PricingPlanId",
                table: "TrialInvites",
                column: "PricingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TrialInvites_Token",
                table: "TrialInvites",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrialInvites");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_PaddleCustomerId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_PaddleSubscriptionId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_PricingPlans_TierSlug",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "FirstPaymentAt",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaddleCancelUrl",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaddleCanceledAt",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaddleCurrentPeriodEnd",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaddleCustomerId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaddlePriceId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaddleSubscriptionCreatedAt",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaddleSubscriptionId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaddleSubscriptionStatus",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaddleUpdatePaymentMethodUrl",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "RefundReason",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "AIOverageRate",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "ImageOverageRate",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "PaddlePriceId",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "PaddleProductId",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "SMSOverageRate",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "TierSlug",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "WhatsAppOverageRate",
                table: "PricingPlans");
        }
    }
}
