-- ============================================================================
-- SPEAKHALO DATABASE MIGRATION - PRODUCTION
-- ============================================================================
-- Database: PostgreSQL 14+
-- Description: Complete database schema with security and performance optimizations
-- Created: 2026-02-09
-- ============================================================================

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================================
-- DROP EXISTING TABLES (BE CAREFUL - THIS WILL DELETE ALL DATA)
-- ============================================================================
-- Uncomment the following section only if you want to recreate the entire database
/*
DROP TABLE IF EXISTS "AITrainingExamples" CASCADE;
DROP TABLE IF EXISTS "AIConversationContexts" CASCADE;
DROP TABLE IF EXISTS "ProductKnowledge" CASCADE;
DROP TABLE IF EXISTS "PartsCatalog" CASCADE;
DROP TABLE IF EXISTS "CommonIssues" CASCADE;
DROP TABLE IF EXISTS "KnowledgeBaseArticles" CASCADE;
DROP TABLE IF EXISTS "MediaFiles" CASCADE;
DROP TABLE IF EXISTS "WebhookEvents" CASCADE;
DROP TABLE IF EXISTS "TrialRequests" CASCADE;
DROP TABLE IF EXISTS "ApiKeys" CASCADE;
DROP TABLE IF EXISTS "AuditLogs" CASCADE;
DROP TABLE IF EXISTS "SystemStatuses" CASCADE;
DROP TABLE IF EXISTS "WebSocketMetrics" CASCADE;
DROP TABLE IF EXISTS "UsageCosts" CASCADE;
DROP TABLE IF EXISTS "WhatsAppScheduledMessages" CASCADE;
DROP TABLE IF EXISTS "WhatsAppVoiceNotes" CASCADE;
DROP TABLE IF EXISTS "WhatsAppFunctionCalls" CASCADE;
DROP TABLE IF EXISTS "WhatsAppContexts" CASCADE;
DROP TABLE IF EXISTS "AppointmentReminders" CASCADE;
DROP TABLE IF EXISTS "ReminderSettings" CASCADE;
DROP TABLE IF EXISTS "FeatureFlags" CASCADE;
DROP TABLE IF EXISTS "TechnicianAssignments" CASCADE;
DROP TABLE IF EXISTS "Technicians" CASCADE;
DROP TABLE IF EXISTS "QuoteLineItems" CASCADE;
DROP TABLE IF EXISTS "Quotes" CASCADE;
DROP TABLE IF EXISTS "SMSMessages" CASCADE;
DROP TABLE IF EXISTS "SMSConversations" CASCADE;
DROP TABLE IF EXISTS "KnowledgeDocuments" CASCADE;
DROP TABLE IF EXISTS "AgentProfiles" CASCADE;
DROP TABLE IF EXISTS "Notifications" CASCADE;
DROP TABLE IF EXISTS "PromoCodeUsages" CASCADE;
DROP TABLE IF EXISTS "PromoCodes" CASCADE;
DROP TABLE IF EXISTS "InvoiceLineItems" CASCADE;
DROP TABLE IF EXISTS "Invoices" CASCADE;
DROP TABLE IF EXISTS "WhatsAppMessages" CASCADE;
DROP TABLE IF EXISTS "WhatsAppConversations" CASCADE;
DROP TABLE IF EXISTS "Appointments" CASCADE;
DROP TABLE IF EXISTS "Customers" CASCADE;
DROP TABLE IF EXISTS "TranscriptEntries" CASCADE;
DROP TABLE IF EXISTS "Calls" CASCADE;
DROP TABLE IF EXISTS "PhoneNumbers" CASCADE;
DROP TABLE IF EXISTS "RefreshTokens" CASCADE;
DROP TABLE IF EXISTS "UserAdmin" CASCADE;
DROP TABLE IF EXISTS "UserTrials" CASCADE;
DROP TABLE IF EXISTS "UserSubscriptions" CASCADE;
DROP TABLE IF EXISTS "UserProfiles" CASCADE;
DROP TABLE IF EXISTS "UserAuths" CASCADE;
DROP TABLE IF EXISTS "Users" CASCADE;
DROP TABLE IF EXISTS "Industries" CASCADE;
*/

-- ============================================================================
-- CORE TABLES
-- ============================================================================

-- Industries Table
CREATE TABLE IF NOT EXISTS "Industries" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "ServiceType" VARCHAR(100) NOT NULL UNIQUE,
    "Name" VARCHAR(100) NOT NULL,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Description" VARCHAR(500),
    "DisplayOrder" INTEGER NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "CK_Industry_Name_Length" CHECK (LENGTH("Name") >= 2),
    CONSTRAINT "CK_Industry_Code_Length" CHECK (LENGTH("Code") >= 2)
);

-- Users Table (Core entity - lean structure)
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "Name" VARCHAR(255) NOT NULL,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "Role" VARCHAR(50) NOT NULL DEFAULT 'Owner',
    "IsActive" BOOLEAN NOT NULL DEFAULT true,

    -- Legacy fields (deprecated - kept for backward compatibility)
    "HashedPassword" VARCHAR(255) NOT NULL DEFAULT '',
    "Avatar" VARCHAR(500),
    "BusinessName" VARCHAR(255) NOT NULL DEFAULT '',
    "Industry" VARCHAR(100) NOT NULL DEFAULT '',
    "IndustrySpecialization" VARCHAR(200),
    "Plan" VARCHAR(100),
    "LastLogin" TIMESTAMP WITH TIME ZONE,
    "BonusMinutes" INTEGER NOT NULL DEFAULT 0,
    "VoiceMinutesUsed" INTEGER NOT NULL DEFAULT 0,
    "WhatsAppMessagesUsed" INTEGER NOT NULL DEFAULT 0,
    "BonusWhatsAppMessages" INTEGER NOT NULL DEFAULT 0,
    "RolloverVoiceMinutes" INTEGER NOT NULL DEFAULT 0,
    "RolloverWhatsAppMessages" INTEGER NOT NULL DEFAULT 0,
    "RolloverExpiresAt" TIMESTAMP WITH TIME ZONE,
    "PromoCodeUsed" VARCHAR(100),
    "IsTrial" BOOLEAN NOT NULL DEFAULT false,
    "TrialStartedAt" TIMESTAMP WITH TIME ZONE,
    "TrialEndsAt" TIMESTAMP WITH TIME ZONE,
    "TrialMinutesLimit" INTEGER,
    "TrialDurationDays" INTEGER,
    "TrialType" VARCHAR(50),
    "TrialAssignedByAdminId" VARCHAR(255),
    "TrialNotes" TEXT,
    "CookieConsent" TEXT,
    "CookieConsentAt" TIMESTAMP WITH TIME ZONE,
    "PasswordResetToken" VARCHAR(500),
    "PasswordResetExpires" TIMESTAMP WITH TIME ZONE,
    "NotificationSettings" TEXT,
    "SecuritySettings" TEXT,
    "GoogleCalendarToken" TEXT,
    "GoogleCalendarConnected" BOOLEAN NOT NULL DEFAULT false,
    "PasswordMustChange" BOOLEAN NOT NULL DEFAULT false,
    "PasswordExpiresAt" TIMESTAMP WITH TIME ZONE,
    "TemporaryPassword" BOOLEAN NOT NULL DEFAULT false,
    "PasswordChangedAt" TIMESTAMP WITH TIME ZONE,
    "CreatedByAdminId" VARCHAR(255),
    "AssignedSalesRepId" VARCHAR(255),
    "MonthlyMinutesLimit" INTEGER,
    "CustomWhatsAppLimit" INTEGER,
    "CustomSmsLimit" INTEGER,
    "CustomMonthlyFee" DECIMAL(18,2),
    "CustomPerMinuteRate" DECIMAL(18,4),
    "SetupFee" DECIMAL(18,2),
    "CustomVoiceMinuteRate" DECIMAL(18,4),
    "CustomTransferMinuteRate" DECIMAL(18,4),
    "CustomWhatsAppMessageRate" DECIMAL(18,4),
    "CustomSmsMessageRate" DECIMAL(18,4),
    "EnabledFeatures" TEXT,
    "AccountManagerNotes" TEXT,
    "BillingNotes" TEXT,
    "InvoiceHistory" TEXT,
    "OnboardingCompleted" BOOLEAN NOT NULL DEFAULT false,
    "OnboardingStep" INTEGER NOT NULL DEFAULT 0,
    "TotpSecret" VARCHAR(500),
    "TotpEnabled" BOOLEAN NOT NULL DEFAULT false,
    "TotpBackupCodes" TEXT,
    "EnabledLanguages" TEXT,
    "AssignedCountries" TEXT,
    "BlockReason" VARCHAR(500),
    "BlockedAt" TIMESTAMP WITH TIME ZONE,
    "BlockedByAdminId" VARCHAR(255),
    "AiAgentConfig" TEXT,
    "AdminNotes" TEXT,
    "ConfigUpdatedAt" TIMESTAMP WITH TIME ZONE,
    "ConfigUpdatedByAdminId" VARCHAR(255),
    "PricingNotifiedAt" TIMESTAMP WITH TIME ZONE,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    -- Constraints
    CONSTRAINT "CK_User_Email_Format" CHECK ("Email" ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$'),
    CONSTRAINT "CK_User_Name_Length" CHECK (LENGTH("Name") >= 2),
    CONSTRAINT "CK_User_BusinessName_Length" CHECK (LENGTH("BusinessName") >= 2 OR "BusinessName" = ''),
    CONSTRAINT "CK_User_Role" CHECK ("Role" IN ('Owner', 'Admin', 'User', 'Manager'))
);

