using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;

#pragma warning disable CS0618

namespace SpeakHalo.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    private readonly IEncryptionService? _encryptionService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IEncryptionService encryptionService) : base(options)
    {
        _encryptionService = encryptionService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserAuth> UserAuths => Set<UserAuth>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<UserAdmin> UserAdmins => Set<UserAdmin>();
    public DbSet<UserTrial> UserTrials => Set<UserTrial>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();
    public DbSet<Call> Calls => Set<Call>();
    public DbSet<CallFollowUp> CallFollowUps => Set<CallFollowUp>();
    public DbSet<TranscriptEntry> TranscriptEntries => Set<TranscriptEntry>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<WhatsAppConversation> WhatsAppConversations => Set<WhatsAppConversation>();
    public DbSet<WhatsAppMessage> WhatsAppMessages => Set<WhatsAppMessage>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AgentProfile> AgentProfiles => Set<AgentProfile>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<SMSConversation> SMSConversations => Set<SMSConversation>();
    public DbSet<SMSMessage> SMSMessages => Set<SMSMessage>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteLineItem> QuoteLineItems => Set<QuoteLineItem>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<AppointmentReminder> AppointmentReminders => Set<AppointmentReminder>();
    public DbSet<ReminderSettings> ReminderSettings => Set<ReminderSettings>();
    public DbSet<WhatsAppContext> WhatsAppContexts => Set<WhatsAppContext>();
    public DbSet<WhatsAppFunctionCall> WhatsAppFunctionCalls => Set<WhatsAppFunctionCall>();
    public DbSet<WhatsAppVoiceNote> WhatsAppVoiceNotes => Set<WhatsAppVoiceNote>();
    public DbSet<WhatsAppScheduledMessage> WhatsAppScheduledMessages => Set<WhatsAppScheduledMessage>();
    public DbSet<UsageCosts> UsageCosts => Set<UsageCosts>();
    public DbSet<WebSocketMetrics> WebSocketMetrics => Set<WebSocketMetrics>();
    public DbSet<SystemStatus> SystemStatuses => Set<SystemStatus>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<Industry> Industries => Set<Industry>();
    public DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles => Set<KnowledgeBaseArticle>();
    public DbSet<CommonIssue> CommonIssues => Set<CommonIssue>();
    public DbSet<ProductKnowledge> ProductKnowledge => Set<ProductKnowledge>();
    public DbSet<AIConversationContext> AIConversationContexts => Set<AIConversationContext>();
    public DbSet<AITrainingExample> AITrainingExamples => Set<AITrainingExample>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<ImageAnalysis> ImageAnalyses => Set<ImageAnalysis>();
    public DbSet<UsageWarning> UsageWarnings => Set<UsageWarning>();
    public DbSet<MonthlyUsageArchive> MonthlyUsageArchives => Set<MonthlyUsageArchive>();

    // Cookie Management
    public DbSet<CookieDefinition> CookieDefinitions => Set<CookieDefinition>();
    public DbSet<CookiePolicy> CookiePolicies => Set<CookiePolicy>();
    public DbSet<UserCookieConsent> UserCookieConsents => Set<UserCookieConsent>();
    public DbSet<CookieAuditLog> CookieAuditLogs => Set<CookieAuditLog>();

    // Voice Agents
    public DbSet<VoiceAgent> VoiceAgents => Set<VoiceAgent>();

    // Phone Verification
    public DbSet<PhoneVerification> PhoneVerifications => Set<PhoneVerification>();

    // Pricing Plans
    public DbSet<PricingPlan> PricingPlans => Set<PricingPlan>();

    // Team Management
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamInvite> TeamInvites => Set<TeamInvite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global query filter for soft deletes
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var filter = Expression.Lambda(Expression.Not(property), parameter);
                entityType.SetQueryFilter(filter);
            }
        }

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.HashedPassword).HasMaxLength(255).IsRequired();
            entity.Property(e => e.BusinessName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>();
            entity.Property(e => e.Industry).HasConversion<string>();
            entity.Property(e => e.IndustrySpecialization).HasMaxLength(200);

            // Check constraints for data integrity
            entity.ToTable(t => t.HasCheckConstraint("CK_User_Email_Format", "\"Email\" ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'"));
            entity.ToTable(t => t.HasCheckConstraint("CK_User_Name_Length", "LENGTH(\"Name\") >= 2"));
            entity.ToTable(t => t.HasCheckConstraint("CK_User_BusinessName_Length", "LENGTH(\"BusinessName\") >= 2"));
        });

        // Configure UserAuth (One-to-One with User)
        modelBuilder.Entity<UserAuth>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.HashedPassword).HasMaxLength(255);
            entity.Property(e => e.PasswordResetToken).HasMaxLength(500);
            entity.Property(e => e.TotpSecret).HasMaxLength(500);
            // JSONB for settings
            entity.Property(e => e.SecuritySettings).HasColumnType("jsonb");
            entity.Property(e => e.TotpBackupCodes).HasColumnType("jsonb");
            entity.HasOne(e => e.User)
                .WithOne(u => u.Auth)
                .HasForeignKey<UserAuth>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserProfile (One-to-One with User)
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.BusinessName).HasMaxLength(255);
            entity.Property(e => e.BusinessPhone).HasMaxLength(50);
            entity.Property(e => e.BusinessAddress).HasMaxLength(500);
            entity.Property(e => e.BusinessWebsite).HasMaxLength(500);
            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.Industry).HasConversion<string>();
            entity.Property(e => e.IndustrySpecialization).HasMaxLength(200);
            // JSONB for configuration and settings
            entity.Property(e => e.NotificationSettings).HasColumnType("jsonb");
            entity.Property(e => e.EnabledLanguages).HasColumnType("jsonb");
            entity.Property(e => e.AssignedCountries).HasColumnType("jsonb");
            entity.Property(e => e.AiAgentConfig).HasColumnType("jsonb");
            entity.Property(e => e.CookieConsent).HasColumnType("jsonb");

            // Encrypt GoogleCalendarToken at rest — OAuth tokens are sensitive
            if (_encryptionService != null)
            {
                entity.Property(e => e.GoogleCalendarToken).HasConversion(
                    v => v != null ? _encryptionService.Encrypt(v) : null,
                    v => v != null ? _encryptionService.Decrypt(v) : null);
            }

            entity.HasOne(e => e.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<UserProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PricingPlan
        modelBuilder.Entity<PricingPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EnabledFeatures).HasColumnType("jsonb");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
        });

        // Configure UserSubscription (One-to-One with User)
        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Plan).HasMaxLength(100);
            // Usage enforcement: default to hard limits (safe for new/unpaid users)
            entity.Property(e => e.EnforceHardLimits).HasDefaultValue(true);
            // JSONB for features and history
            entity.Property(e => e.EnabledFeatures).HasColumnType("jsonb");
            entity.Property(e => e.InvoiceHistory).HasColumnType("jsonb");
            // Indexes for subscription queries
            entity.HasIndex(e => e.Plan);
            entity.HasIndex(e => e.RolloverExpiresAt);
            // Pricing Plan FK
            entity.HasIndex(e => e.PricingPlanId);
            entity.HasOne(e => e.PricingPlan)
                .WithMany()
                .HasForeignKey(e => e.PricingPlanId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.User)
                .WithOne(u => u.Subscription)
                .HasForeignKey<UserSubscription>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserTrial (One-to-One with User)
        modelBuilder.Entity<UserTrial>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.TrialType).HasMaxLength(50);
            entity.Property(e => e.TrialAssignedByAdminId).HasMaxLength(255);
            // Indexes for trial tracking
            entity.HasIndex(e => e.IsTrial);
            entity.HasIndex(e => e.TrialEndsAt);
            entity.HasOne(e => e.User)
                .WithOne(u => u.Trial)
                .HasForeignKey<UserTrial>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserAdmin (One-to-One with User)
        modelBuilder.Entity<UserAdmin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.CreatedByAdminId).HasMaxLength(255);
            entity.Property(e => e.AssignedSalesRepId).HasMaxLength(255);
            entity.Property(e => e.BlockedByAdminId).HasMaxLength(255);
            entity.Property(e => e.BlockReason).HasMaxLength(500);
            // Indexes for admin queries
            entity.HasIndex(e => e.CreatedByAdminId);
            entity.HasIndex(e => e.AssignedSalesRepId);
            entity.HasIndex(e => e.BlockedAt);
            entity.HasOne(e => e.User)
                .WithOne(u => u.Admin)
                .HasForeignKey<UserAdmin>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Industry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ServiceType).IsUnique();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.ServiceType).HasConversion<string>().IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DisplayOrder).IsRequired();

            // Relationships
            entity.HasMany(e => e.Users)
                .WithOne()
                .HasForeignKey(u => u.Industry)
                .HasPrincipalKey(i => i.ServiceType)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            // Composite index for finding active tokens by user
            entity.HasIndex(e => new { e.UserId, e.Revoked });
            entity.HasIndex(e => e.ExpiresAt); // For cleanup jobs
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PhoneNumber>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Number).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CountryCode);
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.Property(e => e.AssignmentMethod).HasMaxLength(50);
            entity.HasOne(e => e.User)
                .WithMany(u => u.PhoneNumbers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Call>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.CreatedAt);
            // Composite index for user's calls by date range
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            // Composite index for caller recognition lookups by userId + caller phone
            entity.HasIndex(e => new { e.UserId, e.CallerPhone });
            entity.Property(e => e.Outcome).HasConversion<string>();
            entity.Property(e => e.Sentiment).HasConversion<string>();
            entity.Property(e => e.ServiceType).HasConversion<string>();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TranscriptEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CallId);
            entity.HasOne(e => e.Call)
                .WithMany(c => c.TranscriptEntries)
                .HasForeignKey(e => e.CallId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Phone);
            entity.HasIndex(e => e.Email);
            // Composite unique index: one customer per phone per business
            entity.HasIndex(e => new { e.UserId, e.Phone }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            // Composite index for filtering user's leads by status
            entity.HasIndex(e => new { e.UserId, e.LeadStatus });
            entity.Property(e => e.LeadStatus).HasConversion<string>();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => e.Status);
            // Composite indexes for common query patterns
            entity.HasIndex(e => new { e.UserId, e.ScheduledAt }); // User's appointments by date
            entity.HasIndex(e => new { e.UserId, e.Status }); // User's appointments by status
            entity.HasIndex(e => new { e.Status, e.ScheduledAt }); // All appointments by status and date
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.ServiceType).HasConversion<string>();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Appointments)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WhatsAppConversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CustomerPhone);
            // Composite unique index: one conversation per phone pair per business
            entity.HasIndex(e => new { e.UserId, e.BusinessPhone, e.CustomerPhone }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            // Composite index for caller recognition lookups by userId + customer phone
            entity.HasIndex(e => new { e.UserId, e.CustomerPhone });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WhatsAppMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.TwilioMessageSid);
            // Composite index for ordering messages in conversations
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
            entity.Property(e => e.Direction).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PeriodStart);
            entity.HasIndex(e => e.PeriodEnd);
            // Composite indexes for invoice queries
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => new { e.UserId, e.PeriodStart });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Check constraints for data integrity
            entity.ToTable(t => t.HasCheckConstraint("CK_Invoice_Subtotal_Positive", "\"Subtotal\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Invoice_Tax_Positive", "\"Tax\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Invoice_Total_Positive", "\"Total\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Invoice_Period_Valid", "\"PeriodEnd\" >= \"PeriodStart\""));
        });

        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceId);
            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.LineItems)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Check constraints for data integrity
            entity.ToTable(t => t.HasCheckConstraint("CK_InvoiceLineItem_Quantity_Positive", "\"Quantity\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_InvoiceLineItem_UnitPrice_Positive", "\"UnitPrice\" >= 0"));
        });

        modelBuilder.Entity<VoiceAgent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.VoiceName).HasMaxLength(100);
            entity.Property(e => e.Language).HasMaxLength(20);
            entity.Property(e => e.LocationName).HasMaxLength(200);
            entity.Property(e => e.LocationAddress).HasMaxLength(500);
            entity.Property(e => e.LocationPhone).HasMaxLength(50);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.CreatedAt);
            // Composite index for querying unread notifications by user
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });
            // Use JSONB for better PostgreSQL performance
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.BrandName).HasMaxLength(200);
            entity.Property(e => e.BrandTagline).HasMaxLength(500);
            entity.Property(e => e.BrandColor).HasMaxLength(20);
            entity.Property(e => e.LogoUrl).HasMaxLength(1000);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SMSConversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CustomerPhone);
            // Composite unique index: one conversation per phone pair per business
            entity.HasIndex(e => new { e.UserId, e.BusinessPhone, e.CustomerPhone }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            // Composite index for caller recognition lookups by userId + customer phone
            entity.HasIndex(e => new { e.UserId, e.CustomerPhone });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SMSMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.TwilioMessageSid);
            // Composite index for ordering messages in conversations
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
            entity.Property(e => e.Direction).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Quote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.QuoteNumber).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Check constraints for data integrity
            entity.ToTable(t => t.HasCheckConstraint("CK_Quote_Subtotal_Positive", "\"Subtotal\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Quote_Tax_Positive", "\"Tax\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Quote_Total_Positive", "\"Total\" >= 0"));
        });

        modelBuilder.Entity<QuoteLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.QuoteId);
            entity.HasOne(e => e.Quote)
                .WithMany(q => q.LineItems)
                .HasForeignKey(e => e.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Check constraints for data integrity
            entity.ToTable(t => t.HasCheckConstraint("CK_QuoteLineItem_Quantity_Positive", "\"Quantity\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_QuoteLineItem_UnitPrice_Positive", "\"UnitPrice\" >= 0"));
        });

        modelBuilder.Entity<FeatureFlag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            // Use JSONB for better PostgreSQL performance
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
        });

        modelBuilder.Entity<AppointmentReminder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AppointmentId);
            entity.HasIndex(e => e.ScheduledFor);
            entity.HasOne(e => e.Appointment)
                .WithMany()
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReminderSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WhatsAppContext>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CustomerPhone);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WhatsAppFunctionCall>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CustomerPhone);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WhatsAppVoiceNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CustomerPhone);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WhatsAppScheduledMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ScheduledFor);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UsageCosts>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.BillingPeriodStart);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WebSocketMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CallId);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Call)
                .WithMany()
                .HasForeignKey(e => e.CallId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SystemStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ServiceName);
            entity.HasIndex(e => e.CheckedAt);
            // Use JSONB for better PostgreSQL performance
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.PerformedAt);
            // Composite indexes for audit queries
            entity.HasIndex(e => new { e.UserId, e.PerformedAt });
            entity.HasIndex(e => new { e.EntityType, e.PerformedAt });
            // Use JSONB for better PostgreSQL performance
            entity.Property(e => e.OldValues).HasColumnType("jsonb");
            entity.Property(e => e.NewValues).HasColumnType("jsonb");
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Prefix);
            entity.HasIndex(e => e.KeyHash).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.KeyHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Prefix).HasMaxLength(20).IsRequired();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Composite unique index for idempotency: same EventId from same Source can only exist once
            entity.HasIndex(e => new { e.EventId, e.Source }).IsUnique();
            entity.HasIndex(e => e.Source);
            entity.HasIndex(e => e.EventName);
            entity.HasIndex(e => e.ProcessedAt);
            entity.HasIndex(e => e.Success);
            entity.Property(e => e.EventId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.EventName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RawPayload).HasColumnType("text");
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
        });

        modelBuilder.Entity<MediaFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.WhatsAppMessageId);
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.MediaType);
            entity.HasIndex(e => e.DownloadStatus);
            entity.HasIndex(e => e.IsProcessed);
            entity.HasIndex(e => e.CreatedAt);
            // Composite indexes for media queries
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => new { e.UserId, e.MediaType });
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
            entity.HasIndex(e => new { e.DownloadStatus, e.CreatedAt }); // For processing queue
            entity.Property(e => e.MediaType).HasConversion<string>();
            entity.Property(e => e.StorageType).HasConversion<string>();
            entity.Property(e => e.DownloadStatus).HasConversion<string>();
            // Use JSONB for better PostgreSQL performance
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.WhatsAppMessage)
                .WithMany()
                .HasForeignKey(e => e.WhatsAppMessageId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Cookie Management Entities
        modelBuilder.Entity<CookieDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsEssential);
            entity.HasIndex(e => new { e.Name, e.Domain }).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Category).HasConversion<string>();
            entity.Property(e => e.Domain).HasMaxLength(255);
            entity.Property(e => e.Path).HasMaxLength(255);
            entity.Property(e => e.SameSite).HasMaxLength(20);
            entity.Property(e => e.Provider).HasMaxLength(100);
            entity.Property(e => e.PrivacyPolicyUrl).HasMaxLength(500);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");

            entity.ToTable(t => t.HasCheckConstraint("CK_CookieDefinition_Name_Length", "LENGTH(\"Name\") >= 1"));
            entity.ToTable(t => t.HasCheckConstraint("CK_CookieDefinition_ExpirationDays", "\"ExpirationDays\" IS NULL OR \"ExpirationDays\" > 0"));
        });

        modelBuilder.Entity<CookiePolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Version).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.EffectiveDate);
            entity.HasIndex(e => e.Language);

            entity.Property(e => e.Version).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Language).HasMaxLength(10).IsRequired();
            entity.Property(e => e.PolicyUrl).HasMaxLength(500);
            entity.Property(e => e.PublishedBy).HasMaxLength(100);
            entity.Property(e => e.ChangeNotes).HasMaxLength(2000);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");

            entity.ToTable(t => t.HasCheckConstraint("CK_CookiePolicy_Version_Length", "LENGTH(\"Version\") >= 1"));
            entity.ToTable(t => t.HasCheckConstraint("CK_CookiePolicy_AcceptanceCount", "\"AcceptanceCount\" >= 0"));
        });

        modelBuilder.Entity<UserCookieConsent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ConsentToken);
            entity.HasIndex(e => e.PolicyId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ConsentDate);
            entity.HasIndex(e => new { e.UserId, e.Category, e.PolicyId });
            entity.HasIndex(e => new { e.ConsentToken, e.Category, e.PolicyId });

            entity.Property(e => e.UserId).HasMaxLength(50);
            entity.Property(e => e.ConsentToken).HasMaxLength(100);
            entity.Property(e => e.PolicyId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Category).HasConversion<string>();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.CountryCode).HasMaxLength(2);
            entity.Property(e => e.ConsentMethod).HasMaxLength(50);
            entity.Property(e => e.WithdrawalReason).HasMaxLength(500);
            entity.Property(e => e.Source).HasMaxLength(50);
            entity.Property(e => e.ReferrerUrl).HasMaxLength(1000);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Policy)
                .WithMany(p => p.UserConsents)
                .HasForeignKey(e => e.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t => t.HasCheckConstraint("CK_UserCookieConsent_UserOrToken", "\"UserId\" IS NOT NULL OR \"ConsentToken\" IS NOT NULL"));
        });

        modelBuilder.Entity<CookieAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ConsentToken);
            entity.HasIndex(e => e.ConsentId);
            entity.HasIndex(e => e.CookieDefinitionId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => new { e.UserId, e.Action, e.CreatedAt });

            entity.Property(e => e.UserId).HasMaxLength(50);
            entity.Property(e => e.ConsentToken).HasMaxLength(100);
            entity.Property(e => e.ConsentId).HasMaxLength(50);
            entity.Property(e => e.CookieDefinitionId).HasMaxLength(50);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Category).HasConversion<string>();
            entity.Property(e => e.OldValue).HasColumnType("jsonb");
            entity.Property(e => e.NewValue).HasColumnType("jsonb");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.CountryCode).HasMaxLength(2);
            entity.Property(e => e.RequestUrl).HasMaxLength(1000);
            entity.Property(e => e.ReferrerUrl).HasMaxLength(1000);
            entity.Property(e => e.SessionId).HasMaxLength(100);
            entity.Property(e => e.Source).HasMaxLength(50);
            entity.Property(e => e.Details).HasColumnType("jsonb");
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Consent)
                .WithMany()
                .HasForeignKey(e => e.ConsentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CookieDefinition)
                .WithMany()
                .HasForeignKey(e => e.CookieDefinitionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable(t => t.HasCheckConstraint("CK_CookieAuditLog_Action_Length", "LENGTH(\"Action\") >= 1"));
            entity.ToTable(t => t.HasCheckConstraint("CK_CookieAuditLog_DurationMs", "\"DurationMs\" IS NULL OR \"DurationMs\" >= 0"));
        });

        // Phone Verification Configuration
        modelBuilder.Entity<PhoneVerification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PhoneNumber);
            entity.HasIndex(e => new { e.PhoneNumber, e.CreatedAt });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.VerificationToken)
                .IsUnique()
                .HasFilter("\"VerificationToken\" IS NOT NULL");

            entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.OtpHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.VerificationToken).HasMaxLength(32);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
        });

        // Team Management
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OwnerId).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.OwnerId).HasMaxLength(36).IsRequired();

            entity.HasOne(e => e.Owner)
                .WithOne()
                .HasForeignKey<Team>(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TeamId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.TeamId).HasMaxLength(36).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(36).IsRequired();

            entity.HasOne(e => e.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeamInvite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.TeamId, e.Email });
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.TeamId).HasMaxLength(36).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(320).IsRequired();
            entity.Property(e => e.Token).HasMaxLength(64).IsRequired();
            entity.Property(e => e.InvitedByUserId).HasMaxLength(36).IsRequired();
            entity.Property(e => e.AcceptedByUserId).HasMaxLength(36);

            entity.HasOne(e => e.Team)
                .WithMany(t => t.Invites)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvitedByUser)
                .WithMany()
                .HasForeignKey(e => e.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CallFollowUp configuration
        modelBuilder.Entity<CallFollowUp>(entity =>
        {
            entity.HasIndex(e => new { e.Status, e.ScheduledFor });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CallId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Call)
                .WithMany()
                .HasForeignKey(e => e.CallId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // MonthlyUsageArchive configuration
        modelBuilder.Entity<MonthlyUsageArchive>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.BillingMonth }).IsUnique();
            entity.HasIndex(e => e.BillingMonth);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UsageWarning configuration
        modelBuilder.Entity<UsageWarning>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ResourceType, e.BillingMonth });
            entity.HasIndex(e => e.BillingMonth);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ImageAnalysis configuration
        modelBuilder.Entity<ImageAnalysis>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.BillingMonth });
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Appointment)
                .WithMany()
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AIConversationContext configuration
        modelBuilder.Entity<AIConversationContext>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.ExpiresAt);
        });

        // KnowledgeBaseArticle configuration
        modelBuilder.Entity<KnowledgeBaseArticle>(entity =>
        {
            entity.HasIndex(e => e.Industry);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
        });

        // CommonIssue configuration
        modelBuilder.Entity<CommonIssue>(entity =>
        {
            entity.HasIndex(e => e.Industry);
            entity.HasIndex(e => e.EquipmentType);
            entity.HasIndex(e => e.IsActive);
        });

        // ProductKnowledge configuration
        modelBuilder.Entity<ProductKnowledge>(entity =>
        {
            entity.HasIndex(e => e.Topic);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
        });

        // AITrainingExample configuration
        modelBuilder.Entity<AITrainingExample>(entity =>
        {
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Intent);
            entity.HasIndex(e => e.IncludeInTraining);
        });

    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

#pragma warning restore CS0618
