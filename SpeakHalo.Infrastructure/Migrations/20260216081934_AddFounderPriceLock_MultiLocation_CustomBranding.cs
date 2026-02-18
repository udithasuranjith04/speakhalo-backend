using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakHalo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFounderPriceLock_MultiLocation_CustomBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallFollowUps_Calls_CallId",
                table: "CallFollowUps");

            migrationBuilder.DropForeignKey(
                name: "FK_CallFollowUps_Users_UserId",
                table: "CallFollowUps");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_UserId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyUsageArchives_Users_UserId",
                table: "MonthlyUsageArchives");

            migrationBuilder.DropForeignKey(
                name: "FK_OverageInvoices_MonthlyUsageArchives_ArchiveId",
                table: "OverageInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_OverageInvoices_Users_UserId",
                table: "OverageInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Customers_CustomerId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Users_UserId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_UsageCosts_Users_UserId",
                table: "UsageCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_UsageWarnings_Users_UserId",
                table: "UsageWarnings");

            migrationBuilder.DropIndex(
                name: "IX_CouponUsages_CouponId_UserId",
                table: "CouponUsages");

            migrationBuilder.AddColumn<string>(
                name: "LocationAddress",
                table: "VoiceAgents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "VoiceAgents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationPhone",
                table: "VoiceAgents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFounderPriceLocked",
                table: "UserSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LockedMonthlyPrice",
                table: "UserSubscriptions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceLockReason",
                table: "UserSubscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PriceLockedAt",
                table: "UserSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Failed2faAttempts",
                table: "UserAuths",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TwoFactorLockoutEndTime",
                table: "UserAuths",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandColor",
                table: "AgentProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandName",
                table: "AgentProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandTagline",
                table: "AgentProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "AgentProfiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_TwilioMessageSid",
                table: "WhatsAppMessages",
                column: "TwilioMessageSid");

            migrationBuilder.CreateIndex(
                name: "IX_SMSMessages_TwilioMessageSid",
                table: "SMSMessages",
                column: "TwilioMessageSid");

            migrationBuilder.CreateIndex(
                name: "IX_ProductKnowledge_Category",
                table: "ProductKnowledge",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ProductKnowledge_IsActive",
                table: "ProductKnowledge",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProductKnowledge_Topic",
                table: "ProductKnowledge",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_PartsCatalog_EquipmentType",
                table: "PartsCatalog",
                column: "EquipmentType");

            migrationBuilder.CreateIndex(
                name: "IX_PartsCatalog_Industry",
                table: "PartsCatalog",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_PartsCatalog_IsActive",
                table: "PartsCatalog",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PartsCatalog_PartNumber",
                table: "PartsCatalog",
                column: "PartNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseArticles_Category",
                table: "KnowledgeBaseArticles",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseArticles_Industry",
                table: "KnowledgeBaseArticles",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseArticles_IsActive",
                table: "KnowledgeBaseArticles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_CouponId_UserId",
                table: "CouponUsages",
                columns: new[] { "CouponId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommonIssues_EquipmentType",
                table: "CommonIssues",
                column: "EquipmentType");

            migrationBuilder.CreateIndex(
                name: "IX_CommonIssues_Industry",
                table: "CommonIssues",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_CommonIssues_IsActive",
                table: "CommonIssues",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AITrainingExamples_Category",
                table: "AITrainingExamples",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AITrainingExamples_IncludeInTraining",
                table: "AITrainingExamples",
                column: "IncludeInTraining");

            migrationBuilder.CreateIndex(
                name: "IX_AITrainingExamples_Intent",
                table: "AITrainingExamples",
                column: "Intent");

            migrationBuilder.AddForeignKey(
                name: "FK_CallFollowUps_Calls_CallId",
                table: "CallFollowUps",
                column: "CallId",
                principalTable: "Calls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CallFollowUps_Users_UserId",
                table: "CallFollowUps",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_UserId",
                table: "Invoices",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyUsageArchives_Users_UserId",
                table: "MonthlyUsageArchives",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OverageInvoices_MonthlyUsageArchives_ArchiveId",
                table: "OverageInvoices",
                column: "ArchiveId",
                principalTable: "MonthlyUsageArchives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OverageInvoices_Users_UserId",
                table: "OverageInvoices",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Customers_CustomerId",
                table: "Quotes",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Users_UserId",
                table: "Quotes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UsageCosts_Users_UserId",
                table: "UsageCosts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UsageWarnings_Users_UserId",
                table: "UsageWarnings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallFollowUps_Calls_CallId",
                table: "CallFollowUps");

            migrationBuilder.DropForeignKey(
                name: "FK_CallFollowUps_Users_UserId",
                table: "CallFollowUps");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_UserId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyUsageArchives_Users_UserId",
                table: "MonthlyUsageArchives");

            migrationBuilder.DropForeignKey(
                name: "FK_OverageInvoices_MonthlyUsageArchives_ArchiveId",
                table: "OverageInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_OverageInvoices_Users_UserId",
                table: "OverageInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Customers_CustomerId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Users_UserId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_UsageCosts_Users_UserId",
                table: "UsageCosts");

            migrationBuilder.DropForeignKey(
                name: "FK_UsageWarnings_Users_UserId",
                table: "UsageWarnings");

            migrationBuilder.DropIndex(
                name: "IX_WhatsAppMessages_TwilioMessageSid",
                table: "WhatsAppMessages");

            migrationBuilder.DropIndex(
                name: "IX_SMSMessages_TwilioMessageSid",
                table: "SMSMessages");

            migrationBuilder.DropIndex(
                name: "IX_ProductKnowledge_Category",
                table: "ProductKnowledge");

            migrationBuilder.DropIndex(
                name: "IX_ProductKnowledge_IsActive",
                table: "ProductKnowledge");

            migrationBuilder.DropIndex(
                name: "IX_ProductKnowledge_Topic",
                table: "ProductKnowledge");

            migrationBuilder.DropIndex(
                name: "IX_PartsCatalog_EquipmentType",
                table: "PartsCatalog");

            migrationBuilder.DropIndex(
                name: "IX_PartsCatalog_Industry",
                table: "PartsCatalog");

            migrationBuilder.DropIndex(
                name: "IX_PartsCatalog_IsActive",
                table: "PartsCatalog");

            migrationBuilder.DropIndex(
                name: "IX_PartsCatalog_PartNumber",
                table: "PartsCatalog");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeBaseArticles_Category",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeBaseArticles_Industry",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeBaseArticles_IsActive",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropIndex(
                name: "IX_CouponUsages_CouponId_UserId",
                table: "CouponUsages");

            migrationBuilder.DropIndex(
                name: "IX_CommonIssues_EquipmentType",
                table: "CommonIssues");

            migrationBuilder.DropIndex(
                name: "IX_CommonIssues_Industry",
                table: "CommonIssues");

            migrationBuilder.DropIndex(
                name: "IX_CommonIssues_IsActive",
                table: "CommonIssues");

            migrationBuilder.DropIndex(
                name: "IX_AITrainingExamples_Category",
                table: "AITrainingExamples");

            migrationBuilder.DropIndex(
                name: "IX_AITrainingExamples_IncludeInTraining",
                table: "AITrainingExamples");

            migrationBuilder.DropIndex(
                name: "IX_AITrainingExamples_Intent",
                table: "AITrainingExamples");

            migrationBuilder.DropColumn(
                name: "LocationAddress",
                table: "VoiceAgents");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "VoiceAgents");

            migrationBuilder.DropColumn(
                name: "LocationPhone",
                table: "VoiceAgents");

            migrationBuilder.DropColumn(
                name: "IsFounderPriceLocked",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "LockedMonthlyPrice",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PriceLockReason",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PriceLockedAt",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "Failed2faAttempts",
                table: "UserAuths");

            migrationBuilder.DropColumn(
                name: "TwoFactorLockoutEndTime",
                table: "UserAuths");

            migrationBuilder.DropColumn(
                name: "BrandColor",
                table: "AgentProfiles");

            migrationBuilder.DropColumn(
                name: "BrandName",
                table: "AgentProfiles");

            migrationBuilder.DropColumn(
                name: "BrandTagline",
                table: "AgentProfiles");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "AgentProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_CouponId_UserId",
                table: "CouponUsages",
                columns: new[] { "CouponId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CallFollowUps_Calls_CallId",
                table: "CallFollowUps",
                column: "CallId",
                principalTable: "Calls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CallFollowUps_Users_UserId",
                table: "CallFollowUps",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_UserId",
                table: "Invoices",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyUsageArchives_Users_UserId",
                table: "MonthlyUsageArchives",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OverageInvoices_MonthlyUsageArchives_ArchiveId",
                table: "OverageInvoices",
                column: "ArchiveId",
                principalTable: "MonthlyUsageArchives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OverageInvoices_Users_UserId",
                table: "OverageInvoices",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Customers_CustomerId",
                table: "Quotes",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Users_UserId",
                table: "Quotes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsageCosts_Users_UserId",
                table: "UsageCosts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsageWarnings_Users_UserId",
                table: "UsageWarnings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