-- UserAuths Table (Authentication & Security)
CREATE TABLE IF NOT EXISTS "UserAuths" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL UNIQUE,
    "HashedPassword" VARCHAR(255),
    "LastLogin" TIMESTAMP WITH TIME ZONE,

    -- Password Reset
    "PasswordResetToken" VARCHAR(500),
    "PasswordResetExpires" TIMESTAMP WITH TIME ZONE,
    "PasswordMustChange" BOOLEAN NOT NULL DEFAULT false,
    "PasswordExpiresAt" TIMESTAMP WITH TIME ZONE,
    "TemporaryPassword" BOOLEAN NOT NULL DEFAULT false,
    "PasswordChangedAt" TIMESTAMP WITH TIME ZONE,

    -- Two-Factor Authentication
    "TotpSecret" VARCHAR(500),
    "TotpEnabled" BOOLEAN NOT NULL DEFAULT false,
    "TotpBackupCodes" JSONB,

    -- Security Settings
    "SecuritySettings" JSONB,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_UserAuth_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- UserProfiles Table (Profile & Business Information)
CREATE TABLE IF NOT EXISTS "UserProfiles" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL UNIQUE,
    "Avatar" VARCHAR(500),
    "BusinessName" VARCHAR(255),
    "Industry" VARCHAR(100),
    "IndustrySpecialization" VARCHAR(200),

    -- Onboarding
    "OnboardingCompleted" BOOLEAN NOT NULL DEFAULT false,
    "OnboardingStep" INTEGER NOT NULL DEFAULT 0,

    -- Integrations
    "GoogleCalendarToken" TEXT,
    "GoogleCalendarConnected" BOOLEAN NOT NULL DEFAULT false,

    -- Configuration
    "NotificationSettings" JSONB,
    "EnabledLanguages" JSONB,
    "AssignedCountries" JSONB,
    "AiAgentConfig" JSONB,
    "ConfigUpdatedAt" TIMESTAMP WITH TIME ZONE,
    "ConfigUpdatedByAdminId" VARCHAR(255),

    -- Consent
    "CookieConsent" JSONB,
    "CookieConsentAt" TIMESTAMP WITH TIME ZONE,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_UserProfile_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- UserSubscriptions Table (Billing & Usage Tracking)
CREATE TABLE IF NOT EXISTS "UserSubscriptions" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL UNIQUE,
    "Plan" VARCHAR(100),
    "EnabledFeatures" JSONB,

    -- Usage Tracking
    "VoiceMinutesUsed" INTEGER NOT NULL DEFAULT 0,
    "WhatsAppMessagesUsed" INTEGER NOT NULL DEFAULT 0,
    "BonusMinutes" INTEGER NOT NULL DEFAULT 0,
    "BonusWhatsAppMessages" INTEGER NOT NULL DEFAULT 0,

    -- Rollover
    "RolloverVoiceMinutes" INTEGER NOT NULL DEFAULT 0,
    "RolloverWhatsAppMessages" INTEGER NOT NULL DEFAULT 0,
    "RolloverExpiresAt" TIMESTAMP WITH TIME ZONE,

    -- Limits
    "MonthlyMinutesLimit" INTEGER,
    "CustomWhatsAppLimit" INTEGER,
    "CustomSmsLimit" INTEGER,

    -- Custom Pricing
    "CustomMonthlyFee" DECIMAL(18,2),
    "CustomPerMinuteRate" DECIMAL(18,4),
    "CustomVoiceMinuteRate" DECIMAL(18,4),
    "CustomTransferMinuteRate" DECIMAL(18,4),
    "CustomWhatsAppMessageRate" DECIMAL(18,4),
    "CustomSmsMessageRate" DECIMAL(18,4),
    "SetupFee" DECIMAL(18,2),

    -- Admin Notes
    "BillingNotes" TEXT,
    "InvoiceHistory" JSONB,
    "PricingNotifiedAt" TIMESTAMP WITH TIME ZONE,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_UserSubscription_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_UserSubscription_VoiceMinutes_NonNegative" CHECK ("VoiceMinutesUsed" >= 0),
    CONSTRAINT "CK_UserSubscription_WhatsAppMessages_NonNegative" CHECK ("WhatsAppMessagesUsed" >= 0),
    CONSTRAINT "CK_UserSubscription_Pricing_Positive" CHECK (
        ("CustomMonthlyFee" IS NULL OR "CustomMonthlyFee" >= 0) AND
        ("CustomPerMinuteRate" IS NULL OR "CustomPerMinuteRate" >= 0) AND
        ("SetupFee" IS NULL OR "SetupFee" >= 0)
    )
);

-- UserTrials Table (Trial Information)
CREATE TABLE IF NOT EXISTS "UserTrials" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL UNIQUE,
    "IsTrial" BOOLEAN NOT NULL DEFAULT false,
    "TrialStartedAt" TIMESTAMP WITH TIME ZONE,
    "TrialEndsAt" TIMESTAMP WITH TIME ZONE,
    "TrialMinutesLimit" INTEGER,
    "TrialDurationDays" INTEGER,
    "TrialType" VARCHAR(50),
    "PromoCodeUsed" VARCHAR(100),

    -- Admin Management
    "TrialAssignedByAdminId" VARCHAR(255),
    "TrialNotes" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_UserTrial_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_UserTrial_Dates" CHECK ("TrialEndsAt" IS NULL OR "TrialStartedAt" IS NULL OR "TrialEndsAt" >= "TrialStartedAt")
);

-- UserAdmins Table (Admin Management)
CREATE TABLE IF NOT EXISTS "UserAdmins" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL UNIQUE,
    "CreatedByAdminId" VARCHAR(255),
    "AssignedSalesRepId" VARCHAR(255),
    "AccountManagerNotes" TEXT,
    "AdminNotes" TEXT,

    -- Account Status
    "BlockReason" VARCHAR(500),
    "BlockedAt" TIMESTAMP WITH TIME ZONE,
    "BlockedByAdminId" VARCHAR(255),

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_UserAdmin_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- RefreshTokens Table
CREATE TABLE IF NOT EXISTS "RefreshTokens" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "Token" VARCHAR(500) NOT NULL UNIQUE,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedByIp" VARCHAR(50),
    "Revoked" BOOLEAN NOT NULL DEFAULT false,
    "RevokedAt" TIMESTAMP WITH TIME ZONE,
    "RevokedByIp" VARCHAR(50),
    "ReplacedByToken" VARCHAR(500),

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_RefreshToken_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- PhoneNumbers Table
CREATE TABLE IF NOT EXISTS "PhoneNumbers" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "Number" VARCHAR(50) NOT NULL UNIQUE,
    "FriendlyName" VARCHAR(255),
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CountryCode" VARCHAR(10),
    "Capabilities" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_PhoneNumber_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_PhoneNumber_Format" CHECK ("Number" ~* '^\+?[1-9]\d{1,14}$')
);

