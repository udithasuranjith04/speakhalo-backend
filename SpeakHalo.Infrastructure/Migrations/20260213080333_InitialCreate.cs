using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakHalo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIConversationContexts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<string>(type: "text", nullable: true),
                    ConversationType = table.Column<string>(type: "text", nullable: false),
                    CurrentIntent = table.Column<string>(type: "text", nullable: false),
                    CurrentTopic = table.Column<string>(type: "text", nullable: true),
                    LastUserMessage = table.Column<string>(type: "text", nullable: true),
                    LastAIResponse = table.Column<string>(type: "text", nullable: true),
                    LastInteractionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomerInfo = table.Column<string>(type: "text", nullable: true),
                    AppointmentInfo = table.Column<string>(type: "text", nullable: true),
                    ServiceHistory = table.Column<string>(type: "text", nullable: true),
                    CurrentIssue = table.Column<string>(type: "text", nullable: true),
                    PhotoAnalysis = table.Column<string>(type: "text", nullable: true),
                    Tone = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    UseEmojis = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIConversationContexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AITrainingExamples",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserMessage = table.Column<string>(type: "text", nullable: false),
                    ExpectedResponse = table.Column<string>(type: "text", nullable: false),
                    ActualResponse = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Intent = table.Column<string>(type: "text", nullable: false),
                    WasSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    CustomerSatisfactionScore = table.Column<int>(type: "integer", nullable: true),
                    FeedbackNotes = table.Column<string>(type: "text", nullable: true),
                    ConversationContext = table.Column<string>(type: "text", nullable: true),
                    BusinessContext = table.Column<string>(type: "text", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedBy = table.Column<string>(type: "text", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IncludeInTraining = table.Column<bool>(type: "boolean", nullable: false),
                    ImportanceWeight = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AITrainingExamples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommonIssues",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IssueName = table.Column<string>(type: "text", nullable: false),
                    Industry = table.Column<int>(type: "integer", nullable: false),
                    EquipmentType = table.Column<string>(type: "text", nullable: false),
                    CommonSymptoms = table.Column<List<string>>(type: "text[]", nullable: false),
                    VisualIndicators = table.Column<List<string>>(type: "text[]", nullable: false),
                    CustomerKeywords = table.Column<List<string>>(type: "text[]", nullable: false),
                    ProbableCause = table.Column<string>(type: "text", nullable: false),
                    AlternativeCauses = table.Column<List<string>>(type: "text[]", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    IsEmergency = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendedSolution = table.Column<string>(type: "text", nullable: false),
                    RequiredParts = table.Column<List<string>>(type: "text[]", nullable: false),
                    RequiredTools = table.Column<List<string>>(type: "text[]", nullable: false),
                    EstimatedRepairTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    RepairComplexity = table.Column<string>(type: "text", nullable: false),
                    MinimumCost = table.Column<decimal>(type: "numeric", nullable: false),
                    MaximumCost = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    AiMatchConfidence = table.Column<double>(type: "double precision", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    SeasonalFactors = table.Column<List<string>>(type: "text[]", nullable: true),
                    EnvironmentalFactors = table.Column<List<string>>(type: "text[]", nullable: true),
                    SafetyWarning = table.Column<string>(type: "text", nullable: true),
                    SafetyEquipmentRequired = table.Column<List<string>>(type: "text[]", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommonIssues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CookieDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpirationDays = table.Column<int>(type: "integer", nullable: true),
                    IsHttpOnly = table.Column<bool>(type: "boolean", nullable: false),
                    IsSecure = table.Column<bool>(type: "boolean", nullable: false),
                    SameSite = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEssential = table.Column<bool>(type: "boolean", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PrivacyPolicyUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    LastReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastReviewedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CookieDefinitions", x => x.Id);
                    table.CheckConstraint("CK_CookieDefinition_ExpirationDays", "\"ExpirationDays\" IS NULL OR \"ExpirationDays\" > 0");
                    table.CheckConstraint("CK_CookieDefinition_Name_Length", "LENGTH(\"Name\") >= 1");
                });

            migrationBuilder.CreateTable(
                name: "CookiePolicies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresConsent = table.Column<bool>(type: "boolean", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PolicyUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublishedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChangeNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AcceptanceCount = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CookiePolicies", x => x.Id);
                    table.CheckConstraint("CK_CookiePolicy_AcceptanceCount", "\"AcceptanceCount\" >= 0");
                    table.CheckConstraint("CK_CookiePolicy_Version_Length", "LENGTH(\"Version\") >= 1");
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlags",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RolloutPercentage = table.Column<int>(type: "integer", nullable: false),
                    EnabledForUsers = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Industries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ServiceType = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Keywords = table.Column<string>(type: "text", nullable: true),
                    CommonServices = table.Column<string>(type: "text", nullable: true),
                    CommonEquipment = table.Column<string>(type: "text", nullable: true),
                    TypicalIssues = table.Column<string>(type: "text", nullable: true),
                    TechnicalTerms = table.Column<string>(type: "text", nullable: true),
                    SeasonalConsiderations = table.Column<string>(type: "text", nullable: true),
                    EmergencyIndicators = table.Column<string>(type: "text", nullable: true),
                    AverageServiceDuration = table.Column<int>(type: "integer", nullable: true),
                    TypicalPriceRange = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Industries", x => x.Id);
                    table.UniqueConstraint("AK_Industries_ServiceType", x => x.ServiceType);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeBaseArticles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Industry = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    EquipmentType = table.Column<string>(type: "text", nullable: true),
                    EquipmentBrand = table.Column<string>(type: "text", nullable: true),
                    EquipmentModel = table.Column<string>(type: "text", nullable: true),
                    IssueType = table.Column<string>(type: "text", nullable: true),
                    PartNumbers = table.Column<string>(type: "text", nullable: true),
                    Symptoms = table.Column<string>(type: "text", nullable: true),
                    DiagnosisSteps = table.Column<string>(type: "text", nullable: true),
                    RepairSteps = table.Column<string>(type: "text", nullable: true),
                    SafetyWarnings = table.Column<string>(type: "text", nullable: true),
                    RequiredTools = table.Column<string>(type: "text", nullable: true),
                    Complexity = table.Column<string>(type: "text", nullable: true),
                    EstimatedTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    SkillLevel = table.Column<string>(type: "text", nullable: true),
                    TypicalPartsCost = table.Column<decimal>(type: "numeric", nullable: true),
                    TypicalLaborCost = table.Column<decimal>(type: "numeric", nullable: true),
                    UseCount = table.Column<int>(type: "integer", nullable: false),
                    Relevance = table.Column<double>(type: "double precision", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedBy = table.Column<string>(type: "text", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Author = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBaseArticles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartsCatalog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PartNumber = table.Column<string>(type: "text", nullable: false),
                    PartName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Industry = table.Column<int>(type: "integer", nullable: false),
                    EquipmentType = table.Column<string>(type: "text", nullable: false),
                    PartCategory = table.Column<string>(type: "text", nullable: false),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    CompatibleBrands = table.Column<List<string>>(type: "text[]", nullable: false),
                    CompatibleModels = table.Column<List<string>>(type: "text[]", nullable: false),
                    AlternativePartNumbers = table.Column<List<string>>(type: "text[]", nullable: false),
                    SupersededBy = table.Column<string>(type: "text", nullable: true),
                    Supersedes = table.Column<string>(type: "text", nullable: true),
                    TechnicalSpecs = table.Column<string>(type: "text", nullable: true),
                    Dimensions = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<double>(type: "double precision", nullable: true),
                    WholesaleCost = table.Column<decimal>(type: "numeric", nullable: false),
                    RetailPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SuggestedMarkup = table.Column<decimal>(type: "numeric", nullable: false),
                    QuantityInStock = table.Column<int>(type: "integer", nullable: true),
                    ReorderLevel = table.Column<int>(type: "integer", nullable: true),
                    PreferredSupplier = table.Column<string>(type: "text", nullable: true),
                    SupplierPartNumber = table.Column<string>(type: "text", nullable: true),
                    InstallationNotes = table.Column<string>(type: "text", nullable: true),
                    WarrantyInformation = table.Column<string>(type: "text", nullable: true),
                    ExpectedLifespanMonths = table.Column<int>(type: "integer", nullable: true),
                    RecommendationCount = table.Column<int>(type: "integer", nullable: false),
                    AverageCustomerSatisfaction = table.Column<double>(type: "double precision", nullable: false),
                    IsPreferredPart = table.Column<bool>(type: "boolean", nullable: false),
                    CommonalityScore = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDiscontinued = table.Column<bool>(type: "boolean", nullable: false),
                    AvailabilityStatus = table.Column<string>(type: "text", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartsCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhoneVerifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OtpHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationToken = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    VerificationTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    IsConsumed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneVerifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductKnowledge",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Topic = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    ShortAnswer = table.Column<string>(type: "text", nullable: true),
                    Keywords = table.Column<List<string>>(type: "text[]", nullable: false),
                    RelatedTopics = table.Column<List<string>>(type: "text[]", nullable: false),
                    AlternativeQuestions = table.Column<List<string>>(type: "text[]", nullable: false),
                    UserType = table.Column<string>(type: "text", nullable: true),
                    Workflow = table.Column<string>(type: "text", nullable: true),
                    Prerequisites = table.Column<string>(type: "text", nullable: true),
                    ExampleScenario = table.Column<string>(type: "text", nullable: true),
                    CodeExample = table.Column<string>(type: "text", nullable: true),
                    VideoUrl = table.Column<string>(type: "text", nullable: true),
                    DocumentationUrl = table.Column<string>(type: "text", nullable: true),
                    Importance = table.Column<int>(type: "integer", nullable: false),
                    IsCommonQuestion = table.Column<bool>(type: "boolean", nullable: false),
                    TimesReferenced = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceThreshold = table.Column<double>(type: "double precision", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: true),
                    IsCurrentVersion = table.Column<bool>(type: "boolean", nullable: false),
                    DeprecatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductKnowledge", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromoCodes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TrialDurationDays = table.Column<int>(type: "integer", nullable: false),
                    TrialMinutesLimit = table.Column<int>(type: "integer", nullable: false),
                    MaxUses = table.Column<int>(type: "integer", nullable: true),
                    UsedCount = table.Column<int>(type: "integer", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EnabledFeatures = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemStatuses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    EventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EventName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RawPayload = table.Column<string>(type: "text", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    HashedPassword = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    BusinessName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Industry = table.Column<string>(type: "text", nullable: false),
                    IndustrySpecialization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Plan = table.Column<string>(type: "text", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BonusMinutes = table.Column<int>(type: "integer", nullable: false),
                    VoiceMinutesUsed = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppMessagesUsed = table.Column<int>(type: "integer", nullable: false),
                    BonusWhatsAppMessages = table.Column<int>(type: "integer", nullable: false),
                    RolloverVoiceMinutes = table.Column<int>(type: "integer", nullable: false),
                    RolloverWhatsAppMessages = table.Column<int>(type: "integer", nullable: false),
                    RolloverExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PromoCodeUsed = table.Column<string>(type: "text", nullable: true),
                    IsTrial = table.Column<bool>(type: "boolean", nullable: false),
                    TrialStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialMinutesLimit = table.Column<int>(type: "integer", nullable: true),
                    TrialDurationDays = table.Column<int>(type: "integer", nullable: true),
                    TrialType = table.Column<string>(type: "text", nullable: true),
                    TrialAssignedByAdminId = table.Column<string>(type: "text", nullable: true),
                    TrialNotes = table.Column<string>(type: "text", nullable: true),
                    CookieConsent = table.Column<string>(type: "text", nullable: true),
                    CookieConsentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "text", nullable: true),
                    PasswordResetExpires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotificationSettings = table.Column<string>(type: "text", nullable: true),
                    SecuritySettings = table.Column<string>(type: "text", nullable: true),
                    GoogleCalendarToken = table.Column<string>(type: "text", nullable: true),
                    GoogleCalendarConnected = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordMustChange = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TemporaryPassword = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByAdminId = table.Column<string>(type: "text", nullable: true),
                    AssignedSalesRepId = table.Column<string>(type: "text", nullable: true),
                    MonthlyMinutesLimit = table.Column<int>(type: "integer", nullable: true),
                    CustomWhatsAppLimit = table.Column<int>(type: "integer", nullable: true),
                    CustomSmsLimit = table.Column<int>(type: "integer", nullable: true),
                    CustomMonthlyFee = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomPerMinuteRate = table.Column<decimal>(type: "numeric", nullable: true),
                    SetupFee = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomVoiceMinuteRate = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomTransferMinuteRate = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomWhatsAppMessageRate = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomSmsMessageRate = table.Column<decimal>(type: "numeric", nullable: true),
                    EnabledFeatures = table.Column<string>(type: "text", nullable: true),
                    AccountManagerNotes = table.Column<string>(type: "text", nullable: true),
                    BillingNotes = table.Column<string>(type: "text", nullable: true),
                    InvoiceHistory = table.Column<string>(type: "text", nullable: true),
                    OnboardingCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    OnboardingStep = table.Column<int>(type: "integer", nullable: false),
                    TotpSecret = table.Column<string>(type: "text", nullable: true),
                    TotpEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TotpBackupCodes = table.Column<string>(type: "text", nullable: true),
                    EnabledLanguages = table.Column<string>(type: "text", nullable: true),
                    AssignedCountries = table.Column<string>(type: "text", nullable: true),
                    BlockReason = table.Column<string>(type: "text", nullable: true),
                    BlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BlockedByAdminId = table.Column<string>(type: "text", nullable: true),
                    AiAgentConfig = table.Column<string>(type: "text", nullable: true),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    ConfigUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfigUpdatedByAdminId = table.Column<string>(type: "text", nullable: true),
                    PricingNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_User_BusinessName_Length", "LENGTH(\"BusinessName\") >= 2");
                    table.CheckConstraint("CK_User_Email_Format", "\"Email\" ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'");
                    table.CheckConstraint("CK_User_Name_Length", "LENGTH(\"Name\") >= 2");
                    table.ForeignKey(
                        name: "FK_Users_Industries_Industry",
                        column: x => x.Industry,
                        principalTable: "Industries",
                        principalColumn: "ServiceType",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    GreetingMessage = table.Column<string>(type: "text", nullable: false),
                    ClosingMessage = table.Column<string>(type: "text", nullable: false),
                    ServicesList = table.Column<string>(type: "text", nullable: false),
                    BusinessHours = table.Column<string>(type: "text", nullable: false),
                    VoicemailMessage = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Tone = table.Column<string>(type: "text", nullable: false),
                    HumanBackupNumber = table.Column<string>(type: "text", nullable: true),
                    TransferOnComplexIssues = table.Column<bool>(type: "boolean", nullable: false),
                    CustomInstructions = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Scopes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedIp = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: true),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CouponCodes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrialDays = table.Column<int>(type: "integer", nullable: false),
                    AIMinutes = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppMessagesLimit = table.Column<int>(type: "integer", nullable: false),
                    SmsMessagesLimit = table.Column<int>(type: "integer", nullable: false),
                    ImagesLimit = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumbersAllowed = table.Column<int>(type: "integer", nullable: false),
                    MaxUses = table.Column<int>(type: "integer", nullable: true),
                    TimesUsed = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByAdminId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CustomPriceOverride = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomPriceDurationMonths = table.Column<int>(type: "integer", nullable: true),
                    CustomPriceReason = table.Column<string>(type: "text", nullable: true),
                    IsFounderCustomer = table.Column<bool>(type: "boolean", nullable: false),
                    FounderNumber = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
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
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    ZipCode = table.Column<string>(type: "text", nullable: true),
                    LeadStatus = table.Column<string>(type: "text", nullable: false),
                    LeadScore = table.Column<int>(type: "integer", nullable: false),
                    LeadQualification = table.Column<string>(type: "text", nullable: false),
                    TotalSpend = table.Column<decimal>(type: "numeric", nullable: false),
                    JobCount = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: true),
                    ConvertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    LastContactedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastServiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentMethod = table.Column<string>(type: "text", nullable: true),
                    PaymentReference = table.Column<string>(type: "text", nullable: true),
                    PdfUrl = table.Column<string>(type: "text", nullable: true),
                    EmailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.CheckConstraint("CK_Invoice_Period_Valid", "\"PeriodEnd\" >= \"PeriodStart\"");
                    table.CheckConstraint("CK_Invoice_Subtotal_Positive", "\"Subtotal\" >= 0");
                    table.CheckConstraint("CK_Invoice_Tax_Positive", "\"Tax\" >= 0");
                    table.CheckConstraint("CK_Invoice_Total_Positive", "\"Total\" >= 0");
                    table.ForeignKey(
                        name: "FK_Invoices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeDocuments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    VectorEmbedding = table.Column<string>(type: "text", nullable: true),
                    LastProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeDocuments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyUsageArchives",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    BillingMonth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Plan = table.Column<string>(type: "text", nullable: false),
                    AIMinutesUsed = table.Column<int>(type: "integer", nullable: false),
                    AIMinutesIncluded = table.Column<int>(type: "integer", nullable: false),
                    AIMinutesOverage = table.Column<int>(type: "integer", nullable: false),
                    AIOverageRate = table.Column<decimal>(type: "numeric", nullable: false),
                    AIOverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    WhatsAppMessagesUsed = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppMessagesIncluded = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppMessagesOverage = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppOverageRate = table.Column<decimal>(type: "numeric", nullable: false),
                    WhatsAppOverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    SMSMessagesUsed = table.Column<int>(type: "integer", nullable: false),
                    SMSMessagesIncluded = table.Column<int>(type: "integer", nullable: false),
                    SMSMessagesOverage = table.Column<int>(type: "integer", nullable: false),
                    SMSOverageRate = table.Column<decimal>(type: "numeric", nullable: false),
                    SMSOverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    ImagesUsed = table.Column<int>(type: "integer", nullable: false),
                    ImagesIncluded = table.Column<int>(type: "integer", nullable: false),
                    ImagesOverage = table.Column<int>(type: "integer", nullable: false),
                    ImagesOverageRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ImagesOverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalOverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCalls = table.Column<int>(type: "integer", nullable: false),
                    TotalCustomersContacted = table.Column<int>(type: "integer", nullable: false),
                    SummaryEmailSent = table.Column<bool>(type: "boolean", nullable: false),
                    SummaryEmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsageReset = table.Column<bool>(type: "boolean", nullable: false),
                    UsageResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyUsageArchives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyUsageArchives_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActionUrl = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhoneNumbers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    TwilioSid = table.Column<string>(type: "text", nullable: true),
                    CountryCode = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    VoiceEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsAppVerified = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignmentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneNumbers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromoCodeUsages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PromoCodeId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodeUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromoCodeUsages_PromoCodes_PromoCodeId",
                        column: x => x.PromoCodeId,
                        principalTable: "PromoCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromoCodeUsages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false),
                    DeviceName = table.Column<string>(type: "text", nullable: true),
                    DeviceType = table.Column<string>(type: "text", nullable: true),
                    Browser = table.Column<string>(type: "text", nullable: true),
                    Os = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    LastActive = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReminderSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    HoursBeforeAppointment = table.Column<int>(type: "integer", nullable: false),
                    SendSms = table.Column<bool>(type: "boolean", nullable: false),
                    SendEmail = table.Column<bool>(type: "boolean", nullable: false),
                    SendVoiceCall = table.Column<bool>(type: "boolean", nullable: false),
                    CustomMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReminderSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    MaxMembers = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Technicians",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Skills = table.Column<string>(type: "text", nullable: false),
                    ServiceAreas = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentLatitude = table.Column<double>(type: "double precision", nullable: true),
                    CurrentLongitude = table.Column<double>(type: "double precision", nullable: true),
                    LastLocationUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    JobsCompleted = table.Column<int>(type: "integer", nullable: false),
                    AverageRating = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
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
                    CompanyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NumberOfTechnicians = table.Column<int>(type: "integer", nullable: false),
                    RequestedReason = table.Column<string>(type: "text", nullable: false),
                    ReferralSource = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    AdminNotes = table.Column<string>(type: "text", nullable: false),
                    ApprovedByAdminId = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedByAdminId = table.Column<string>(type: "text", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    ConvertedUserId = table.Column<string>(type: "text", nullable: true),
                    ConvertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HasConverted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
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
                name: "UsageCosts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    BillingPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AiCosts = table.Column<decimal>(type: "numeric", nullable: false),
                    TwilioCosts = table.Column<decimal>(type: "numeric", nullable: false),
                    SendGridCosts = table.Column<decimal>(type: "numeric", nullable: false),
                    StorageCosts = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCosts = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCalls = table.Column<int>(type: "integer", nullable: false),
                    TotalMinutes = table.Column<int>(type: "integer", nullable: false),
                    TotalSms = table.Column<int>(type: "integer", nullable: false),
                    TotalEmails = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageCosts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsageWarnings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ResourceType = table.Column<string>(type: "text", nullable: false),
                    PercentageUsed = table.Column<int>(type: "integer", nullable: false),
                    AmountUsed = table.Column<int>(type: "integer", nullable: false),
                    AmountLimit = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    EmailSent = table.Column<bool>(type: "boolean", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InAppNotificationCreated = table.Column<bool>(type: "boolean", nullable: false),
                    Acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserPlan = table.Column<string>(type: "text", nullable: true),
                    BillingMonth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageWarnings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageWarnings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAdmins",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedByAdminId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AssignedSalesRepId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AccountManagerNotes = table.Column<string>(type: "text", nullable: true),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    BlockReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BlockedByAdminId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAdmins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAdmins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAuths",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    HashedPassword = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordResetToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PasswordResetExpires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordMustChange = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TemporaryPassword = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotpSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TotpEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TotpBackupCodes = table.Column<string>(type: "jsonb", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockoutEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SecuritySettings = table.Column<string>(type: "jsonb", nullable: true),
                    EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    EmailVerificationToken = table.Column<string>(type: "text", nullable: true),
                    EmailVerificationTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAuths_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCookieConsents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConsentToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PolicyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Consented = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    ConsentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    WithdrawnAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferrerUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCookieConsents", x => x.Id);
                    table.CheckConstraint("CK_UserCookieConsent_UserOrToken", "\"UserId\" IS NOT NULL OR \"ConsentToken\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_UserCookieConsents_CookiePolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "CookiePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCookieConsents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BusinessName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Industry = table.Column<string>(type: "text", nullable: false),
                    IndustrySpecialization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OnboardingCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    OnboardingStep = table.Column<int>(type: "integer", nullable: false),
                    GoogleCalendarToken = table.Column<string>(type: "text", nullable: true),
                    GoogleCalendarConnected = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSettings = table.Column<string>(type: "jsonb", nullable: true),
                    EnabledLanguages = table.Column<string>(type: "jsonb", nullable: true),
                    AssignedCountries = table.Column<string>(type: "jsonb", nullable: true),
                    AiAgentConfig = table.Column<string>(type: "jsonb", nullable: true),
                    ConfigUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfigUpdatedByAdminId = table.Column<string>(type: "text", nullable: true),
                    CallerRecognitionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CookieConsent = table.Column<string>(type: "jsonb", nullable: true),
                    CookieConsentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Plan = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EnabledFeatures = table.Column<string>(type: "jsonb", nullable: true),
                    MaxPhoneNumbers = table.Column<int>(type: "integer", nullable: false),
                    RequiresPayment = table.Column<bool>(type: "boolean", nullable: false),
                    HasActivePayment = table.Column<bool>(type: "boolean", nullable: false),
                    SelectedPlanAtRegistration = table.Column<string>(type: "text", nullable: true),
                    StripeCustomerId = table.Column<string>(type: "text", nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "text", nullable: true),
                    LemonSqueezySubscriptionId = table.Column<string>(type: "text", nullable: true),
                    LemonSqueezyCustomerId = table.Column<string>(type: "text", nullable: true),
                    LemonSqueezyVariantId = table.Column<string>(type: "text", nullable: true),
                    AIMinutesUsedThisMonth = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppMessagesUsedThisMonth = table.Column<int>(type: "integer", nullable: false),
                    SMSMessagesUsedThisMonth = table.Column<int>(type: "integer", nullable: false),
                    ImagesUsedThisMonth = table.Column<int>(type: "integer", nullable: false),
                    VoiceMinutesUsed = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppMessagesUsed = table.Column<int>(type: "integer", nullable: false),
                    BonusMinutes = table.Column<int>(type: "integer", nullable: false),
                    BonusWhatsAppMessages = table.Column<int>(type: "integer", nullable: false),
                    RolloverVoiceMinutes = table.Column<int>(type: "integer", nullable: false),
                    RolloverWhatsAppMessages = table.Column<int>(type: "integer", nullable: false),
                    RolloverExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AIMinutesIncluded = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppMessagesIncluded = table.Column<int>(type: "integer", nullable: false),
                    SMSMessagesIncluded = table.Column<int>(type: "integer", nullable: false),
                    ImagesIncluded = table.Column<int>(type: "integer", nullable: false),
                    MonthlyMinutesLimit = table.Column<int>(type: "integer", nullable: true),
                    CustomWhatsAppLimit = table.Column<int>(type: "integer", nullable: true),
                    CustomSmsLimit = table.Column<int>(type: "integer", nullable: true),
                    AIOverageRate = table.Column<decimal>(type: "numeric", nullable: false),
                    WhatsAppOverageRate = table.Column<decimal>(type: "numeric", nullable: false),
                    SMSOverageRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ImageOverageRate = table.Column<decimal>(type: "numeric", nullable: false),
                    BillingCycleStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextBillingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OverageChargesThisMonth = table.Column<decimal>(type: "numeric", nullable: false),
                    EnforceHardLimits = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CustomPriceOverride = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomPriceDurationMonths = table.Column<int>(type: "integer", nullable: true),
                    CustomPriceExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomPriceReason = table.Column<string>(type: "text", nullable: true),
                    IsFounderCustomer = table.Column<bool>(type: "boolean", nullable: false),
                    FounderNumber = table.Column<int>(type: "integer", nullable: true),
                    IsTemporarilySuspended = table.Column<bool>(type: "boolean", nullable: false),
                    SuspendedReason = table.Column<string>(type: "text", nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspendedByAdminId = table.Column<string>(type: "text", nullable: true),
                    CustomMonthlyFee = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomPerMinuteRate = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomVoiceMinuteRate = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomTransferMinuteRate = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomWhatsAppMessageRate = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomSmsMessageRate = table.Column<decimal>(type: "numeric", nullable: true),
                    SetupFee = table.Column<decimal>(type: "numeric", nullable: true),
                    BillingNotes = table.Column<string>(type: "text", nullable: true),
                    InvoiceHistory = table.Column<string>(type: "jsonb", nullable: true),
                    PricingNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTrials",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsTrial = table.Column<bool>(type: "boolean", nullable: false),
                    TrialStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialMinutesLimit = table.Column<int>(type: "integer", nullable: true),
                    TrialDurationDays = table.Column<int>(type: "integer", nullable: true),
                    TrialType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PromoCodeUsed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrialAssignedByAdminId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TrialNotes = table.Column<string>(type: "text", nullable: true),
                    LastExpirationWarningAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastWarningDaysRemaining = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTrials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTrials_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoiceAgents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    VoiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaxCallDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoiceAgents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppContexts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerPhone = table.Column<string>(type: "text", nullable: false),
                    ConversationContext = table.Column<string>(type: "text", nullable: false),
                    LastInteraction = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    CurrentIntent = table.Column<string>(type: "text", nullable: true),
                    SessionData = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppContexts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppFunctionCalls",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerPhone = table.Column<string>(type: "text", nullable: false),
                    FunctionName = table.Column<string>(type: "text", nullable: false),
                    Arguments = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: true),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ExecutionTimeMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppFunctionCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppFunctionCalls_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppScheduledMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerPhone = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSent = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MessageSid = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppScheduledMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppScheduledMessages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppVoiceNotes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerPhone = table.Column<string>(type: "text", nullable: false),
                    MediaUrl = table.Column<string>(type: "text", nullable: false),
                    TranscriptText = table.Column<string>(type: "text", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsTranscribed = table.Column<bool>(type: "boolean", nullable: false),
                    TranscribedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppVoiceNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppVoiceNotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CouponUsages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CouponId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TrialDaysGranted = table.Column<int>(type: "integer", nullable: false),
                    AIMinutesGranted = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppMessagesGranted = table.Column<int>(type: "integer", nullable: false),
                    SmsMessagesGranted = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
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
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<string>(type: "text", nullable: false),
                    TechnicianId = table.Column<string>(type: "text", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ServiceType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric", nullable: false),
                    ActualCost = table.Column<decimal>(type: "numeric", nullable: true),
                    GoogleCalendarEventId = table.Column<string>(type: "text", nullable: true),
                    ReminderSent24h = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderSent2h = table.Column<bool>(type: "boolean", nullable: false),
                    Reminder24hSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reminder2hSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecurrenceRule = table.Column<string>(type: "text", nullable: true),
                    ParentAppointmentId = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CompletionNotes = table.Column<string>(type: "text", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    ReminderSent = table.Column<bool>(type: "boolean", nullable: false),
                    IssueId = table.Column<string>(type: "text", nullable: true),
                    IsIssueResolved = table.Column<bool>(type: "boolean", nullable: false),
                    IsFirstVisit = table.Column<bool>(type: "boolean", nullable: false),
                    IssueResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<string>(type: "text", nullable: false),
                    QuoteNumber = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<string>(type: "text", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PdfUrl = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.CheckConstraint("CK_Quote_Subtotal_Positive", "\"Subtotal\" >= 0");
                    table.CheckConstraint("CK_Quote_Tax_Positive", "\"Tax\" >= 0");
                    table.CheckConstraint("CK_Quote_Total_Positive", "\"Total\" >= 0");
                    table.ForeignKey(
                        name: "FK_Quotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SMSConversations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<string>(type: "text", nullable: true),
                    CustomerPhone = table.Column<string>(type: "text", nullable: false),
                    BusinessPhone = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMSConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SMSConversations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SMSConversations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppConversations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<string>(type: "text", nullable: true),
                    CustomerPhone = table.Column<string>(type: "text", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    BusinessPhone = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    UnreadCount = table.Column<int>(type: "integer", nullable: false),
                    AiMode = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppConversations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WhatsAppConversations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLineItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    InvoiceId = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItems", x => x.Id);
                    table.CheckConstraint("CK_InvoiceLineItem_Quantity_Positive", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_InvoiceLineItem_UnitPrice_Positive", "\"UnitPrice\" >= 0");
                    table.ForeignKey(
                        name: "FK_InvoiceLineItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OverageInvoices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ArchiveId = table.Column<string>(type: "text", nullable: false),
                    BillingPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AIOverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    WhatsAppOverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    SMSOverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    ImagesOverageCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    LemonSqueezyInvoiceId = table.Column<string>(type: "text", nullable: true),
                    LemonSqueezyInvoiceUrl = table.Column<string>(type: "text", nullable: true),
                    LemonSqueezyCustomerId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentId = table.Column<string>(type: "text", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverageInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OverageInvoices_MonthlyUsageArchives_ArchiveId",
                        column: x => x.ArchiveId,
                        principalTable: "MonthlyUsageArchives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OverageInvoices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Calls",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberId = table.Column<string>(type: "text", nullable: true),
                    CallerPhone = table.Column<string>(type: "text", nullable: false),
                    CallerName = table.Column<string>(type: "text", nullable: true),
                    RecipientPhone = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: true),
                    Sentiment = table.Column<string>(type: "text", nullable: true),
                    ServiceType = table.Column<string>(type: "text", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric", nullable: false),
                    RecordingSid = table.Column<string>(type: "text", nullable: true),
                    RecordingUrl = table.Column<string>(type: "text", nullable: true),
                    RecordingDuration = table.Column<int>(type: "integer", nullable: true),
                    RecordingStatus = table.Column<string>(type: "text", nullable: true),
                    TwilioCallSid = table.Column<string>(type: "text", nullable: true),
                    TransferredToNumber = table.Column<string>(type: "text", nullable: true),
                    WasTransferred = table.Column<bool>(type: "boolean", nullable: false),
                    TransferDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TranscriptText = table.Column<string>(type: "text", nullable: true),
                    ActionItems = table.Column<string>(type: "text", nullable: true),
                    AppointmentScheduled = table.Column<bool>(type: "boolean", nullable: false),
                    QuoteRequested = table.Column<bool>(type: "boolean", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calls_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Calls_PhoneNumbers_PhoneNumberId",
                        column: x => x.PhoneNumberId,
                        principalTable: "PhoneNumbers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Calls_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamInvites",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TeamId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InvitedByUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    AcceptedByUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamInvites_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamInvites_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TeamId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CookieAuditLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConsentToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConsentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CookieDefinitionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    OldValue = table.Column<string>(type: "jsonb", nullable: true),
                    NewValue = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    RequestUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReferrerUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Details = table.Column<string>(type: "jsonb", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CookieAuditLogs", x => x.Id);
                    table.CheckConstraint("CK_CookieAuditLog_Action_Length", "LENGTH(\"Action\") >= 1");
                    table.CheckConstraint("CK_CookieAuditLog_DurationMs", "\"DurationMs\" IS NULL OR \"DurationMs\" >= 0");
                    table.ForeignKey(
                        name: "FK_CookieAuditLogs_CookieDefinitions_CookieDefinitionId",
                        column: x => x.CookieDefinitionId,
                        principalTable: "CookieDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CookieAuditLogs_UserCookieConsents_ConsentId",
                        column: x => x.ConsentId,
                        principalTable: "UserCookieConsents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CookieAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentReminders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AppointmentId = table.Column<string>(type: "text", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSent = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReminderType = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentReminders_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageAnalyses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<string>(type: "text", nullable: true),
                    AppointmentId = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    ImageSource = table.Column<string>(type: "text", nullable: true),
                    ImageSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ImageType = table.Column<string>(type: "text", nullable: true),
                    AnalysisType = table.Column<string>(type: "text", nullable: false),
                    AnalysisResult = table.Column<string>(type: "text", nullable: true),
                    ExtractedText = table.Column<string>(type: "text", nullable: true),
                    DiagnosisCategory = table.Column<string>(type: "text", nullable: true),
                    IssueIdentified = table.Column<string>(type: "text", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric", nullable: true),
                    ProcessingCost = table.Column<decimal>(type: "numeric", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    BillingMonth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CountsTowardQuota = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ProcessingTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageAnalyses_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ImageAnalyses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ImageAnalyses_Users_UserId",
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
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "QuoteLineItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    QuoteId = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteLineItems", x => x.Id);
                    table.CheckConstraint("CK_QuoteLineItem_Quantity_Positive", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_QuoteLineItem_UnitPrice_Positive", "\"UnitPrice\" >= 0");
                    table.ForeignKey(
                        name: "FK_QuoteLineItems_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SMSMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TwilioMessageSid = table.Column<string>(type: "text", nullable: true),
                    ErrorCode = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMSMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SMSMessages_SMSConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "SMSConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    MediaUrl = table.Column<string>(type: "text", nullable: true),
                    MediaType = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TwilioMessageSid = table.Column<string>(type: "text", nullable: true),
                    ErrorCode = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    AiContext = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_WhatsAppConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "WhatsAppConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CallFollowUps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CallId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<string>(type: "text", nullable: true),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    RecipientPhone = table.Column<string>(type: "text", nullable: false),
                    CallOutcome = table.Column<int>(type: "integer", nullable: true),
                    AppointmentScheduled = table.Column<bool>(type: "boolean", nullable: false),
                    QuoteRequested = table.Column<bool>(type: "boolean", nullable: false),
                    MessageTemplate = table.Column<string>(type: "text", nullable: false),
                    MessageContent = table.Column<string>(type: "text", nullable: false),
                    PersonalizationData = table.Column<string>(type: "text", nullable: true),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DelayMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WhatsAppMessageId = table.Column<string>(type: "text", nullable: true),
                    SMSMessageId = table.Column<string>(type: "text", nullable: true),
                    DeliveryStatus = table.Column<string>(type: "text", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomerResponded = table.Column<bool>(type: "boolean", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseSummary = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallFollowUps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallFollowUps_Calls_CallId",
                        column: x => x.CallId,
                        principalTable: "Calls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CallFollowUps_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CallFollowUps_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CallId = table.Column<string>(type: "text", nullable: false),
                    Speaker = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptEntries_Calls_CallId",
                        column: x => x.CallId,
                        principalTable: "Calls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebSocketMetrics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CallId = table.Column<string>(type: "text", nullable: false),
                    ConnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DisconnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    BytesSent = table.Column<long>(type: "bigint", nullable: false),
                    BytesReceived = table.Column<long>(type: "bigint", nullable: false),
                    MessagesIn = table.Column<int>(type: "integer", nullable: false),
                    MessagesOut = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    AverageLatencyMs = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebSocketMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebSocketMetrics_Calls_CallId",
                        column: x => x.CallId,
                        principalTable: "Calls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WebSocketMetrics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaFiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    OriginalUrl = table.Column<string>(type: "text", nullable: false),
                    StoredPath = table.Column<string>(type: "text", nullable: false),
                    ThumbnailPath = table.Column<string>(type: "text", nullable: true),
                    OriginalFileName = table.Column<string>(type: "text", nullable: true),
                    StoredFileName = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    MediaType = table.Column<string>(type: "text", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    StorageType = table.Column<string>(type: "text", nullable: false),
                    DownloadStatus = table.Column<string>(type: "text", nullable: false),
                    DownloadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DownloadError = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    WhatsAppMessageId = table.Column<string>(type: "text", nullable: true),
                    ConversationId = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "text", nullable: true),
                    VisionAnalysis = table.Column<string>(type: "text", nullable: true),
                    AnalysisConfidence = table.Column<double>(type: "double precision", nullable: true),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    FlagReason = table.Column<string>(type: "text", nullable: true),
                    PublicUrl = table.Column<string>(type: "text", nullable: true),
                    PublicUrlExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaFiles_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MediaFiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaFiles_WhatsAppMessages_WhatsAppMessageId",
                        column: x => x.WhatsAppMessageId,
                        principalTable: "WhatsAppMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfiles_UserId",
                table: "AgentProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversationContexts_ConversationId",
                table: "AIConversationContexts",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversationContexts_ExpiresAt",
                table: "AIConversationContexts",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversationContexts_UserId",
                table: "AIConversationContexts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash",
                table: "ApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_Prefix",
                table: "ApiKeys",
                column: "Prefix");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UserId",
                table: "ApiKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentReminders_AppointmentId",
                table: "AppointmentReminders",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentReminders_ScheduledFor",
                table: "AppointmentReminders",
                column: "ScheduledFor");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CustomerId",
                table: "Appointments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_IssueId",
                table: "Appointments",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_IssueId_IsFirstVisit",
                table: "Appointments",
                columns: new[] { "IssueId", "IsFirstVisit" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScheduledAt",
                table: "Appointments",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status",
                table: "Appointments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status_ScheduledAt",
                table: "Appointments",
                columns: new[] { "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TechnicianId_IsIssueResolved",
                table: "Appointments",
                columns: new[] { "TechnicianId", "IsIssueResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId_ScheduledAt",
                table: "Appointments",
                columns: new[] { "UserId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId_Status",
                table: "Appointments",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType",
                table: "AuditLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_PerformedAt",
                table: "AuditLogs",
                columns: new[] { "EntityType", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PerformedAt",
                table: "AuditLogs",
                column: "PerformedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_PerformedAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CallFollowUps_CallId",
                table: "CallFollowUps",
                column: "CallId");

            migrationBuilder.CreateIndex(
                name: "IX_CallFollowUps_CustomerId",
                table: "CallFollowUps",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CallFollowUps_Status_ScheduledFor",
                table: "CallFollowUps",
                columns: new[] { "Status", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_CallFollowUps_UserId",
                table: "CallFollowUps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_CreatedAt",
                table: "Calls",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_CustomerId",
                table: "Calls",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_PhoneNumberId",
                table: "Calls",
                column: "PhoneNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_UserId",
                table: "Calls",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_UserId_CallerPhone",
                table: "Calls",
                columns: new[] { "UserId", "CallerPhone" });

            migrationBuilder.CreateIndex(
                name: "IX_Calls_UserId_CreatedAt",
                table: "Calls",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CookieAuditLogs_Action",
                table: "CookieAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_CookieAuditLogs_Category",
                table: "CookieAuditLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CookieAuditLogs_ConsentId",
                table: "CookieAuditLogs",
                column: "ConsentId");

            migrationBuilder.CreateIndex(
                name: "IX_CookieAuditLogs_ConsentToken",
                table: "CookieAuditLogs",
                column: "ConsentToken");

            migrationBuilder.CreateIndex(
                name: "IX_CookieAuditLogs_CookieDefinitionId",
                table: "CookieAuditLogs",
                column: "CookieDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CookieAuditLogs_CreatedAt",
                table: "CookieAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CookieAuditLogs_SessionId",
                table: "CookieAuditLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CookieAuditLogs_UserId",
                table: "CookieAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CookieAuditLogs_UserId_Action_CreatedAt",
                table: "CookieAuditLogs",
                columns: new[] { "UserId", "Action", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CookieDefinitions_Category",
                table: "CookieDefinitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CookieDefinitions_IsActive",
                table: "CookieDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CookieDefinitions_IsEssential",
                table: "CookieDefinitions",
                column: "IsEssential");

            migrationBuilder.CreateIndex(
                name: "IX_CookieDefinitions_Name",
                table: "CookieDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CookieDefinitions_Name_Domain",
                table: "CookieDefinitions",
                columns: new[] { "Name", "Domain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CookiePolicies_EffectiveDate",
                table: "CookiePolicies",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_CookiePolicies_IsActive",
                table: "CookiePolicies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CookiePolicies_Language",
                table: "CookiePolicies",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_CookiePolicies_Version",
                table: "CookiePolicies",
                column: "Version",
                unique: true);

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
                columns: new[] { "CouponId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_UsedAt",
                table: "CouponUsages",
                column: "UsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_UserId",
                table: "CouponUsages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UserId",
                table: "Customers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UserId_LeadStatus",
                table: "Customers",
                columns: new[] { "UserId", "LeadStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UserId_Phone",
                table: "Customers",
                columns: new[] { "UserId", "Phone" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_Name",
                table: "FeatureFlags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageAnalyses_AppointmentId",
                table: "ImageAnalyses",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageAnalyses_CustomerId",
                table: "ImageAnalyses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageAnalyses_Status",
                table: "ImageAnalyses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImageAnalyses_UserId_BillingMonth",
                table: "ImageAnalyses",
                columns: new[] { "UserId", "BillingMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_Industries_Code",
                table: "Industries",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Industries_ServiceType",
                table: "Industries",
                column: "ServiceType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItems_InvoiceId",
                table: "InvoiceLineItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PeriodEnd",
                table: "Invoices",
                column: "PeriodEnd");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PeriodStart",
                table: "Invoices",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                table: "Invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_UserId",
                table: "Invoices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_UserId_PeriodStart",
                table: "Invoices",
                columns: new[] { "UserId", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_UserId_Status",
                table: "Invoices",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_IsActive",
                table: "KnowledgeDocuments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_UserId",
                table: "KnowledgeDocuments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_ConversationId",
                table: "MediaFiles",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_ConversationId_CreatedAt",
                table: "MediaFiles",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_CreatedAt",
                table: "MediaFiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_CustomerId",
                table: "MediaFiles",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_DownloadStatus",
                table: "MediaFiles",
                column: "DownloadStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_DownloadStatus_CreatedAt",
                table: "MediaFiles",
                columns: new[] { "DownloadStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_IsProcessed",
                table: "MediaFiles",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_MediaType",
                table: "MediaFiles",
                column: "MediaType");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_UserId",
                table: "MediaFiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_UserId_CreatedAt",
                table: "MediaFiles",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_UserId_MediaType",
                table: "MediaFiles",
                columns: new[] { "UserId", "MediaType" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_WhatsAppMessageId",
                table: "MediaFiles",
                column: "WhatsAppMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyUsageArchives_BillingMonth",
                table: "MonthlyUsageArchives",
                column: "BillingMonth");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyUsageArchives_UserId_BillingMonth",
                table: "MonthlyUsageArchives",
                columns: new[] { "UserId", "BillingMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

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

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumbers_CountryCode",
                table: "PhoneNumbers",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumbers_Number",
                table: "PhoneNumbers",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumbers_UserId",
                table: "PhoneNumbers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumbers_UserId_IsActive",
                table: "PhoneNumbers",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_ExpiresAt",
                table: "PhoneVerifications",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_PhoneNumber",
                table: "PhoneVerifications",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_PhoneNumber_CreatedAt",
                table: "PhoneVerifications",
                columns: new[] { "PhoneNumber", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_VerificationToken",
                table: "PhoneVerifications",
                column: "VerificationToken",
                unique: true,
                filter: "\"VerificationToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_Code",
                table: "PromoCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_IsActive",
                table: "PromoCodes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeUsages_PromoCodeId",
                table: "PromoCodeUsages",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeUsages_UserId",
                table: "PromoCodeUsages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteLineItems_QuoteId",
                table: "QuoteLineItems",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CustomerId",
                table: "Quotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_QuoteNumber",
                table: "Quotes",
                column: "QuoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_UserId",
                table: "Quotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_Revoked",
                table: "RefreshTokens",
                columns: new[] { "UserId", "Revoked" });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderSettings_UserId",
                table: "ReminderSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SMSConversations_CustomerId",
                table: "SMSConversations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SMSConversations_CustomerPhone",
                table: "SMSConversations",
                column: "CustomerPhone");

            migrationBuilder.CreateIndex(
                name: "IX_SMSConversations_UserId",
                table: "SMSConversations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SMSConversations_UserId_BusinessPhone_CustomerPhone",
                table: "SMSConversations",
                columns: new[] { "UserId", "BusinessPhone", "CustomerPhone" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SMSConversations_UserId_CustomerPhone",
                table: "SMSConversations",
                columns: new[] { "UserId", "CustomerPhone" });

            migrationBuilder.CreateIndex(
                name: "IX_SMSMessages_ConversationId",
                table: "SMSMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_SMSMessages_ConversationId_CreatedAt",
                table: "SMSMessages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SMSMessages_CreatedAt",
                table: "SMSMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemStatuses_CheckedAt",
                table: "SystemStatuses",
                column: "CheckedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemStatuses_ServiceName",
                table: "SystemStatuses",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_ExpiresAt",
                table: "TeamInvites",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_InvitedByUserId",
                table: "TeamInvites",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_TeamId_Email",
                table: "TeamInvites",
                columns: new[] { "TeamId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamInvites_Token",
                table: "TeamInvites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId_UserId",
                table: "TeamMembers",
                columns: new[] { "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_UserId",
                table: "TeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_OwnerId",
                table: "Teams",
                column: "OwnerId",
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
                name: "IX_TranscriptEntries_CallId",
                table: "TranscriptEntries",
                column: "CallId");

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

            migrationBuilder.CreateIndex(
                name: "IX_UsageCosts_BillingPeriodStart",
                table: "UsageCosts",
                column: "BillingPeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_UsageCosts_UserId",
                table: "UsageCosts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageWarnings_BillingMonth",
                table: "UsageWarnings",
                column: "BillingMonth");

            migrationBuilder.CreateIndex(
                name: "IX_UsageWarnings_UserId_ResourceType_BillingMonth",
                table: "UsageWarnings",
                columns: new[] { "UserId", "ResourceType", "BillingMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAdmins_AssignedSalesRepId",
                table: "UserAdmins",
                column: "AssignedSalesRepId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAdmins_BlockedAt",
                table: "UserAdmins",
                column: "BlockedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserAdmins_CreatedByAdminId",
                table: "UserAdmins",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAdmins_UserId",
                table: "UserAdmins",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuths_UserId",
                table: "UserAuths",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCookieConsents_Category",
                table: "UserCookieConsents",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_UserCookieConsents_ConsentDate",
                table: "UserCookieConsents",
                column: "ConsentDate");

            migrationBuilder.CreateIndex(
                name: "IX_UserCookieConsents_ConsentToken",
                table: "UserCookieConsents",
                column: "ConsentToken");

            migrationBuilder.CreateIndex(
                name: "IX_UserCookieConsents_ConsentToken_Category_PolicyId",
                table: "UserCookieConsents",
                columns: new[] { "ConsentToken", "Category", "PolicyId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCookieConsents_IsActive",
                table: "UserCookieConsents",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserCookieConsents_PolicyId",
                table: "UserCookieConsents",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCookieConsents_UserId",
                table: "UserCookieConsents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCookieConsents_UserId_Category_PolicyId",
                table: "UserCookieConsents",
                columns: new[] { "UserId", "Category", "PolicyId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Industry",
                table: "Users",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_Plan",
                table: "UserSubscriptions",
                column: "Plan");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_RolloverExpiresAt",
                table: "UserSubscriptions",
                column: "RolloverExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTrials_IsTrial",
                table: "UserTrials",
                column: "IsTrial");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrials_TrialEndsAt",
                table: "UserTrials",
                column: "TrialEndsAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrials_UserId",
                table: "UserTrials",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceAgents_UserId",
                table: "VoiceAgents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceAgents_UserId_IsActive",
                table: "VoiceAgents",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_EventId_Source",
                table: "WebhookEvents",
                columns: new[] { "EventId", "Source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_EventName",
                table: "WebhookEvents",
                column: "EventName");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_ProcessedAt",
                table: "WebhookEvents",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_Source",
                table: "WebhookEvents",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_Success",
                table: "WebhookEvents",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_WebSocketMetrics_CallId",
                table: "WebSocketMetrics",
                column: "CallId");

            migrationBuilder.CreateIndex(
                name: "IX_WebSocketMetrics_UserId",
                table: "WebSocketMetrics",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppContexts_CustomerPhone",
                table: "WhatsAppContexts",
                column: "CustomerPhone");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppContexts_UserId",
                table: "WhatsAppContexts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppConversations_CustomerId",
                table: "WhatsAppConversations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppConversations_CustomerPhone",
                table: "WhatsAppConversations",
                column: "CustomerPhone");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppConversations_UserId",
                table: "WhatsAppConversations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppConversations_UserId_BusinessPhone_CustomerPhone",
                table: "WhatsAppConversations",
                columns: new[] { "UserId", "BusinessPhone", "CustomerPhone" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppConversations_UserId_CustomerPhone",
                table: "WhatsAppConversations",
                columns: new[] { "UserId", "CustomerPhone" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppFunctionCalls_CustomerPhone",
                table: "WhatsAppFunctionCalls",
                column: "CustomerPhone");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppFunctionCalls_UserId",
                table: "WhatsAppFunctionCalls",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_ConversationId",
                table: "WhatsAppMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_ConversationId_CreatedAt",
                table: "WhatsAppMessages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_CreatedAt",
                table: "WhatsAppMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppScheduledMessages_ScheduledFor",
                table: "WhatsAppScheduledMessages",
                column: "ScheduledFor");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppScheduledMessages_UserId",
                table: "WhatsAppScheduledMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppVoiceNotes_CustomerPhone",
                table: "WhatsAppVoiceNotes",
                column: "CustomerPhone");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppVoiceNotes_UserId",
                table: "WhatsAppVoiceNotes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentProfiles");

            migrationBuilder.DropTable(
                name: "AIConversationContexts");

            migrationBuilder.DropTable(
                name: "AITrainingExamples");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "AppointmentReminders");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CallFollowUps");

            migrationBuilder.DropTable(
                name: "CommonIssues");

            migrationBuilder.DropTable(
                name: "CookieAuditLogs");

            migrationBuilder.DropTable(
                name: "CouponUsages");

            migrationBuilder.DropTable(
                name: "FeatureFlags");

            migrationBuilder.DropTable(
                name: "ImageAnalyses");

            migrationBuilder.DropTable(
                name: "InvoiceLineItems");

            migrationBuilder.DropTable(
                name: "KnowledgeBaseArticles");

            migrationBuilder.DropTable(
                name: "KnowledgeDocuments");

            migrationBuilder.DropTable(
                name: "MediaFiles");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OverageInvoices");

            migrationBuilder.DropTable(
                name: "PartsCatalog");

            migrationBuilder.DropTable(
                name: "PhoneVerifications");

            migrationBuilder.DropTable(
                name: "ProductKnowledge");

            migrationBuilder.DropTable(
                name: "PromoCodeUsages");

            migrationBuilder.DropTable(
                name: "QuoteLineItems");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ReminderSettings");

            migrationBuilder.DropTable(
                name: "SMSMessages");

            migrationBuilder.DropTable(
                name: "SystemStatuses");

            migrationBuilder.DropTable(
                name: "TeamInvites");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "TechnicianAssignments");

            migrationBuilder.DropTable(
                name: "TranscriptEntries");

            migrationBuilder.DropTable(
                name: "TrialRequests");

            migrationBuilder.DropTable(
                name: "UsageCosts");

            migrationBuilder.DropTable(
                name: "UsageWarnings");

            migrationBuilder.DropTable(
                name: "UserAdmins");

            migrationBuilder.DropTable(
                name: "UserAuths");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "UserTrials");

            migrationBuilder.DropTable(
                name: "VoiceAgents");

            migrationBuilder.DropTable(
                name: "WebhookEvents");

            migrationBuilder.DropTable(
                name: "WebSocketMetrics");

            migrationBuilder.DropTable(
                name: "WhatsAppContexts");

            migrationBuilder.DropTable(
                name: "WhatsAppFunctionCalls");

            migrationBuilder.DropTable(
                name: "WhatsAppScheduledMessages");

            migrationBuilder.DropTable(
                name: "WhatsAppVoiceNotes");

            migrationBuilder.DropTable(
                name: "CookieDefinitions");

            migrationBuilder.DropTable(
                name: "UserCookieConsents");

            migrationBuilder.DropTable(
                name: "CouponCodes");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "WhatsAppMessages");

            migrationBuilder.DropTable(
                name: "MonthlyUsageArchives");

            migrationBuilder.DropTable(
                name: "PromoCodes");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "SMSConversations");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "Technicians");

            migrationBuilder.DropTable(
                name: "Calls");

            migrationBuilder.DropTable(
                name: "CookiePolicies");

            migrationBuilder.DropTable(
                name: "WhatsAppConversations");

            migrationBuilder.DropTable(
                name: "PhoneNumbers");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Industries");
        }
    }
}
