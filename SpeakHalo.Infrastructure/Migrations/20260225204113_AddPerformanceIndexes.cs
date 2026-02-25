using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakHalo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserTrials_IsTrial_TrialEndsAt",
                table: "UserTrials",
                columns: new[] { "IsTrial", "TrialEndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTrials_TrialStartedAt",
                table: "UserTrials",
                column: "TrialStartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_IsOnTrial_TrialEndsAt",
                table: "UserSubscriptions",
                columns: new[] { "IsOnTrial", "TrialEndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_IsTemporarilySuspended",
                table: "UserSubscriptions",
                column: "IsTemporarilySuspended");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_NextBillingDate_HasActivePayment",
                table: "UserSubscriptions",
                columns: new[] { "NextBillingDate", "HasActivePayment" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrialInvites_IsRedeemed_ExpiresAt",
                table: "TrialInvites",
                columns: new[] { "IsRedeemed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_Type_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status_ScheduledAt_ReminderSent24h",
                table: "Appointments",
                columns: new[] { "Status", "ScheduledAt", "ReminderSent24h" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserTrials_IsTrial_TrialEndsAt",
                table: "UserTrials");

            migrationBuilder.DropIndex(
                name: "IX_UserTrials_TrialStartedAt",
                table: "UserTrials");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_IsOnTrial_TrialEndsAt",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_IsTemporarilySuspended",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_NextBillingDate_HasActivePayment",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsActive",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TrialInvites_IsRedeemed_ExpiresAt",
                table: "TrialInvites");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_Type_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Status_ScheduledAt_ReminderSent24h",
                table: "Appointments");
        }
    }
}
