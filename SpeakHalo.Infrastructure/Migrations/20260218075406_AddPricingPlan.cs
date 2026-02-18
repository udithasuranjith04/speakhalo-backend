using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakHalo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CouponUsages");

            migrationBuilder.DropTable(
                name: "PartsCatalog");

            migrationBuilder.DropTable(
                name: "TechnicianAssignments");

            migrationBuilder.DropTable(
                name: "TrialRequests");

            migrationBuilder.DropTable(
                name: "CouponCodes");

            migrationBuilder.DropTable(
                name: "Technicians");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_IssueId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_IssueId_IsFirstVisit",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TechnicianId_IsIssueResolved",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "IsFirstVisit",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "IsIssueResolved",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "IssueId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "IssueResolvedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ResolutionNotes",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "TechnicianId",
                table: "Appointments");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnTrial",
                table: "UserSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PricingPlanId",
                table: "UserSubscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "UserSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessAddress",
                table: "UserProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessPhone",
                table: "UserProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessWebsite",
                table: "UserProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PricingPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    TrialDays = table.Column<int>(type: "integer", nullable: false),
                    AIMinutesIncluded = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppMessagesIncluded = table.Column<int>(type: "integer", nullable: false),
                    SMSMessagesIncluded = table.Column<int>(type: "integer", nullable: false),
                    ImagesIncluded = table.Column<int>(type: "integer", nullable: false),
                    MaxPhoneNumbers = table.Column<int>(type: "integer", nullable: false),
                    EnforceHardLimits = table.Column<bool>(type: "boolean", nullable: false),
                    EnabledFeatures = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PricingPlanId",
                table: "UserSubscriptions",
                column: "PricingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingPlans_Name",
                table: "PricingPlans",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_PricingPlans_PricingPlanId",
                table: "UserSubscriptions",
                column: "PricingPlanId",
                principalTable: "PricingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_PricingPlans_PricingPlanId",
                table: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "PricingPlans");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_PricingPlanId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "IsOnTrial",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PricingPlanId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "BusinessAddress",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "BusinessPhone",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "BusinessWebsite",
                table: "UserProfiles");

            migrationBuilder.AddColumn<bool>(
                name: "IsFirstVisit",
                table: "Appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsIssueResolved",
                table: "Appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IssueId",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssueResolvedAt",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicianId",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CouponCodes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CreatedByAdminId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AIMinutes = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomPriceDurationMonths = table.Column<int>(type: "integer", nullable: true),
                    CustomPriceOverride = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomPriceReason = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FounderNumber = table.Column<int>(type: "integer", nullable: true),
                    ImagesLimit = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsFounderCustomer = table.Column<bool>(type: "boolean", nullable: false),
                    MaxUses = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PhoneNumbersAllowed = table.Column<int>(type: "integer", nullable: false),
                    SmsMessagesLimit = table.Column<int>(type: "integer", nullable: false),
                    TargetEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TimesUsed = table.Column<int>(type: "integer", nullable: false),
                    TrialDays = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WhatsAppMessagesLimit = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponCodes", x => x.Id);
                    table.CheckConstraint("CK_CouponCode_AIMinutes_Positive", "\"AIMinutes\" >= 0");
                    table.CheckConstraint("CK_CouponCode_Code_Length", "LENGTH(\"Code\") >= 4");
                    table.CheckConstraint("CK_CouponCode_MaxUses_Positive", "\"MaxUses\" IS NULL OR \"MaxUses\" > 0");
                    table.CheckConstraint("CK_CouponCode_PhoneNumbers_Positive", "\"PhoneNumbersAllowed\" > 0");
                    table.CheckConstraint("CK_CouponCode_SMS_Positive", "\"SmsMessagesLimit\" >= 0");
                    table.CheckConstraint("CK_CouponCode_TimesUsed_NonNegative", "\"TimesUsed\" >= 0");
                    table.CheckConstraint("CK_CouponCode_TrialDays_Positive", "\"TrialDays\" > 0");
                    table.CheckConstraint("CK_CouponCode_WhatsApp_Positive", "\"WhatsAppMessagesLimit\" >= 0");
                    table.ForeignKey(
                        name: "FK_CouponCodes_Users_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartsCatalog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AlternativePartNumbers = table.Column<List<string>>(type: "text[]", nullable: false),
                    AvailabilityStatus = table.Column<string>(type: "text", nullable: true),
                    AverageCustomerSatisfaction = table.Column<double>(type: "double precision", nullable: false),
                    CommonalityScore = table.Column<int>(type: "integer", nullable: false),
                    CompatibleBrands = table.Column<List<string>>(type: "text[]", nullable: false),
                    CompatibleModels = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Dimensions = table.Column<string>(type: "text", nullable: true),
                    EquipmentType = table.Column<string>(type: "text", nullable: false),
                    ExpectedLifespanMonths = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Industry = table.Column<int>(type: "integer", nullable: false),
                    InstallationNotes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsDiscontinued = table.Column<bool>(type: "boolean", nullable: false),
                    IsPreferredPart = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    PartCategory = table.Column<string>(type: "text", nullable: false),
                    PartName = table.Column<string>(type: "text", nullable: false),
                    PartNumber = table.Column<string>(type: "text", nullable: false),
                    PreferredSupplier = table.Column<string>(type: "text", nullable: true),
                    QuantityInStock = table.Column<int>(type: "integer", nullable: true),
                    RecommendationCount = table.Column<int>(type: "integer", nullable: false),
                    ReorderLevel = table.Column<int>(type: "integer", nullable: true),
                    RetailPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SuggestedMarkup = table.Column<decimal>(type: "numeric", nullable: false),
                    SupersededBy = table.Column<string>(type: "text", nullable: true),
                    Supersedes = table.Column<string>(type: "text", nullable: true),
                    SupplierPartNumber = table.Column<string>(type: "text", nullable: true),
                    TechnicalSpecs = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WarrantyInformation = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<double>(type: "double precision", nullable: true),
                    WholesaleCost = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartsCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technicians",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    AverageRating = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentLatitude = table.Column<double>(type: "double precision", nullable: true),
                    CurrentLongitude = table.Column<double>(type: "double precision", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    JobsCompleted = table.Column<int>(type: "integer", nullable: false),
                    LastLocationUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    ServiceAreas = table.Column<string>(type: "text", nullable: false),
                    Skills = table.Column<string>(type: "text", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technicians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Technicians_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrialRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ApprovedByAdminId = table.Column<string>(type: "text", nullable: true),
                    ConvertedUserId = table.Column<string>(type: "text", nullable: true),
                    RejectedByAdminId = table.Column<string>(type: "text", nullable: true),
                    AdminNotes = table.Column<string>(type: "text", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ConvertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    HasConverted = table.Column<bool>(type: "boolean", nullable: false),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    NumberOfTechnicians = table.Column<int>(type: "integer", nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReferralSource = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    RequestedReason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrialRequests_Users_ApprovedByAdminId",
                        column: x => x.ApprovedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrialRequests_Users_ConvertedUserId",
                        column: x => x.ConvertedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrialRequests_Users_RejectedByAdminId",
                        column: x => x.RejectedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CouponUsages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CouponId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AIMinutesGranted = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    SmsMessagesGranted = table.Column<int>(type: "integer", nullable: false),
                    TrialDaysGranted = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WhatsAppMessagesGranted = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponUsages", x => x.Id);
                    table.CheckConstraint("CK_CouponUsage_AIMinutes_NonNegative", "\"AIMinutesGranted\" >= 0");
                    table.CheckConstraint("CK_CouponUsage_TrialDays_Positive", "\"TrialDaysGranted\" > 0");
                    table.ForeignKey(
                        name: "FK_CouponUsages_CouponCodes_CouponId",
                        column: x => x.CouponId,
                        principalTable: "CouponCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CouponUsages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicianAssignments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AppointmentId = table.Column<string>(type: "text", nullable: false),
                    TechnicianId = table.Column<string>(type: "text", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicianAssignments_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnicianAssignments_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_IssueId",
                table: "Appointments",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_IssueId_IsFirstVisit",
                table: "Appointments",
                columns: new[] { "IssueId", "IsFirstVisit" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TechnicianId_IsIssueResolved",
                table: "Appointments",
                columns: new[] { "TechnicianId", "IsIssueResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_CouponCodes_Code",
                table: "CouponCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CouponCodes_CreatedByAdminId",
                table: "CouponCodes",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponCodes_ExpiresAt",
                table: "CouponCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CouponCodes_IsActive",
                table: "CouponCodes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CouponCodes_IsActive_ExpiresAt",
                table: "CouponCodes",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CouponCodes_TargetEmail",
                table: "CouponCodes",
                column: "TargetEmail");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_CouponId",
                table: "CouponUsages",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_CouponId_UserId",
                table: "CouponUsages",
                columns: new[] { "CouponId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_UsedAt",
                table: "CouponUsages",
                column: "UsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_UserId",
                table: "CouponUsages",
                column: "UserId");

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
                name: "IX_TechnicianAssignments_AppointmentId",
                table: "TechnicianAssignments",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianAssignments_TechnicianId",
                table: "TechnicianAssignments",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_IsActive",
                table: "Technicians",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_Phone",
                table: "Technicians",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_UserId",
                table: "Technicians",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrialRequests_ApprovedByAdminId",
                table: "TrialRequests",
                column: "ApprovedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_TrialRequests_ConvertedUserId",
                table: "TrialRequests",
                column: "ConvertedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrialRequests_CreatedAt",
                table: "TrialRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrialRequests_Email",
                table: "TrialRequests",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_TrialRequests_Email_Status",
                table: "TrialRequests",
                columns: new[] { "Email", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TrialRequests_RejectedByAdminId",
                table: "TrialRequests",
                column: "RejectedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_TrialRequests_Status",
                table: "TrialRequests",
                column: "Status");
        }
    }
}