-- ============================================================================
-- CUSTOMER & COMMUNICATION TABLES
-- ============================================================================

-- Customers Table
CREATE TABLE IF NOT EXISTS "Customers" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "Name" VARCHAR(255) NOT NULL,
    "Phone" VARCHAR(50),
    "Email" VARCHAR(255),
    "Address" VARCHAR(500),
    "City" VARCHAR(100),
    "State" VARCHAR(100),
    "ZipCode" VARCHAR(20),
    "Country" VARCHAR(100),
    "Notes" TEXT,
    "LeadStatus" VARCHAR(50) NOT NULL DEFAULT 'New',
    "LeadScore" INTEGER,
    "LastContactedAt" TIMESTAMP WITH TIME ZONE,
    "Tags" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_Customer_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_Customer_Email_Format" CHECK ("Email" IS NULL OR "Email" ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$'),
    CONSTRAINT "CK_Customer_Name_Length" CHECK (LENGTH("Name") >= 2)
);

-- Calls Table
CREATE TABLE IF NOT EXISTS "Calls" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "CustomerId" VARCHAR(255),
    "PhoneNumber" VARCHAR(50),
    "Direction" VARCHAR(20) NOT NULL,
    "Status" VARCHAR(50) NOT NULL,
    "Duration" INTEGER NOT NULL DEFAULT 0,
    "RecordingUrl" VARCHAR(500),
    "TranscriptionText" TEXT,
    "Summary" TEXT,
    "Outcome" VARCHAR(50),
    "Sentiment" VARCHAR(50),
    "ServiceType" VARCHAR(100),
    "CallSid" VARCHAR(255),
    "Cost" DECIMAL(18,4),

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_Call_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Call_Customer" FOREIGN KEY ("CustomerId") REFERENCES "Customers"("Id") ON DELETE SET NULL,
    CONSTRAINT "CK_Call_Duration_NonNegative" CHECK ("Duration" >= 0),
    CONSTRAINT "CK_Call_Cost_NonNegative" CHECK ("Cost" IS NULL OR "Cost" >= 0)
);

-- TranscriptEntries Table
CREATE TABLE IF NOT EXISTS "TranscriptEntries" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "CallId" VARCHAR(255) NOT NULL,
    "Speaker" VARCHAR(50) NOT NULL,
    "Text" TEXT NOT NULL,
    "Timestamp" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Confidence" DECIMAL(5,4),

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_TranscriptEntry_Call" FOREIGN KEY ("CallId") REFERENCES "Calls"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_TranscriptEntry_Confidence" CHECK ("Confidence" IS NULL OR ("Confidence" >= 0 AND "Confidence" <= 1))
);

-- Appointments Table
CREATE TABLE IF NOT EXISTS "Appointments" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "CustomerId" VARCHAR(255) NOT NULL,
    "TechnicianId" VARCHAR(255),
    "Title" VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "ScheduledAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Duration" INTEGER NOT NULL DEFAULT 60,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Scheduled',
    "ServiceType" VARCHAR(100),
    "Location" VARCHAR(500),
    "Notes" TEXT,
    "CancellationReason" TEXT,
    "CompletedAt" TIMESTAMP WITH TIME ZONE,

    -- First-Time Fix Rate Tracking
    "IssueId" VARCHAR(255),
    "IsIssueResolved" BOOLEAN NOT NULL DEFAULT false,
    "IsFirstVisit" BOOLEAN NOT NULL DEFAULT true,
    "IssueResolvedAt" TIMESTAMP WITH TIME ZONE,
    "ResolutionNotes" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_Appointment_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Appointment_Customer" FOREIGN KEY ("CustomerId") REFERENCES "Customers"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_Appointment_Duration_Positive" CHECK ("Duration" > 0)
);

-- WhatsAppConversations Table
CREATE TABLE IF NOT EXISTS "WhatsAppConversations" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "CustomerPhone" VARCHAR(50) NOT NULL,
    "CustomerName" VARCHAR(255),
    "LastMessageAt" TIMESTAMP WITH TIME ZONE,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "UnreadCount" INTEGER NOT NULL DEFAULT 0,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_WhatsAppConversation_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- WhatsAppMessages Table
CREATE TABLE IF NOT EXISTS "WhatsAppMessages" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "ConversationId" VARCHAR(255) NOT NULL,
    "MessageSid" VARCHAR(255),
    "Direction" VARCHAR(20) NOT NULL,
    "Body" TEXT,
    "MediaUrl" VARCHAR(500),
    "MediaType" VARCHAR(100),
    "Status" VARCHAR(50) NOT NULL DEFAULT 'sent',
    "ErrorMessage" TEXT,
    "SentAt" TIMESTAMP WITH TIME ZONE,
    "DeliveredAt" TIMESTAMP WITH TIME ZONE,
    "ReadAt" TIMESTAMP WITH TIME ZONE,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_WhatsAppMessage_Conversation" FOREIGN KEY ("ConversationId") REFERENCES "WhatsAppConversations"("Id") ON DELETE CASCADE
);

-- WhatsAppContexts Table
CREATE TABLE IF NOT EXISTS "WhatsAppContexts" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "CustomerPhone" VARCHAR(50) NOT NULL,
    "ContextData" JSONB NOT NULL,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "LastInteractionAt" TIMESTAMP WITH TIME ZONE NOT NULL,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_WhatsAppContext_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- WhatsAppFunctionCalls Table
CREATE TABLE IF NOT EXISTS "WhatsAppFunctionCalls" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "CustomerPhone" VARCHAR(50) NOT NULL,
    "FunctionName" VARCHAR(255) NOT NULL,
    "Arguments" JSONB,
    "Result" JSONB,
    "Success" BOOLEAN NOT NULL DEFAULT false,
    "ErrorMessage" TEXT,
    "ExecutedAt" TIMESTAMP WITH TIME ZONE NOT NULL,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_WhatsAppFunctionCall_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- WhatsAppVoiceNotes Table
CREATE TABLE IF NOT EXISTS "WhatsAppVoiceNotes" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "CustomerPhone" VARCHAR(50) NOT NULL,
    "MediaUrl" VARCHAR(500) NOT NULL,
    "Duration" INTEGER,
    "TranscriptionText" TEXT,
    "TranscriptionStatus" VARCHAR(50) NOT NULL DEFAULT 'pending',
    "ProcessedAt" TIMESTAMP WITH TIME ZONE,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_WhatsAppVoiceNote_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- WhatsAppScheduledMessages Table
CREATE TABLE IF NOT EXISTS "WhatsAppScheduledMessages" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "CustomerPhone" VARCHAR(50) NOT NULL,
    "Body" TEXT NOT NULL,
    "ScheduledFor" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'pending',
    "SentAt" TIMESTAMP WITH TIME ZONE,
    "ErrorMessage" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_WhatsAppScheduledMessage_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- SMSConversations Table
CREATE TABLE IF NOT EXISTS "SMSConversations" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "CustomerPhone" VARCHAR(50) NOT NULL,
    "CustomerName" VARCHAR(255),
    "LastMessageAt" TIMESTAMP WITH TIME ZONE,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "UnreadCount" INTEGER NOT NULL DEFAULT 0,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_SMSConversation_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- SMSMessages Table
CREATE TABLE IF NOT EXISTS "SMSMessages" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "ConversationId" VARCHAR(255) NOT NULL,
    "MessageSid" VARCHAR(255),
    "Direction" VARCHAR(20) NOT NULL,
    "Body" TEXT,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'sent',
    "ErrorMessage" TEXT,
    "SentAt" TIMESTAMP WITH TIME ZONE,
    "DeliveredAt" TIMESTAMP WITH TIME ZONE,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_SMSMessage_Conversation" FOREIGN KEY ("ConversationId") REFERENCES "SMSConversations"("Id") ON DELETE CASCADE
);

-- ============================================================================
-- BILLING & INVOICING TABLES
-- ============================================================================

