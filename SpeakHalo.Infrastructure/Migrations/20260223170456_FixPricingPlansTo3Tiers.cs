using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakHalo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPricingPlansTo3Tiers : Migration
    {
        // Feature JSON applied to every plan — all features enabled on every tier.
        // Differentiation is by monthly volume and overage rates only.
        private const string AllFeatures =
            "{\"voiceAI\":true,\"sms\":true,\"calendar\":true,\"knowledgeBase\":true," +
            "\"whatsApp\":true,\"aiPhotoQuotes\":true,\"advancedAnalytics\":true," +
            "\"customBranding\":true,\"teamMembers\":true,\"multipleLocations\":true}";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Deactivate every plan whose Name is NOT one of our 3 maintained plans.
            //       Covers orphans identified by Name (the unique column).
            migrationBuilder.Sql(@"
                UPDATE ""PricingPlans""
                SET    ""IsActive""   = false,
                       ""UpdatedAt""  = NOW() AT TIME ZONE 'UTC'
                WHERE  ""Name""       NOT IN ('Starter', 'Professional', 'Business')
                AND    ""IsDeleted""  = false;
            ");

            // ── 2. Upsert Starter plan ($49 / mo) — conflict on Name ─────────────────────────
            migrationBuilder.Sql($@"
                INSERT INTO ""PricingPlans""
                    (""Id"", ""Name"", ""TierSlug"", ""Description"",
                     ""DisplayPrice"", ""SortOrder"", ""IsActive"", ""IsDeleted"", ""TrialDays"",
                     ""AIMinutesIncluded"", ""SMSMessagesIncluded"", ""WhatsAppMessagesIncluded"",
                     ""ImagesIncluded"", ""MaxPhoneNumbers"", ""EnforceHardLimits"",
                     ""AIOverageRate"", ""SMSOverageRate"", ""WhatsAppOverageRate"", ""ImageOverageRate"",
                     ""EnabledFeatures"", ""CreatedAt"")
                VALUES (
                    gen_random_uuid()::text,
                    'Starter', 'starter',
                    'Every feature included. Perfect for solo operators ready to stop missing calls and start booking more jobs.',
                    49, 0, true, false, 14,
                    150, 150, 150, 30, 1, true,
                    0.25, 0.18, 0.04, 0.75,
                    '{AllFeatures}',
                    NOW() AT TIME ZONE 'UTC'
                )
                ON CONFLICT (""Name"") DO UPDATE SET
                    ""TierSlug""                  = 'starter',
                    ""Description""               = 'Every feature included. Perfect for solo operators ready to stop missing calls and start booking more jobs.',
                    ""DisplayPrice""              = 49,
                    ""SortOrder""                = 0,
                    ""IsActive""                 = true,
                    ""IsDeleted""                = false,
                    ""TrialDays""                = 14,
                    ""AIMinutesIncluded""         = 150,
                    ""SMSMessagesIncluded""       = 150,
                    ""WhatsAppMessagesIncluded""  = 150,
                    ""ImagesIncluded""            = 30,
                    ""MaxPhoneNumbers""           = 1,
                    ""EnforceHardLimits""         = true,
                    ""AIOverageRate""             = 0.25,
                    ""SMSOverageRate""            = 0.18,
                    ""WhatsAppOverageRate""       = 0.04,
                    ""ImageOverageRate""          = 0.75,
                    ""EnabledFeatures""           = '{AllFeatures}',
                    ""UpdatedAt""                = NOW() AT TIME ZONE 'UTC';
            ");

            // ── 3. Upsert Professional plan ($99 / mo) — conflict on Name ────────────────────
            migrationBuilder.Sql($@"
                INSERT INTO ""PricingPlans""
                    (""Id"", ""Name"", ""TierSlug"", ""Description"",
                     ""DisplayPrice"", ""SortOrder"", ""IsActive"", ""IsDeleted"", ""TrialDays"",
                     ""AIMinutesIncluded"", ""SMSMessagesIncluded"", ""WhatsAppMessagesIncluded"",
                     ""ImagesIncluded"", ""MaxPhoneNumbers"", ""EnforceHardLimits"",
                     ""AIOverageRate"", ""SMSOverageRate"", ""WhatsAppOverageRate"", ""ImageOverageRate"",
                     ""EnabledFeatures"", ""CreatedAt"")
                VALUES (
                    gen_random_uuid()::text,
                    'Professional', 'professional',
                    'Every feature included. More monthly volume and lower overage rates for growing businesses.',
                    99, 1, true, false, 14,
                    500, 500, 400, 100, 1, false,
                    0.20, 0.13, 0.025, 0.50,
                    '{AllFeatures}',
                    NOW() AT TIME ZONE 'UTC'
                )
                ON CONFLICT (""Name"") DO UPDATE SET
                    ""TierSlug""                  = 'professional',
                    ""Description""               = 'Every feature included. More monthly volume and lower overage rates for growing businesses.',
                    ""DisplayPrice""              = 99,
                    ""SortOrder""                = 1,
                    ""IsActive""                 = true,
                    ""IsDeleted""                = false,
                    ""TrialDays""                = 14,
                    ""AIMinutesIncluded""         = 500,
                    ""SMSMessagesIncluded""       = 500,
                    ""WhatsAppMessagesIncluded""  = 400,
                    ""ImagesIncluded""            = 100,
                    ""MaxPhoneNumbers""           = 1,
                    ""EnforceHardLimits""         = false,
                    ""AIOverageRate""             = 0.20,
                    ""SMSOverageRate""            = 0.13,
                    ""WhatsAppOverageRate""       = 0.025,
                    ""ImageOverageRate""          = 0.50,
                    ""EnabledFeatures""           = '{AllFeatures}',
                    ""UpdatedAt""                = NOW() AT TIME ZONE 'UTC';
            ");

            // ── 4. Upsert Business plan ($199 / mo) — conflict on Name ───────────────────────
            migrationBuilder.Sql($@"
                INSERT INTO ""PricingPlans""
                    (""Id"", ""Name"", ""TierSlug"", ""Description"",
                     ""DisplayPrice"", ""SortOrder"", ""IsActive"", ""IsDeleted"", ""TrialDays"",
                     ""AIMinutesIncluded"", ""SMSMessagesIncluded"", ""WhatsAppMessagesIncluded"",
                     ""ImagesIncluded"", ""MaxPhoneNumbers"", ""EnforceHardLimits"",
                     ""AIOverageRate"", ""SMSOverageRate"", ""WhatsAppOverageRate"", ""ImageOverageRate"",
                     ""EnabledFeatures"", ""CreatedAt"")
                VALUES (
                    gen_random_uuid()::text,
                    'Business', 'business',
                    'Every feature included. Highest monthly volume and the lowest overage rates for established teams.',
                    199, 2, true, false, 14,
                    1200, 1000, 1000, 250, 1, false,
                    0.15, 0.10, 0.015, 0.30,
                    '{AllFeatures}',
                    NOW() AT TIME ZONE 'UTC'
                )
                ON CONFLICT (""Name"") DO UPDATE SET
                    ""TierSlug""                  = 'business',
                    ""Description""               = 'Every feature included. Highest monthly volume and the lowest overage rates for established teams.',
                    ""DisplayPrice""              = 199,
                    ""SortOrder""                = 2,
                    ""IsActive""                 = true,
                    ""IsDeleted""                = false,
                    ""TrialDays""                = 14,
                    ""AIMinutesIncluded""         = 1200,
                    ""SMSMessagesIncluded""       = 1000,
                    ""WhatsAppMessagesIncluded""  = 1000,
                    ""ImagesIncluded""            = 250,
                    ""MaxPhoneNumbers""           = 1,
                    ""EnforceHardLimits""         = false,
                    ""AIOverageRate""             = 0.15,
                    ""SMSOverageRate""            = 0.10,
                    ""WhatsAppOverageRate""       = 0.015,
                    ""ImageOverageRate""          = 0.30,
                    ""EnabledFeatures""           = '{AllFeatures}',
                    ""UpdatedAt""                = NOW() AT TIME ZONE 'UTC';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data migrations are intentionally not reversed.
            // Re-activating the old 4th plan would require knowing its original values.
        }
    }
}
