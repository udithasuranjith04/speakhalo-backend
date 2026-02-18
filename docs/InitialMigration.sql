CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "FeatureFlags" (
    "Id" text NOT NULL,
    "Name" text NOT NULL,
    "Description" text NOT NULL,
    "IsEnabled" boolean NOT NULL,
    "RolloutPercentage" integer NOT NULL,
    "EnabledForUsers" text,
    "Metadata" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_FeatureFlags" PRIMARY KEY ("Id")
);

CREATE TABLE "PromoCodes" (
    "Id" text NOT NULL,
    "Code" text NOT NULL,
    "Description" text NOT NULL,
    "TrialDurationDays" integer NOT NULL,
    "TrialMinutesLimit" integer NOT NULL,
    "MaxUses" integer,
    "UsedCount" integer NOT NULL,
    "ValidFrom" timestamp with time zone,
    "ValidUntil" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "EnabledFeatures" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_PromoCodes" PRIMARY KEY ("Id")
);

CREATE TABLE "SystemStatuses" (
    "Id" text NOT NULL,
    "ServiceName" text NOT NULL,
    "Status" text NOT NULL,
    "CheckedAt" timestamp with time zone NOT NULL,
    "ResponseTimeMs" integer NOT NULL,
    "ErrorMessage" text,
    "Metadata" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_SystemStatuses" PRIMARY KEY ("Id")
);