-- Invoices Table
CREATE TABLE IF NOT EXISTS "Invoices" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "InvoiceNumber" VARCHAR(100) NOT NULL UNIQUE,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'draft',
    "Subtotal" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "Tax" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "Total" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "DueDate" TIMESTAMP WITH TIME ZONE,
    "PaidAt" TIMESTAMP WITH TIME ZONE,
    "PeriodStart" TIMESTAMP WITH TIME ZONE NOT NULL,
    "PeriodEnd" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Notes" TEXT,
    "PaymentMethod" VARCHAR(100),
    "PaymentReference" VARCHAR(255),

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_Invoice_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_Invoice_Subtotal_Positive" CHECK ("Subtotal" >= 0),
    CONSTRAINT "CK_Invoice_Tax_Positive" CHECK ("Tax" >= 0),
    CONSTRAINT "CK_Invoice_Total_Positive" CHECK ("Total" >= 0),
    CONSTRAINT "CK_Invoice_Period_Valid" CHECK ("PeriodEnd" >= "PeriodStart")
);

-- InvoiceLineItems Table
CREATE TABLE IF NOT EXISTS "InvoiceLineItems" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "InvoiceId" VARCHAR(255) NOT NULL,
    "Description" VARCHAR(500) NOT NULL,
    "Quantity" DECIMAL(18,4) NOT NULL,
    "UnitPrice" DECIMAL(18,4) NOT NULL,
    "Amount" DECIMAL(18,2) NOT NULL,
    "ItemType" VARCHAR(100),

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_InvoiceLineItem_Invoice" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_InvoiceLineItem_Quantity_Positive" CHECK ("Quantity" > 0),
    CONSTRAINT "CK_InvoiceLineItem_UnitPrice_Positive" CHECK ("UnitPrice" >= 0)
);

-- Quotes Table
CREATE TABLE IF NOT EXISTS "Quotes" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "CustomerId" VARCHAR(255) NOT NULL,
    "QuoteNumber" VARCHAR(100) NOT NULL UNIQUE,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'draft',
    "Tier" VARCHAR(50) NOT NULL DEFAULT 'Better',
    "Subtotal" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "Tax" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "Total" DECIMAL(18,2) NOT NULL DEFAULT 0,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE,
    "AcceptedAt" TIMESTAMP WITH TIME ZONE,
    "Notes" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_Quote_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Quote_Customer" FOREIGN KEY ("CustomerId") REFERENCES "Customers"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_Quote_Subtotal_Positive" CHECK ("Subtotal" >= 0),
    CONSTRAINT "CK_Quote_Tax_Positive" CHECK ("Tax" >= 0),
    CONSTRAINT "CK_Quote_Total_Positive" CHECK ("Total" >= 0),
    CONSTRAINT "CK_Quote_Tier" CHECK ("Tier" IN ('Good', 'Better', 'Best'))
);

-- QuoteLineItems Table
CREATE TABLE IF NOT EXISTS "QuoteLineItems" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "QuoteId" VARCHAR(255) NOT NULL,
    "Description" VARCHAR(500) NOT NULL,
    "Quantity" DECIMAL(18,4) NOT NULL,
    "UnitPrice" DECIMAL(18,4) NOT NULL,
    "Amount" DECIMAL(18,2) NOT NULL,
    "ItemType" VARCHAR(100),

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_QuoteLineItem_Quote" FOREIGN KEY ("QuoteId") REFERENCES "Quotes"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_QuoteLineItem_Quantity_Positive" CHECK ("Quantity" > 0),
    CONSTRAINT "CK_QuoteLineItem_UnitPrice_Positive" CHECK ("UnitPrice" >= 0)
);

-- PromoCodes Table
CREATE TABLE IF NOT EXISTS "PromoCodes" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "Code" VARCHAR(100) NOT NULL UNIQUE,
    "Description" VARCHAR(500),
    "DiscountType" VARCHAR(50) NOT NULL,
    "DiscountValue" DECIMAL(18,4) NOT NULL,
    "MaxUses" INTEGER,
    "UsedCount" INTEGER NOT NULL DEFAULT 0,
    "ValidFrom" TIMESTAMP WITH TIME ZONE NOT NULL,
    "ValidUntil" TIMESTAMP WITH TIME ZONE NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "MinimumPurchase" DECIMAL(18,2),
    "ApplicablePlans" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "CK_PromoCode_DiscountValue_Positive" CHECK ("DiscountValue" > 0),
    CONSTRAINT "CK_PromoCode_UsedCount_NonNegative" CHECK ("UsedCount" >= 0),
    CONSTRAINT "CK_PromoCode_ValidDates" CHECK ("ValidUntil" >= "ValidFrom")
);

-- PromoCodeUsages Table
CREATE TABLE IF NOT EXISTS "PromoCodeUsages" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "PromoCodeId" VARCHAR(255) NOT NULL,
    "UserId" VARCHAR(255) NOT NULL,
    "UsedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "DiscountApplied" DECIMAL(18,2) NOT NULL,
    "OrderReference" VARCHAR(255),

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_PromoCodeUsage_PromoCode" FOREIGN KEY ("PromoCodeId") REFERENCES "PromoCodes"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PromoCodeUsage_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- ============================================================================
-- TECHNICIAN & SCHEDULING TABLES
-- ============================================================================

-- Technicians Table
CREATE TABLE IF NOT EXISTS "Technicians" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "Name" VARCHAR(255) NOT NULL,
    "Email" VARCHAR(255),
    "Phone" VARCHAR(50) NOT NULL,
    "Skills" TEXT,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "Latitude" DECIMAL(10,8),
    "Longitude" DECIMAL(11,8),
    "CurrentLocation" VARCHAR(500),
    "LastLocationUpdate" TIMESTAMP WITH TIME ZONE,
    "Rating" DECIMAL(3,2),
    "CompletedJobs" INTEGER NOT NULL DEFAULT 0,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_Technician_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_Technician_Latitude" CHECK ("Latitude" IS NULL OR ("Latitude" >= -90 AND "Latitude" <= 90)),
    CONSTRAINT "CK_Technician_Longitude" CHECK ("Longitude" IS NULL OR ("Longitude" >= -180 AND "Longitude" <= 180)),
    CONSTRAINT "CK_Technician_Rating" CHECK ("Rating" IS NULL OR ("Rating" >= 0 AND "Rating" <= 5))
);

-- TechnicianAssignments Table
CREATE TABLE IF NOT EXISTS "TechnicianAssignments" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "AppointmentId" VARCHAR(255) NOT NULL,
    "TechnicianId" VARCHAR(255) NOT NULL,
    "AssignedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Status" VARCHAR(50) NOT NULL DEFAULT 'assigned',
    "CompletedAt" TIMESTAMP WITH TIME ZONE,
    "Notes" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_TechnicianAssignment_Appointment" FOREIGN KEY ("AppointmentId") REFERENCES "Appointments"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TechnicianAssignment_Technician" FOREIGN KEY ("TechnicianId") REFERENCES "Technicians"("Id") ON DELETE CASCADE
);

-- AppointmentReminders Table
CREATE TABLE IF NOT EXISTS "AppointmentReminders" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "AppointmentId" VARCHAR(255) NOT NULL,
    "ReminderType" VARCHAR(50) NOT NULL,
    "ScheduledFor" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'pending',
    "SentAt" TIMESTAMP WITH TIME ZONE,
    "ErrorMessage" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_AppointmentReminder_Appointment" FOREIGN KEY ("AppointmentId") REFERENCES "Appointments"("Id") ON DELETE CASCADE
);

-- ReminderSettings Table
CREATE TABLE IF NOT EXISTS "ReminderSettings" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL UNIQUE,
    "EmailEnabled" BOOLEAN NOT NULL DEFAULT true,
    "SmsEnabled" BOOLEAN NOT NULL DEFAULT true,
    "WhatsAppEnabled" BOOLEAN NOT NULL DEFAULT false,
    "DefaultHoursBefore" INTEGER NOT NULL DEFAULT 24,
    "CustomSettings" JSONB,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_ReminderSettings_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- ============================================================================
-- KNOWLEDGE BASE & AI TABLES
-- ============================================================================

-- KnowledgeDocuments Table
CREATE TABLE IF NOT EXISTS "KnowledgeDocuments" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "Title" VARCHAR(255) NOT NULL,
    "Content" TEXT NOT NULL,
    "DocumentType" VARCHAR(100),
    "FileUrl" VARCHAR(500),
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "Tags" TEXT,
    "Category" VARCHAR(100),

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_KnowledgeDocument_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- KnowledgeBaseArticles Table
CREATE TABLE IF NOT EXISTS "KnowledgeBaseArticles" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "IndustryId" VARCHAR(255) NOT NULL,
    "Title" VARCHAR(255) NOT NULL,
    "Content" TEXT NOT NULL,
    "Category" VARCHAR(100),
    "Tags" TEXT,
    "IsPublished" BOOLEAN NOT NULL DEFAULT false,
    "ViewCount" INTEGER NOT NULL DEFAULT 0,
    "HelpfulCount" INTEGER NOT NULL DEFAULT 0,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_KnowledgeBaseArticle_Industry" FOREIGN KEY ("IndustryId") REFERENCES "Industries"("Id") ON DELETE CASCADE
);

-- CommonIssues Table
CREATE TABLE IF NOT EXISTS "CommonIssues" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "IndustryId" VARCHAR(255) NOT NULL,
    "Title" VARCHAR(255) NOT NULL,
    "Description" TEXT NOT NULL,
    "Symptoms" TEXT,
    "Solutions" TEXT NOT NULL,
    "Difficulty" VARCHAR(50),
    "EstimatedTime" INTEGER,
    "RequiredParts" TEXT,
    "Tags" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_CommonIssue_Industry" FOREIGN KEY ("IndustryId") REFERENCES "Industries"("Id") ON DELETE CASCADE
);

-- PartsCatalog Table
CREATE TABLE IF NOT EXISTS "PartsCatalog" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "IndustryId" VARCHAR(255) NOT NULL,
    "PartNumber" VARCHAR(100) NOT NULL,
    "Name" VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "Manufacturer" VARCHAR(255),
    "Price" DECIMAL(18,2),
    "Currency" VARCHAR(10),
    "AvailabilityStatus" VARCHAR(50),
    "SupplierInfo" TEXT,
    "CompatibleModels" TEXT,
    "Tags" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_PartsCatalog_Industry" FOREIGN KEY ("IndustryId") REFERENCES "Industries"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_PartsCatalog_Price_NonNegative" CHECK ("Price" IS NULL OR "Price" >= 0)
);

-- ProductKnowledge Table
CREATE TABLE IF NOT EXISTS "ProductKnowledge" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "IndustryId" VARCHAR(255) NOT NULL,
    "ProductName" VARCHAR(255) NOT NULL,
    "Manufacturer" VARCHAR(255),
    "ModelNumber" VARCHAR(100),
    "Specifications" TEXT,
    "MaintenanceGuidelines" TEXT,
    "TroubleshootingGuide" TEXT,
    "ManualUrl" VARCHAR(500),
    "Tags" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_ProductKnowledge_Industry" FOREIGN KEY ("IndustryId") REFERENCES "Industries"("Id") ON DELETE CASCADE
);

-- AgentProfiles Table
CREATE TABLE IF NOT EXISTS "AgentProfiles" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "Name" VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "SystemPrompt" TEXT NOT NULL,
    "Voice" VARCHAR(50) NOT NULL DEFAULT 'alloy',
    "Language" VARCHAR(20) NOT NULL DEFAULT 'en-US',
    "Temperature" DECIMAL(3,2) NOT NULL DEFAULT 0.7,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CustomInstructions" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_AgentProfile_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_AgentProfile_Temperature" CHECK ("Temperature" >= 0 AND "Temperature" <= 2)
);

-- AIConversationContexts Table
CREATE TABLE IF NOT EXISTS "AIConversationContexts" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "ConversationId" VARCHAR(255) NOT NULL,
    "ContextData" JSONB NOT NULL,
    "MessageHistory" JSONB,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "LastInteractionAt" TIMESTAMP WITH TIME ZONE NOT NULL,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_AIConversationContext_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- AITrainingExamples Table
CREATE TABLE IF NOT EXISTS "AITrainingExamples" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "Category" VARCHAR(100) NOT NULL,
    "Prompt" TEXT NOT NULL,
    "ExpectedResponse" TEXT NOT NULL,
    "Context" JSONB,
    "IsApproved" BOOLEAN NOT NULL DEFAULT false,
    "Tags" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_AITrainingExample_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- ============================================================================
-- SYSTEM & MONITORING TABLES
-- ============================================================================

-- Notifications Table
CREATE TABLE IF NOT EXISTS "Notifications" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "Title" VARCHAR(255) NOT NULL,
    "Message" TEXT NOT NULL,
    "Type" VARCHAR(50) NOT NULL,
    "IsRead" BOOLEAN NOT NULL DEFAULT false,
    "ReadAt" TIMESTAMP WITH TIME ZONE,
    "ActionUrl" VARCHAR(500),
    "Metadata" JSONB,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_Notification_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- AuditLogs Table
CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255),
    "Action" VARCHAR(255) NOT NULL,
    "EntityType" VARCHAR(255) NOT NULL,
    "EntityId" VARCHAR(255),
    "OldValues" JSONB,
    "NewValues" JSONB,
    "IpAddress" VARCHAR(50),
    "UserAgent" TEXT,
    "PerformedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_AuditLog_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE SET NULL
);

-- ApiKeys Table
CREATE TABLE IF NOT EXISTS "ApiKeys" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "Name" VARCHAR(255) NOT NULL,
    "KeyHash" VARCHAR(255) NOT NULL UNIQUE,
    "Prefix" VARCHAR(20) NOT NULL,
    "Scopes" TEXT,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE,
    "LastUsedAt" TIMESTAMP WITH TIME ZONE,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_ApiKey_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

-- TrialRequests Table
CREATE TABLE IF NOT EXISTS "TrialRequests" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "CompanyName" VARCHAR(255) NOT NULL,
    "ContactName" VARCHAR(255) NOT NULL,
    "Email" VARCHAR(255) NOT NULL,
    "Phone" VARCHAR(50) NOT NULL,
    "Industry" VARCHAR(100) NOT NULL,
    "ReferralSource" VARCHAR(255) NOT NULL,
    "Notes" TEXT,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'pending',
    "ApprovedByAdminId" VARCHAR(255),
    "RejectedByAdminId" VARCHAR(255),
    "ConvertedUserId" VARCHAR(255),
    "AdminNotes" TEXT,
    "ProcessedAt" TIMESTAMP WITH TIME ZONE,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_TrialRequest_ApprovedByAdmin" FOREIGN KEY ("ApprovedByAdminId") REFERENCES "Users"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_TrialRequest_RejectedByAdmin" FOREIGN KEY ("RejectedByAdminId") REFERENCES "Users"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_TrialRequest_ConvertedUser" FOREIGN KEY ("ConvertedUserId") REFERENCES "Users"("Id") ON DELETE SET NULL
);

-- WebhookEvents Table
CREATE TABLE IF NOT EXISTS "WebhookEvents" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "EventId" VARCHAR(255) NOT NULL,
    "EventName" VARCHAR(100) NOT NULL,
    "Source" VARCHAR(50) NOT NULL,
    "RawPayload" TEXT NOT NULL,
    "ProcessedAt" TIMESTAMP WITH TIME ZONE,
    "Success" BOOLEAN,
    "ErrorMessage" VARCHAR(2000),
    "RetryCount" INTEGER NOT NULL DEFAULT 0,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "UQ_WebhookEvent_EventId_Source" UNIQUE ("EventId", "Source")
);

-- FeatureFlags Table
CREATE TABLE IF NOT EXISTS "FeatureFlags" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "Name" VARCHAR(255) NOT NULL UNIQUE,
    "Description" TEXT,
    "IsEnabled" BOOLEAN NOT NULL DEFAULT false,
    "Metadata" JSONB,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255)
);