CREATE TABLE "Users" (
    "Id" text NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "HashedPassword" character varying(255) NOT NULL,
    "Avatar" text,
    "Role" text NOT NULL,
    "BusinessName" character varying(255) NOT NULL,
    "Industry" text NOT NULL,
    "Plan" text,
    "IsActive" boolean NOT NULL,
    "LastLogin" timestamp with time zone,
    "BonusMinutes" integer NOT NULL,
    "VoiceMinutesUsed" integer NOT NULL,
    "WhatsAppMessagesUsed" integer NOT NULL,
    "BonusWhatsAppMessages" integer NOT NULL,
    "RolloverVoiceMinutes" integer NOT NULL,
    "RolloverWhatsAppMessages" integer NOT NULL,
    "RolloverExpiresAt" timestamp with time zone,
    "PromoCodeUsed" text,
    "IsTrial" boolean NOT NULL,
    "TrialStartedAt" timestamp with time zone,
    "TrialEndsAt" timestamp with time zone,
    "TrialMinutesLimit" integer,
    "TrialDurationDays" integer,
    "TrialType" text,
    "TrialAssignedByAdminId" text,
    "TrialNotes" text,
    "CookieConsent" text,
    "CookieConsentAt" timestamp with time zone,
    "PasswordResetToken" text,
    "PasswordResetExpires" timestamp with time zone,
    "NotificationSettings" text,
    "SecuritySettings" text,
    "GoogleCalendarToken" text,
    "GoogleCalendarConnected" boolean NOT NULL,
    "PasswordMustChange" boolean NOT NULL,
    "PasswordExpiresAt" timestamp with time zone,
    "TemporaryPassword" boolean NOT NULL,
    "PasswordChangedAt" timestamp with time zone,
    "CreatedByAdminId" text,
    "AssignedSalesRepId" text,
    "MonthlyMinutesLimit" integer,
    "CustomWhatsAppLimit" integer,
    "CustomSmsLimit" integer,
    "CustomMonthlyFee" numeric,
    "CustomPerMinuteRate" numeric,
    "SetupFee" numeric,
    "CustomVoiceMinuteRate" numeric,
    "CustomTransferMinuteRate" numeric,
    "CustomWhatsAppMessageRate" numeric,
    "CustomSmsMessageRate" numeric,
    "EnabledFeatures" text,
    "AccountManagerNotes" text,
    "BillingNotes" text,
    "InvoiceHistory" text,
    "OnboardingCompleted" boolean NOT NULL,
    "OnboardingStep" integer NOT NULL,
    "TotpSecret" text,
    "TotpEnabled" boolean NOT NULL,
    "TotpBackupCodes" text,
    "EnabledLanguages" text,
    "AssignedCountries" text,
    "BlockReason" text,
    "BlockedAt" timestamp with time zone,
    "BlockedByAdminId" text,
    "AiAgentConfig" text,
    "AdminNotes" text,
    "ConfigUpdatedAt" timestamp with time zone,
    "ConfigUpdatedByAdminId" text,
    "PricingNotifiedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "AgentProfiles" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "GreetingMessage" text NOT NULL,
    "ClosingMessage" text NOT NULL,
    "ServicesList" text NOT NULL,
    "BusinessHours" text NOT NULL,
    "VoicemailMessage" text NOT NULL,
    "Language" text NOT NULL,
    "Tone" text NOT NULL,
    "HumanBackupNumber" text,
    "TransferOnComplexIssues" boolean NOT NULL,
    "CustomInstructions" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_AgentProfiles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AgentProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ApiKeys" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "Name" character varying(255) NOT NULL,
    "KeyHash" character varying(255) NOT NULL,
    "Prefix" character varying(20) NOT NULL,
    "Scopes" text,
    "IsActive" boolean NOT NULL,
    "ExpiresAt" timestamp with time zone,
    "LastUsedAt" timestamp with time zone,
    "LastUsedIp" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_ApiKeys" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ApiKeys_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AuditLogs" (
    "Id" text NOT NULL,
    "UserId" text,
    "Action" text NOT NULL,
    "EntityType" text NOT NULL,
    "EntityId" text,
    "OldValues" text,
    "NewValues" text,
    "IpAddress" text,
    "UserAgent" text,
    "PerformedAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AuditLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE TABLE "Customers" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "Name" text NOT NULL,
    "Phone" text NOT NULL,
    "Email" text,
    "Address" text,
    "City" text,
    "State" text,
    "ZipCode" text,
    "LeadStatus" text NOT NULL,
    "LeadScore" integer NOT NULL,
    "LeadQualification" text NOT NULL,
    "TotalSpend" numeric NOT NULL,
    "JobCount" integer NOT NULL,
    "Source" text,
    "ConvertedAt" timestamp with time zone,
    "Notes" text,
    "LastContactedAt" timestamp with time zone,
    "LastServiceDate" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Customers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Customers_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Invoices" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "InvoiceNumber" text NOT NULL,
    "PeriodStart" timestamp with time zone NOT NULL,
    "PeriodEnd" timestamp with time zone NOT NULL,
    "Subtotal" numeric NOT NULL,
    "Tax" numeric NOT NULL,
    "Total" numeric NOT NULL,
    "Status" text NOT NULL,
    "PaidAt" timestamp with time zone,
    "PaymentMethod" text,
    "PaymentReference" text,
    "PdfUrl" text,
    "EmailedAt" timestamp with time zone,
    "Notes" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Invoices" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Invoices_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "KnowledgeDocuments" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "Title" text NOT NULL,
    "Content" text NOT NULL,
    "Category" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "VectorEmbedding" text,
    "LastProcessedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_KnowledgeDocuments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_KnowledgeDocuments_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Notifications" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "Title" text NOT NULL,
    "Message" text NOT NULL,
    "Type" text NOT NULL,
    "IsRead" boolean NOT NULL,
    "ReadAt" timestamp with time zone,
    "ActionUrl" text,
    "Metadata" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PhoneNumbers" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "Number" text NOT NULL,
    "FriendlyName" text,
    "TwilioSid" text,
    "CountryCode" text NOT NULL,
    "IsPrimary" boolean NOT NULL,
    "WhatsAppEnabled" boolean NOT NULL,
    "WhatsAppVerified" boolean NOT NULL,
    "VoiceEnabled" boolean NOT NULL,
    "SmsEnabled" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_PhoneNumbers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PhoneNumbers_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PromoCodeUsages" (
    "Id" text NOT NULL,
    "PromoCodeId" text NOT NULL,
    "UserId" text NOT NULL,
    "UsedAt" timestamp with time zone NOT NULL,
    "IpAddress" text,
    "UserAgent" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_PromoCodeUsages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PromoCodeUsages_PromoCodes_PromoCodeId" FOREIGN KEY ("PromoCodeId") REFERENCES "PromoCodes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PromoCodeUsages_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "RefreshTokens" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "Token" text NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "Revoked" boolean NOT NULL,
    "DeviceName" text,
    "DeviceType" text,
    "Browser" text,
    "Os" text,
    "IpAddress" text,
    "Location" text,
    "LastActive" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ReminderSettings" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "Enabled" boolean NOT NULL,
    "HoursBeforeAppointment" integer NOT NULL,
    "SendSms" boolean NOT NULL,
    "SendEmail" boolean NOT NULL,
    "SendVoiceCall" boolean NOT NULL,
    "CustomMessage" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_ReminderSettings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ReminderSettings_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Technicians" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "Name" text NOT NULL,
    "Phone" text NOT NULL,
    "Email" text,
    "Skills" text NOT NULL,
    "ServiceAreas" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "CurrentLatitude" double precision,
    "CurrentLongitude" double precision,
    "LastLocationUpdate" timestamp with time zone,
    "JobsCompleted" integer NOT NULL,
    "AverageRating" numeric NOT NULL,
    "TotalRevenue" numeric NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Technicians" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Technicians_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "TrialRequests" (
    "Id" text NOT NULL,
    "CompanyName" character varying(255) NOT NULL,
    "ContactName" character varying(255) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "Phone" character varying(50) NOT NULL,
    "Industry" character varying(100) NOT NULL,
    "NumberOfTechnicians" integer NOT NULL,
    "RequestedReason" text NOT NULL,
    "ReferralSource" character varying(255) NOT NULL,
    "Status" character varying(50) NOT NULL DEFAULT 'pending',
    "AdminNotes" text NOT NULL,
    "ApprovedByAdminId" text,
    "ApprovedAt" timestamp with time zone,
    "RejectedByAdminId" text,
    "RejectedAt" timestamp with time zone,
    "RejectionReason" text,
    "ConvertedUserId" text,
    "ConvertedAt" timestamp with time zone,
    "HasConverted" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_TrialRequests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TrialRequests_Users_ApprovedByAdminId" FOREIGN KEY ("ApprovedByAdminId") REFERENCES "Users" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_TrialRequests_Users_ConvertedUserId" FOREIGN KEY ("ConvertedUserId") REFERENCES "Users" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_TrialRequests_Users_RejectedByAdminId" FOREIGN KEY ("RejectedByAdminId") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE TABLE "UsageCosts" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "BillingPeriodStart" timestamp with time zone NOT NULL,
    "BillingPeriodEnd" timestamp with time zone NOT NULL,
    "OpenAiCosts" numeric NOT NULL,
    "TwilioCosts" numeric NOT NULL,
    "SendGridCosts" numeric NOT NULL,
    "StorageCosts" numeric NOT NULL,
    "TotalCosts" numeric NOT NULL,
    "TotalCalls" integer NOT NULL,
    "TotalMinutes" integer NOT NULL,
    "TotalSms" integer NOT NULL,
    "TotalEmails" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_UsageCosts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UsageCosts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WhatsAppContexts" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "CustomerPhone" text NOT NULL,
    "ConversationContext" text NOT NULL,
    "LastInteraction" timestamp with time zone NOT NULL,
    "MessageCount" integer NOT NULL,
    "CurrentIntent" text,
    "SessionData" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_WhatsAppContexts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WhatsAppContexts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WhatsAppFunctionCalls" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "CustomerPhone" text NOT NULL,
    "FunctionName" text NOT NULL,
    "Arguments" text NOT NULL,
    "Result" text,
    "IsSuccess" boolean NOT NULL,
    "ErrorMessage" text,
    "ExecutionTimeMs" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_WhatsAppFunctionCalls" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WhatsAppFunctionCalls_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WhatsAppScheduledMessages" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "CustomerPhone" text NOT NULL,
    "Message" text NOT NULL,
    "ScheduledFor" timestamp with time zone NOT NULL,
    "IsSent" boolean NOT NULL,
    "SentAt" timestamp with time zone,
    "MessageSid" text,
    "ErrorMessage" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_WhatsAppScheduledMessages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WhatsAppScheduledMessages_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WhatsAppVoiceNotes" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "CustomerPhone" text NOT NULL,
    "MediaUrl" text NOT NULL,
    "TranscriptText" text,
    "DurationSeconds" integer NOT NULL,
    "IsTranscribed" boolean NOT NULL,
    "TranscribedAt" timestamp with time zone,
    "ErrorMessage" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_WhatsAppVoiceNotes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WhatsAppVoiceNotes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Appointments" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "CustomerId" text NOT NULL,
    "TechnicianId" text,
    "ScheduledAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone,
    "DurationMinutes" integer NOT NULL,
    "Status" text NOT NULL,
    "ServiceType" text NOT NULL,
    "Title" text,
    "Description" text,
    "Location" text,
    "EstimatedCost" numeric NOT NULL,
    "ActualCost" numeric,
    "GoogleCalendarEventId" text,
    "ReminderSent24h" boolean NOT NULL,
    "ReminderSent2h" boolean NOT NULL,
    "Reminder24hSentAt" timestamp with time zone,
    "Reminder2hSentAt" timestamp with time zone,
    "RecurrenceRule" text,
    "ParentAppointmentId" text,
    "Notes" text,
    "CompletionNotes" text,
    "CancelledAt" timestamp with time zone,
    "CancellationReason" text,
    "ReminderSent" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Appointments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Appointments_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Appointments_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Quotes" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "CustomerId" text NOT NULL,
    "QuoteNumber" text NOT NULL,
    "Status" text NOT NULL,
    "Subtotal" numeric NOT NULL,
    "Tax" numeric NOT NULL,
    "Total" numeric NOT NULL,
    "SentAt" timestamp with time zone,
    "ViewedAt" timestamp with time zone,
    "AcceptedAt" timestamp with time zone,
    "RejectedAt" timestamp with time zone,
    "ExpiresAt" timestamp with time zone,
    "PdfUrl" text,
    "Notes" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Quotes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Quotes_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Quotes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "SMSConversations" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "CustomerId" text,
    "CustomerPhone" text NOT NULL,
    "BusinessPhone" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "LastMessageAt" timestamp with time zone,
    "MessageCount" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_SMSConversations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SMSConversations_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id"),
    CONSTRAINT "FK_SMSConversations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WhatsAppConversations" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "CustomerId" text,
    "CustomerPhone" text NOT NULL,
    "CustomerName" text,
    "BusinessPhone" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "LastMessageAt" timestamp with time zone,
    "MessageCount" integer NOT NULL,
    "UnreadCount" integer NOT NULL,
    "AiMode" text NOT NULL,
    "Summary" text,
    "Language" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_WhatsAppConversations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WhatsAppConversations_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id"),
    CONSTRAINT "FK_WhatsAppConversations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "InvoiceLineItems" (
    "Id" text NOT NULL,
    "InvoiceId" text NOT NULL,
    "Description" text NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric NOT NULL,
    "Amount" numeric NOT NULL,
    "Category" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_InvoiceLineItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_InvoiceLineItems_Invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Calls" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "CustomerId" text,
    "PhoneNumberId" text,
    "CallerPhone" text NOT NULL,
    "CallerName" text,
    "RecipientPhone" text NOT NULL,
    "Direction" text NOT NULL,
    "Outcome" text,
    "Sentiment" text,
    "ServiceType" text,
    "DurationSeconds" integer NOT NULL,
    "Cost" numeric NOT NULL,
    "RecordingSid" text,
    "RecordingUrl" text,
    "RecordingDuration" integer,
    "RecordingStatus" text,
    "TwilioCallSid" text,
    "TransferredToNumber" text,
    "WasTransferred" boolean NOT NULL,
    "TransferDurationSeconds" integer,
    "Summary" text,
    "Notes" text,
    "TranscriptText" text,
    "ActionItems" text,
    "AppointmentScheduled" boolean NOT NULL,
    "QuoteRequested" boolean NOT NULL,
    "EndedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Calls" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Calls_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Calls_PhoneNumbers_PhoneNumberId" FOREIGN KEY ("PhoneNumberId") REFERENCES "PhoneNumbers" ("Id"),
    CONSTRAINT "FK_Calls_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AppointmentReminders" (
    "Id" text NOT NULL,
    "AppointmentId" text NOT NULL,
    "ScheduledFor" timestamp with time zone NOT NULL,
    "IsSent" boolean NOT NULL,
    "SentAt" timestamp with time zone,
    "ReminderType" text NOT NULL,
    "ErrorMessage" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_AppointmentReminders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AppointmentReminders_Appointments_AppointmentId" FOREIGN KEY ("AppointmentId") REFERENCES "Appointments" ("Id") ON DELETE CASCADE
);

CREATE TABLE "TechnicianAssignments" (
    "Id" text NOT NULL,
    "AppointmentId" text NOT NULL,
    "TechnicianId" text NOT NULL,
    "AssignedAt" timestamp with time zone NOT NULL,
    "AssignedBy" text,
    "Status" text NOT NULL,
    "AcceptedAt" timestamp with time zone,
    "CompletedAt" timestamp with time zone,
    "Notes" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_TechnicianAssignments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TechnicianAssignments_Appointments_AppointmentId" FOREIGN KEY ("AppointmentId") REFERENCES "Appointments" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TechnicianAssignments_Technicians_TechnicianId" FOREIGN KEY ("TechnicianId") REFERENCES "Technicians" ("Id") ON DELETE CASCADE
);

CREATE TABLE "QuoteLineItems" (
    "Id" text NOT NULL,
    "QuoteId" text NOT NULL,
    "Description" text NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric NOT NULL,
    "Amount" numeric NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_QuoteLineItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_QuoteLineItems_Quotes_QuoteId" FOREIGN KEY ("QuoteId") REFERENCES "Quotes" ("Id") ON DELETE CASCADE
);

CREATE TABLE "SMSMessages" (
    "Id" text NOT NULL,
    "ConversationId" text NOT NULL,
    "Direction" text NOT NULL,
    "Content" text NOT NULL,
    "Status" text NOT NULL,
    "DeliveredAt" timestamp with time zone,
    "TwilioMessageSid" text,
    "ErrorCode" text,
    "ErrorMessage" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_SMSMessages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SMSMessages_SMSConversations_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "SMSConversations" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WhatsAppMessages" (
    "Id" text NOT NULL,
    "ConversationId" text NOT NULL,
    "Direction" text NOT NULL,
    "Content" text NOT NULL,
    "MediaUrl" text,
    "MediaType" text,
    "Status" text NOT NULL,
    "DeliveredAt" timestamp with time zone,
    "ReadAt" timestamp with time zone,
    "TwilioMessageSid" text,
    "ErrorCode" text,
    "ErrorMessage" text,
    "IsAiGenerated" boolean NOT NULL,
    "AiContext" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_WhatsAppMessages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WhatsAppMessages_WhatsAppConversations_ConversationId" FOREIGN KEY ("ConversationId") REFERENCES "WhatsAppConversations" ("Id") ON DELETE CASCADE
);

CREATE TABLE "TranscriptEntries" (
    "Id" text NOT NULL,
    "CallId" text NOT NULL,
    "Speaker" text NOT NULL,
    "Text" text NOT NULL,
    "Confidence" double precision NOT NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    "SequenceNumber" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_TranscriptEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TranscriptEntries_Calls_CallId" FOREIGN KEY ("CallId") REFERENCES "Calls" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WebSocketMetrics" (
    "Id" text NOT NULL,
    "UserId" text NOT NULL,
    "CallId" text NOT NULL,
    "ConnectedAt" timestamp with time zone NOT NULL,
    "DisconnectedAt" timestamp with time zone,
    "DurationSeconds" integer NOT NULL,
    "BytesSent" bigint NOT NULL,
    "BytesReceived" bigint NOT NULL,
    "MessagesIn" integer NOT NULL,
    "MessagesOut" integer NOT NULL,
    "ErrorCount" integer NOT NULL,
    "AverageLatencyMs" double precision NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_WebSocketMetrics" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WebSocketMetrics_Calls_CallId" FOREIGN KEY ("CallId") REFERENCES "Calls" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WebSocketMetrics_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AgentProfiles_UserId" ON "AgentProfiles" ("UserId");

CREATE UNIQUE INDEX "IX_ApiKeys_KeyHash" ON "ApiKeys" ("KeyHash");

CREATE INDEX "IX_ApiKeys_Prefix" ON "ApiKeys" ("Prefix");

CREATE INDEX "IX_ApiKeys_UserId" ON "ApiKeys" ("UserId");

CREATE INDEX "IX_AppointmentReminders_AppointmentId" ON "AppointmentReminders" ("AppointmentId");

CREATE INDEX "IX_AppointmentReminders_ScheduledFor" ON "AppointmentReminders" ("ScheduledFor");

CREATE INDEX "IX_Appointments_CustomerId" ON "Appointments" ("CustomerId");

CREATE INDEX "IX_Appointments_ScheduledAt" ON "Appointments" ("ScheduledAt");

CREATE INDEX "IX_Appointments_UserId" ON "Appointments" ("UserId");

CREATE INDEX "IX_AuditLogs_EntityType" ON "AuditLogs" ("EntityType");

CREATE INDEX "IX_AuditLogs_PerformedAt" ON "AuditLogs" ("PerformedAt");

CREATE INDEX "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");

CREATE INDEX "IX_Calls_CreatedAt" ON "Calls" ("CreatedAt");

CREATE INDEX "IX_Calls_CustomerId" ON "Calls" ("CustomerId");

CREATE INDEX "IX_Calls_PhoneNumberId" ON "Calls" ("PhoneNumberId");

CREATE INDEX "IX_Calls_UserId" ON "Calls" ("UserId");

CREATE INDEX "IX_Customers_Email" ON "Customers" ("Email");

CREATE INDEX "IX_Customers_Phone" ON "Customers" ("Phone");

CREATE INDEX "IX_Customers_UserId" ON "Customers" ("UserId");

CREATE UNIQUE INDEX "IX_FeatureFlags_Name" ON "FeatureFlags" ("Name");

CREATE INDEX "IX_InvoiceLineItems_InvoiceId" ON "InvoiceLineItems" ("InvoiceId");

CREATE UNIQUE INDEX "IX_Invoices_InvoiceNumber" ON "Invoices" ("InvoiceNumber");

CREATE INDEX "IX_Invoices_UserId" ON "Invoices" ("UserId");

CREATE INDEX "IX_KnowledgeDocuments_IsActive" ON "KnowledgeDocuments" ("IsActive");

CREATE INDEX "IX_KnowledgeDocuments_UserId" ON "KnowledgeDocuments" ("UserId");

CREATE INDEX "IX_Notifications_IsRead" ON "Notifications" ("IsRead");

CREATE INDEX "IX_Notifications_UserId" ON "Notifications" ("UserId");

CREATE UNIQUE INDEX "IX_PhoneNumbers_Number" ON "PhoneNumbers" ("Number");

CREATE INDEX "IX_PhoneNumbers_UserId" ON "PhoneNumbers" ("UserId");

CREATE UNIQUE INDEX "IX_PromoCodes_Code" ON "PromoCodes" ("Code");

CREATE INDEX "IX_PromoCodes_IsActive" ON "PromoCodes" ("IsActive");

CREATE INDEX "IX_PromoCodeUsages_PromoCodeId" ON "PromoCodeUsages" ("PromoCodeId");

CREATE INDEX "IX_PromoCodeUsages_UserId" ON "PromoCodeUsages" ("UserId");

CREATE INDEX "IX_QuoteLineItems_QuoteId" ON "QuoteLineItems" ("QuoteId");

CREATE INDEX "IX_Quotes_CustomerId" ON "Quotes" ("CustomerId");

CREATE UNIQUE INDEX "IX_Quotes_QuoteNumber" ON "Quotes" ("QuoteNumber");

CREATE INDEX "IX_Quotes_UserId" ON "Quotes" ("UserId");

CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");

CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");

CREATE UNIQUE INDEX "IX_ReminderSettings_UserId" ON "ReminderSettings" ("UserId");

CREATE INDEX "IX_SMSConversations_CustomerId" ON "SMSConversations" ("CustomerId");

CREATE INDEX "IX_SMSConversations_CustomerPhone" ON "SMSConversations" ("CustomerPhone");

CREATE INDEX "IX_SMSConversations_UserId" ON "SMSConversations" ("UserId");

CREATE INDEX "IX_SMSMessages_ConversationId" ON "SMSMessages" ("ConversationId");

CREATE INDEX "IX_SystemStatuses_CheckedAt" ON "SystemStatuses" ("CheckedAt");

CREATE INDEX "IX_SystemStatuses_ServiceName" ON "SystemStatuses" ("ServiceName");

CREATE INDEX "IX_TechnicianAssignments_AppointmentId" ON "TechnicianAssignments" ("AppointmentId");

CREATE INDEX "IX_TechnicianAssignments_TechnicianId" ON "TechnicianAssignments" ("TechnicianId");

CREATE INDEX "IX_Technicians_IsActive" ON "Technicians" ("IsActive");

CREATE INDEX "IX_Technicians_Phone" ON "Technicians" ("Phone");

CREATE INDEX "IX_Technicians_UserId" ON "Technicians" ("UserId");

CREATE INDEX "IX_TranscriptEntries_CallId" ON "TranscriptEntries" ("CallId");

CREATE INDEX "IX_TrialRequests_ApprovedByAdminId" ON "TrialRequests" ("ApprovedByAdminId");

CREATE INDEX "IX_TrialRequests_ConvertedUserId" ON "TrialRequests" ("ConvertedUserId");

CREATE INDEX "IX_TrialRequests_CreatedAt" ON "TrialRequests" ("CreatedAt");

CREATE INDEX "IX_TrialRequests_Email" ON "TrialRequests" ("Email");

CREATE INDEX "IX_TrialRequests_Email_Status" ON "TrialRequests" ("Email", "Status");

CREATE INDEX "IX_TrialRequests_RejectedByAdminId" ON "TrialRequests" ("RejectedByAdminId");

CREATE INDEX "IX_TrialRequests_Status" ON "TrialRequests" ("Status");

CREATE INDEX "IX_UsageCosts_BillingPeriodStart" ON "UsageCosts" ("BillingPeriodStart");

CREATE INDEX "IX_UsageCosts_UserId" ON "UsageCosts" ("UserId");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

CREATE INDEX "IX_WebSocketMetrics_CallId" ON "WebSocketMetrics" ("CallId");

CREATE INDEX "IX_WebSocketMetrics_UserId" ON "WebSocketMetrics" ("UserId");

CREATE INDEX "IX_WhatsAppContexts_CustomerPhone" ON "WhatsAppContexts" ("CustomerPhone");

CREATE INDEX "IX_WhatsAppContexts_UserId" ON "WhatsAppContexts" ("UserId");

CREATE INDEX "IX_WhatsAppConversations_CustomerId" ON "WhatsAppConversations" ("CustomerId");

CREATE INDEX "IX_WhatsAppConversations_CustomerPhone" ON "WhatsAppConversations" ("CustomerPhone");

CREATE INDEX "IX_WhatsAppConversations_UserId" ON "WhatsAppConversations" ("UserId");

CREATE INDEX "IX_WhatsAppFunctionCalls_CustomerPhone" ON "WhatsAppFunctionCalls" ("CustomerPhone");

CREATE INDEX "IX_WhatsAppFunctionCalls_UserId" ON "WhatsAppFunctionCalls" ("UserId");

CREATE INDEX "IX_WhatsAppMessages_ConversationId" ON "WhatsAppMessages" ("ConversationId");

CREATE INDEX "IX_WhatsAppScheduledMessages_ScheduledFor" ON "WhatsAppScheduledMessages" ("ScheduledFor");

CREATE INDEX "IX_WhatsAppScheduledMessages_UserId" ON "WhatsAppScheduledMessages" ("UserId");

CREATE INDEX "IX_WhatsAppVoiceNotes_CustomerPhone" ON "WhatsAppVoiceNotes" ("CustomerPhone");

CREATE INDEX "IX_WhatsAppVoiceNotes_UserId" ON "WhatsAppVoiceNotes" ("UserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260208135046_InitialCreate', '8.0.11');

COMMIT;