-- SystemStatuses Table
CREATE TABLE IF NOT EXISTS "SystemStatuses" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "ServiceName" VARCHAR(255) NOT NULL,
    "Status" VARCHAR(50) NOT NULL,
    "ResponseTime" INTEGER,
    "ErrorMessage" TEXT,
    "CheckedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Metadata" JSONB,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255)
);

-- UsageCosts Table
CREATE TABLE IF NOT EXISTS "UsageCosts" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "BillingPeriodStart" TIMESTAMP WITH TIME ZONE NOT NULL,
    "BillingPeriodEnd" TIMESTAMP WITH TIME ZONE NOT NULL,
    "VoiceMinutes" INTEGER NOT NULL DEFAULT 0,
    "VoiceCost" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "WhatsAppMessages" INTEGER NOT NULL DEFAULT 0,
    "WhatsAppCost" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "SmsMessages" INTEGER NOT NULL DEFAULT 0,
    "SmsCost" DECIMAL(18,4) NOT NULL DEFAULT 0,
    "TotalCost" DECIMAL(18,4) NOT NULL DEFAULT 0,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_UsageCosts_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_UsageCosts_Period" CHECK ("BillingPeriodEnd" > "BillingPeriodStart")
);

-- WebSocketMetrics Table
CREATE TABLE IF NOT EXISTS "WebSocketMetrics" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "CallId" VARCHAR(255),
    "EventType" VARCHAR(100) NOT NULL,
    "Duration" INTEGER,
    "DataTransferred" BIGINT,
    "ErrorMessage" TEXT,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_WebSocketMetrics_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WebSocketMetrics_Call" FOREIGN KEY ("CallId") REFERENCES "Calls"("Id") ON DELETE CASCADE
);

-- MediaFiles Table
CREATE TABLE IF NOT EXISTS "MediaFiles" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "WhatsAppMessageId" VARCHAR(255),
    "ConversationId" VARCHAR(255),
    "CustomerId" VARCHAR(255),
    "MediaType" VARCHAR(100) NOT NULL,
    "OriginalUrl" VARCHAR(500),
    "LocalFilePath" VARCHAR(500),
    "StorageType" VARCHAR(50) NOT NULL DEFAULT 'local',
    "FileSize" BIGINT,
    "MimeType" VARCHAR(100),
    "DownloadStatus" VARCHAR(50) NOT NULL DEFAULT 'pending',
    "IsProcessed" BOOLEAN NOT NULL DEFAULT false,
    "ProcessedData" JSONB,
    "Metadata" JSONB,

    -- Base entity fields
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP WITH TIME ZONE,
    "DeletedBy" VARCHAR(255),

    CONSTRAINT "FK_MediaFile_User" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_MediaFile_WhatsAppMessage" FOREIGN KEY ("WhatsAppMessageId") REFERENCES "WhatsAppMessages"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_MediaFile_Customer" FOREIGN KEY ("CustomerId") REFERENCES "Customers"("Id") ON DELETE SET NULL
);

-- ============================================================================
-- INDEXES FOR PERFORMANCE
-- ============================================================================

-- Industries Indexes
CREATE INDEX IF NOT EXISTS "IX_Industries_ServiceType" ON "Industries"("ServiceType");
CREATE INDEX IF NOT EXISTS "IX_Industries_IsActive" ON "Industries"("IsActive");

-- Users Indexes
CREATE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users"("Email");
CREATE INDEX IF NOT EXISTS "IX_Users_Industry" ON "Users"("Industry");
CREATE INDEX IF NOT EXISTS "IX_Users_IsActive" ON "Users"("IsActive");
CREATE INDEX IF NOT EXISTS "IX_Users_Role" ON "Users"("Role");
CREATE INDEX IF NOT EXISTS "IX_Users_IsTrial" ON "Users"("IsTrial");
CREATE INDEX IF NOT EXISTS "IX_Users_IsDeleted" ON "Users"("IsDeleted") WHERE "IsDeleted" = false;

-- UserAuths Indexes
CREATE INDEX IF NOT EXISTS "IX_UserAuths_UserId" ON "UserAuths"("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserAuths_PasswordResetToken" ON "UserAuths"("PasswordResetToken") WHERE "PasswordResetToken" IS NOT NULL;

-- UserProfiles Indexes
CREATE INDEX IF NOT EXISTS "IX_UserProfiles_UserId" ON "UserProfiles"("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserProfiles_Industry" ON "UserProfiles"("Industry");

-- UserSubscriptions Indexes
CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_UserId" ON "UserSubscriptions"("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_Plan" ON "UserSubscriptions"("Plan");
CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_RolloverExpiresAt" ON "UserSubscriptions"("RolloverExpiresAt");

-- UserTrials Indexes
CREATE INDEX IF NOT EXISTS "IX_UserTrials_UserId" ON "UserTrials"("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserTrials_IsTrial" ON "UserTrials"("IsTrial");
CREATE INDEX IF NOT EXISTS "IX_UserTrials_TrialEndsAt" ON "UserTrials"("TrialEndsAt");

-- UserAdmins Indexes
CREATE INDEX IF NOT EXISTS "IX_UserAdmins_UserId" ON "UserAdmins"("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserAdmins_CreatedByAdminId" ON "UserAdmins"("CreatedByAdminId");
CREATE INDEX IF NOT EXISTS "IX_UserAdmins_AssignedSalesRepId" ON "UserAdmins"("AssignedSalesRepId");
CREATE INDEX IF NOT EXISTS "IX_UserAdmins_BlockedAt" ON "UserAdmins"("BlockedAt");

-- RefreshTokens Indexes
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId" ON "RefreshTokens"("UserId");
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_Token" ON "RefreshTokens"("Token");
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_ExpiresAt" ON "RefreshTokens"("ExpiresAt");
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId_Revoked" ON "RefreshTokens"("UserId", "Revoked");

-- PhoneNumbers Indexes
CREATE INDEX IF NOT EXISTS "IX_PhoneNumbers_UserId" ON "PhoneNumbers"("UserId");
CREATE INDEX IF NOT EXISTS "IX_PhoneNumbers_Number" ON "PhoneNumbers"("Number");

-- Customers Indexes
CREATE INDEX IF NOT EXISTS "IX_Customers_UserId" ON "Customers"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Customers_Phone" ON "Customers"("Phone");
CREATE INDEX IF NOT EXISTS "IX_Customers_Email" ON "Customers"("Email");
CREATE INDEX IF NOT EXISTS "IX_Customers_LeadStatus" ON "Customers"("LeadStatus");
CREATE INDEX IF NOT EXISTS "IX_Customers_UserId_LeadStatus" ON "Customers"("UserId", "LeadStatus");

-- Calls Indexes
CREATE INDEX IF NOT EXISTS "IX_Calls_UserId" ON "Calls"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Calls_CustomerId" ON "Calls"("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_Calls_CreatedAt" ON "Calls"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Calls_UserId_CreatedAt" ON "Calls"("UserId", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Calls_CallSid" ON "Calls"("CallSid") WHERE "CallSid" IS NOT NULL;

-- TranscriptEntries Indexes
CREATE INDEX IF NOT EXISTS "IX_TranscriptEntries_CallId" ON "TranscriptEntries"("CallId");

-- Appointments Indexes
CREATE INDEX IF NOT EXISTS "IX_Appointments_UserId" ON "Appointments"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Appointments_CustomerId" ON "Appointments"("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_Appointments_TechnicianId" ON "Appointments"("TechnicianId");
CREATE INDEX IF NOT EXISTS "IX_Appointments_ScheduledAt" ON "Appointments"("ScheduledAt");
CREATE INDEX IF NOT EXISTS "IX_Appointments_Status" ON "Appointments"("Status");
CREATE INDEX IF NOT EXISTS "IX_Appointments_UserId_ScheduledAt" ON "Appointments"("UserId", "ScheduledAt");
CREATE INDEX IF NOT EXISTS "IX_Appointments_UserId_Status" ON "Appointments"("UserId", "Status");
CREATE INDEX IF NOT EXISTS "IX_Appointments_Status_ScheduledAt" ON "Appointments"("Status", "ScheduledAt");
CREATE INDEX IF NOT EXISTS "IX_Appointments_IssueId" ON "Appointments"("IssueId") WHERE "IssueId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_Appointments_TechnicianId_IsIssueResolved" ON "Appointments"("TechnicianId", "IsIssueResolved");
CREATE INDEX IF NOT EXISTS "IX_Appointments_IssueId_IsFirstVisit" ON "Appointments"("IssueId", "IsFirstVisit");

-- WhatsAppConversations Indexes
CREATE INDEX IF NOT EXISTS "IX_WhatsAppConversations_UserId" ON "WhatsAppConversations"("UserId");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppConversations_CustomerPhone" ON "WhatsAppConversations"("CustomerPhone");

-- WhatsAppMessages Indexes
CREATE INDEX IF NOT EXISTS "IX_WhatsAppMessages_ConversationId" ON "WhatsAppMessages"("ConversationId");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppMessages_CreatedAt" ON "WhatsAppMessages"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppMessages_ConversationId_CreatedAt" ON "WhatsAppMessages"("ConversationId", "CreatedAt");

-- WhatsAppContexts Indexes
CREATE INDEX IF NOT EXISTS "IX_WhatsAppContexts_UserId" ON "WhatsAppContexts"("UserId");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppContexts_CustomerPhone" ON "WhatsAppContexts"("CustomerPhone");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppContexts_ExpiresAt" ON "WhatsAppContexts"("ExpiresAt");
CREATE GIN INDEX IF NOT EXISTS "IX_WhatsAppContexts_ContextData" ON "WhatsAppContexts" USING gin("ContextData");

-- WhatsAppFunctionCalls Indexes
CREATE INDEX IF NOT EXISTS "IX_WhatsAppFunctionCalls_UserId" ON "WhatsAppFunctionCalls"("UserId");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppFunctionCalls_CustomerPhone" ON "WhatsAppFunctionCalls"("CustomerPhone");

-- WhatsAppVoiceNotes Indexes
CREATE INDEX IF NOT EXISTS "IX_WhatsAppVoiceNotes_UserId" ON "WhatsAppVoiceNotes"("UserId");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppVoiceNotes_CustomerPhone" ON "WhatsAppVoiceNotes"("CustomerPhone");

-- WhatsAppScheduledMessages Indexes
CREATE INDEX IF NOT EXISTS "IX_WhatsAppScheduledMessages_UserId" ON "WhatsAppScheduledMessages"("UserId");
CREATE INDEX IF NOT EXISTS "IX_WhatsAppScheduledMessages_ScheduledFor" ON "WhatsAppScheduledMessages"("ScheduledFor");

-- SMSConversations Indexes
CREATE INDEX IF NOT EXISTS "IX_SMSConversations_UserId" ON "SMSConversations"("UserId");
CREATE INDEX IF NOT EXISTS "IX_SMSConversations_CustomerPhone" ON "SMSConversations"("CustomerPhone");

-- SMSMessages Indexes
CREATE INDEX IF NOT EXISTS "IX_SMSMessages_ConversationId" ON "SMSMessages"("ConversationId");
CREATE INDEX IF NOT EXISTS "IX_SMSMessages_CreatedAt" ON "SMSMessages"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_SMSMessages_ConversationId_CreatedAt" ON "SMSMessages"("ConversationId", "CreatedAt");

-- Invoices Indexes
CREATE INDEX IF NOT EXISTS "IX_Invoices_UserId" ON "Invoices"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Invoices_InvoiceNumber" ON "Invoices"("InvoiceNumber");
CREATE INDEX IF NOT EXISTS "IX_Invoices_Status" ON "Invoices"("Status");
CREATE INDEX IF NOT EXISTS "IX_Invoices_PeriodStart" ON "Invoices"("PeriodStart");
CREATE INDEX IF NOT EXISTS "IX_Invoices_PeriodEnd" ON "Invoices"("PeriodEnd");
CREATE INDEX IF NOT EXISTS "IX_Invoices_UserId_Status" ON "Invoices"("UserId", "Status");
CREATE INDEX IF NOT EXISTS "IX_Invoices_UserId_PeriodStart" ON "Invoices"("UserId", "PeriodStart");

-- InvoiceLineItems Indexes
CREATE INDEX IF NOT EXISTS "IX_InvoiceLineItems_InvoiceId" ON "InvoiceLineItems"("InvoiceId");

-- Quotes Indexes
CREATE INDEX IF NOT EXISTS "IX_Quotes_UserId" ON "Quotes"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Quotes_CustomerId" ON "Quotes"("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_Quotes_QuoteNumber" ON "Quotes"("QuoteNumber");
CREATE INDEX IF NOT EXISTS "IX_Quotes_Status" ON "Quotes"("Status");

-- QuoteLineItems Indexes
CREATE INDEX IF NOT EXISTS "IX_QuoteLineItems_QuoteId" ON "QuoteLineItems"("QuoteId");

-- PromoCodes Indexes
CREATE INDEX IF NOT EXISTS "IX_PromoCodes_Code" ON "PromoCodes"("Code");
CREATE INDEX IF NOT EXISTS "IX_PromoCodes_IsActive" ON "PromoCodes"("IsActive");

-- PromoCodeUsages Indexes
CREATE INDEX IF NOT EXISTS "IX_PromoCodeUsages_PromoCodeId" ON "PromoCodeUsages"("PromoCodeId");
CREATE INDEX IF NOT EXISTS "IX_PromoCodeUsages_UserId" ON "PromoCodeUsages"("UserId");

-- Technicians Indexes
CREATE INDEX IF NOT EXISTS "IX_Technicians_UserId" ON "Technicians"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Technicians_Phone" ON "Technicians"("Phone");
CREATE INDEX IF NOT EXISTS "IX_Technicians_IsActive" ON "Technicians"("IsActive");

-- TechnicianAssignments Indexes
CREATE INDEX IF NOT EXISTS "IX_TechnicianAssignments_AppointmentId" ON "TechnicianAssignments"("AppointmentId");
CREATE INDEX IF NOT EXISTS "IX_TechnicianAssignments_TechnicianId" ON "TechnicianAssignments"("TechnicianId");

-- AppointmentReminders Indexes
CREATE INDEX IF NOT EXISTS "IX_AppointmentReminders_AppointmentId" ON "AppointmentReminders"("AppointmentId");
CREATE INDEX IF NOT EXISTS "IX_AppointmentReminders_ScheduledFor" ON "AppointmentReminders"("ScheduledFor");

-- ReminderSettings Indexes
CREATE INDEX IF NOT EXISTS "IX_ReminderSettings_UserId" ON "ReminderSettings"("UserId");

-- KnowledgeDocuments Indexes
CREATE INDEX IF NOT EXISTS "IX_KnowledgeDocuments_UserId" ON "KnowledgeDocuments"("UserId");
CREATE INDEX IF NOT EXISTS "IX_KnowledgeDocuments_IsActive" ON "KnowledgeDocuments"("IsActive");

-- KnowledgeBaseArticles Indexes
CREATE INDEX IF NOT EXISTS "IX_KnowledgeBaseArticles_IndustryId" ON "KnowledgeBaseArticles"("IndustryId");
CREATE INDEX IF NOT EXISTS "IX_KnowledgeBaseArticles_IsPublished" ON "KnowledgeBaseArticles"("IsPublished");

-- CommonIssues Indexes
CREATE INDEX IF NOT EXISTS "IX_CommonIssues_IndustryId" ON "CommonIssues"("IndustryId");

-- PartsCatalog Indexes
CREATE INDEX IF NOT EXISTS "IX_PartsCatalog_IndustryId" ON "PartsCatalog"("IndustryId");
CREATE INDEX IF NOT EXISTS "IX_PartsCatalog_PartNumber" ON "PartsCatalog"("PartNumber");

-- ProductKnowledge Indexes
CREATE INDEX IF NOT EXISTS "IX_ProductKnowledge_IndustryId" ON "ProductKnowledge"("IndustryId");

-- AgentProfiles Indexes
CREATE INDEX IF NOT EXISTS "IX_AgentProfiles_UserId" ON "AgentProfiles"("UserId");
CREATE INDEX IF NOT EXISTS "IX_AgentProfiles_IsActive" ON "AgentProfiles"("IsActive");

-- AIConversationContexts Indexes
CREATE INDEX IF NOT EXISTS "IX_AIConversationContexts_UserId" ON "AIConversationContexts"("UserId");
CREATE INDEX IF NOT EXISTS "IX_AIConversationContexts_ConversationId" ON "AIConversationContexts"("ConversationId");
CREATE INDEX IF NOT EXISTS "IX_AIConversationContexts_ExpiresAt" ON "AIConversationContexts"("ExpiresAt");
CREATE GIN INDEX IF NOT EXISTS "IX_AIConversationContexts_ContextData" ON "AIConversationContexts" USING gin("ContextData");

-- AITrainingExamples Indexes
CREATE INDEX IF NOT EXISTS "IX_AITrainingExamples_UserId" ON "AITrainingExamples"("UserId");
CREATE INDEX IF NOT EXISTS "IX_AITrainingExamples_Category" ON "AITrainingExamples"("Category");
CREATE INDEX IF NOT EXISTS "IX_AITrainingExamples_IsApproved" ON "AITrainingExamples"("IsApproved");

-- Notifications Indexes
CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId" ON "Notifications"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Notifications_IsRead" ON "Notifications"("IsRead");
CREATE INDEX IF NOT EXISTS "IX_Notifications_CreatedAt" ON "Notifications"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_IsRead_CreatedAt" ON "Notifications"("UserId", "IsRead", "CreatedAt");
CREATE GIN INDEX IF NOT EXISTS "IX_Notifications_Metadata" ON "Notifications" USING gin("Metadata");

-- AuditLogs Indexes
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId" ON "AuditLogs"("UserId");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_EntityType" ON "AuditLogs"("EntityType");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_PerformedAt" ON "AuditLogs"("PerformedAt");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId_PerformedAt" ON "AuditLogs"("UserId", "PerformedAt");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_EntityType_PerformedAt" ON "AuditLogs"("EntityType", "PerformedAt");
CREATE GIN INDEX IF NOT EXISTS "IX_AuditLogs_OldValues" ON "AuditLogs" USING gin("OldValues");
CREATE GIN INDEX IF NOT EXISTS "IX_AuditLogs_NewValues" ON "AuditLogs" USING gin("NewValues");

-- ApiKeys Indexes
CREATE INDEX IF NOT EXISTS "IX_ApiKeys_UserId" ON "ApiKeys"("UserId");
CREATE INDEX IF NOT EXISTS "IX_ApiKeys_Prefix" ON "ApiKeys"("Prefix");
CREATE INDEX IF NOT EXISTS "IX_ApiKeys_KeyHash" ON "ApiKeys"("KeyHash");

-- TrialRequests Indexes
CREATE INDEX IF NOT EXISTS "IX_TrialRequests_Email" ON "TrialRequests"("Email");
CREATE INDEX IF NOT EXISTS "IX_TrialRequests_Status" ON "TrialRequests"("Status");
CREATE INDEX IF NOT EXISTS "IX_TrialRequests_CreatedAt" ON "TrialRequests"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_TrialRequests_Email_Status" ON "TrialRequests"("Email", "Status");

-- WebhookEvents Indexes
CREATE INDEX IF NOT EXISTS "IX_WebhookEvents_EventId_Source" ON "WebhookEvents"("EventId", "Source");
CREATE INDEX IF NOT EXISTS "IX_WebhookEvents_Source" ON "WebhookEvents"("Source");
CREATE INDEX IF NOT EXISTS "IX_WebhookEvents_EventName" ON "WebhookEvents"("EventName");
CREATE INDEX IF NOT EXISTS "IX_WebhookEvents_ProcessedAt" ON "WebhookEvents"("ProcessedAt");
CREATE INDEX IF NOT EXISTS "IX_WebhookEvents_Success" ON "WebhookEvents"("Success");

-- FeatureFlags Indexes
CREATE INDEX IF NOT EXISTS "IX_FeatureFlags_Name" ON "FeatureFlags"("Name");
CREATE GIN INDEX IF NOT EXISTS "IX_FeatureFlags_Metadata" ON "FeatureFlags" USING gin("Metadata");

-- SystemStatuses Indexes
CREATE INDEX IF NOT EXISTS "IX_SystemStatuses_ServiceName" ON "SystemStatuses"("ServiceName");
CREATE INDEX IF NOT EXISTS "IX_SystemStatuses_CheckedAt" ON "SystemStatuses"("CheckedAt");
CREATE GIN INDEX IF NOT EXISTS "IX_SystemStatuses_Metadata" ON "SystemStatuses" USING gin("Metadata");

-- UsageCosts Indexes
CREATE INDEX IF NOT EXISTS "IX_UsageCosts_UserId" ON "UsageCosts"("UserId");
CREATE INDEX IF NOT EXISTS "IX_UsageCosts_BillingPeriodStart" ON "UsageCosts"("BillingPeriodStart");

-- WebSocketMetrics Indexes
CREATE INDEX IF NOT EXISTS "IX_WebSocketMetrics_UserId" ON "WebSocketMetrics"("UserId");
CREATE INDEX IF NOT EXISTS "IX_WebSocketMetrics_CallId" ON "WebSocketMetrics"("CallId");

-- MediaFiles Indexes
CREATE INDEX IF NOT EXISTS "IX_MediaFiles_UserId" ON "MediaFiles"("UserId");
CREATE INDEX IF NOT EXISTS "IX_MediaFiles_WhatsAppMessageId" ON "MediaFiles"("WhatsAppMessageId");
CREATE INDEX IF NOT EXISTS "IX_MediaFiles_ConversationId" ON "MediaFiles"("ConversationId");
CREATE INDEX IF NOT EXISTS "IX_MediaFiles_CustomerId" ON "MediaFiles"("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_MediaFiles_MediaType" ON "MediaFiles"("MediaType");
CREATE INDEX IF NOT EXISTS "IX_MediaFiles_DownloadStatus" ON "MediaFiles"("DownloadStatus");
CREATE INDEX IF NOT EXISTS "IX_MediaFiles_IsProcessed" ON "MediaFiles"("IsProcessed");
CREATE INDEX IF NOT EXISTS "IX_MediaFiles_CreatedAt" ON "MediaFiles"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_MediaFiles_UserId_CreatedAt" ON "MediaFiles"("UserId", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_MediaFiles_UserId_MediaType" ON "MediaFiles"("UserId", "MediaType");
CREATE INDEX IF NOT EXISTS "IX_MediaFiles_ConversationId_CreatedAt" ON "MediaFiles"("ConversationId", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_MediaFiles_DownloadStatus_CreatedAt" ON "MediaFiles"("DownloadStatus", "CreatedAt");
CREATE GIN INDEX IF NOT EXISTS "IX_MediaFiles_Metadata" ON "MediaFiles" USING gin("Metadata");

-- ============================================================================
-- COMMENTS FOR DOCUMENTATION
-- ============================================================================

COMMENT ON TABLE "Users" IS 'Core user entity with backward-compatible legacy fields';
COMMENT ON TABLE "UserAuths" IS 'Authentication and security data for users';
COMMENT ON TABLE "UserProfiles" IS 'User profile and business information';
COMMENT ON TABLE "UserSubscriptions" IS 'User subscription, billing, and usage tracking';
COMMENT ON TABLE "UserTrials" IS 'User trial information and tracking';
COMMENT ON TABLE "UserAdmins" IS 'Admin management and tracking for users';

-- ============================================================================
-- COMPLETION MESSAGE
-- ============================================================================

DO $$
BEGIN
    RAISE NOTICE '====================================================================';
    RAISE NOTICE 'SPEAKHALO DATABASE MIGRATION COMPLETED SUCCESSFULLY';
    RAISE NOTICE '====================================================================';
    RAISE NOTICE 'Total Tables Created: 48';
    RAISE NOTICE 'Total Indexes Created: 150+';
    RAISE NOTICE 'Security Features: Check constraints, foreign keys, data validation';
    RAISE NOTICE 'Performance Features: Comprehensive indexes including GIN for JSONB';
    RAISE NOTICE '====================================================================';
END $$;
