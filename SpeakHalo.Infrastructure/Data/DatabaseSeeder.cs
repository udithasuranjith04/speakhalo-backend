using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;

#pragma warning disable CS0618

namespace SpeakHalo.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            await SeedAdminUserAsync();
            await SeedDemoUsersAsync();
            await SeedFeatureFlagsAsync();
            await SeedNotificationsAsync();
            await SeedDemoBusinessDataAsync();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var adminPassword = Environment.GetEnvironmentVariable("SPEAKHALO_ADMIN_SEED_PASSWORD") ?? "Admin@123456";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: 12);

        var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@speakhalo.com");
        if (existingAdmin != null)
        {
            var existingAuth = await _context.Set<UserAuth>().FirstOrDefaultAsync(a => a.UserId == existingAdmin.Id);
            if (existingAuth != null)
            {
                existingAuth.HashedPassword = hashedPassword;
                existingAuth.PasswordMustChange = false;
                existingAuth.EmailVerified = true;
                _context.Set<UserAuth>().Update(existingAuth);
            }

            // Ensure subscription exists
            var existingSub = await _context.Set<UserSubscription>().FirstOrDefaultAsync(s => s.UserId == existingAdmin.Id);
            if (existingSub == null)
            {
                await _context.Set<UserSubscription>().AddAsync(new UserSubscription
                {
                    UserId = existingAdmin.Id,
                    Plan = "business",
                    HasActivePayment = true,
                    RequiresPayment = false,
                    EnforceHardLimits = false,
                    AIMinutesIncluded = 99999,
                    WhatsAppMessagesIncluded = 99999,
                    SMSMessagesIncluded = 99999,
                    ImagesIncluded = 99999,
                    BillingCycleStartDate = DateTime.UtcNow,
                    NextBillingDate = DateTime.UtcNow.AddYears(10)
                });
                _logger.LogInformation("Created missing subscription for admin");
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin user already exists, reset password");
            return;
        }

        var adminUser = new User
        {
            Name = "Admin User",
            Email = "admin@speakhalo.com",
            Role = UserRole.Admin,
            IsActive = true,
            BusinessName = "SpeakHalo Admin",
            Industry = ServiceType.HomeServices,
            OnboardingCompleted = true,
            OnboardingStep = 999,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(adminUser);
        await _context.SaveChangesAsync();

        var adminAuth = new UserAuth
        {
            UserId = adminUser.Id,
            HashedPassword = hashedPassword,
            PasswordMustChange = false,
            EmailVerified = true
        };
        await _context.Set<UserAuth>().AddAsync(adminAuth);

        var adminSubscription = new UserSubscription
        {
            UserId = adminUser.Id,
            Plan = "business",
            HasActivePayment = true,
            RequiresPayment = false,
            EnforceHardLimits = false,
            AIMinutesIncluded = 99999,
            WhatsAppMessagesIncluded = 99999,
            SMSMessagesIncluded = 99999,
            ImagesIncluded = 99999,
            BillingCycleStartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddYears(10)
        };
        await _context.Set<UserSubscription>().AddAsync(adminSubscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded admin user: admin@speakhalo.com");
    }

    private async Task SeedDemoUsersAsync()
    {
        await SeedOrResetUserAsync("owner@demo.com", "Demo@123456", "Sarah Thompson", UserRole.Owner,
            "Sparkle Clean Co.", ServiceType.Cleaning, "business");
        await SeedOrResetUserAsync("member@demo.com", "Demo@123456", "David Chen", UserRole.Member,
            "Premier Clean Services", ServiceType.Cleaning, "starter");
    }

    private async Task SeedOrResetUserAsync(string email, string password, string name,
        UserRole role, string businessName, ServiceType industry, string plan)
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            existingUser.HashedPassword = hashedPassword;
            _context.Users.Update(existingUser);

            var existingAuth = await _context.Set<UserAuth>().FirstOrDefaultAsync(a => a.UserId == existingUser.Id);
            if (existingAuth != null)
            {
                existingAuth.HashedPassword = hashedPassword;
                existingAuth.PasswordMustChange = false;
                existingAuth.EmailVerified = true;
                _context.Set<UserAuth>().Update(existingAuth);
            }

            // Ensure subscription exists (may have been missing from earlier seeds)
            var existingSub = await _context.Set<UserSubscription>().FirstOrDefaultAsync(s => s.UserId == existingUser.Id);
            if (existingSub == null)
            {
                await _context.Set<UserSubscription>().AddAsync(new UserSubscription
                {
                    UserId = existingUser.Id,
                    Plan = plan,
                    HasActivePayment = true,
                    RequiresPayment = false,
                    EnforceHardLimits = plan == "starter",
                    AIMinutesIncluded = plan == "business" ? 500 : 100,
                    WhatsAppMessagesIncluded = plan == "business" ? 1000 : 200,
                    SMSMessagesIncluded = plan == "business" ? 200 : 50,
                    ImagesIncluded = plan == "business" ? 100 : 20,
                    BillingCycleStartDate = DateTime.UtcNow.AddDays(-15),
                    NextBillingDate = DateTime.UtcNow.AddDays(15)
                });
                _logger.LogInformation("Created missing subscription for {Email}", email);
            }
            else if (!existingSub.HasActivePayment)
            {
                existingSub.HasActivePayment = true;
                existingSub.IsTemporarilySuspended = false;
                _context.Set<UserSubscription>().Update(existingSub);
                _logger.LogInformation("Reactivated subscription for {Email}", email);
            }

            // Ensure profile exists
            var existingProfile = await _context.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == existingUser.Id);
            if (existingProfile == null)
            {
                await _context.Set<UserProfile>().AddAsync(new UserProfile
                {
                    UserId = existingUser.Id,
                    BusinessName = businessName,
                    BusinessAddress = email == "owner@demo.com" ? "500 Terry A Francois Blvd, San Francisco, CA 94158" : null,
                    Industry = industry
                });
                _logger.LogInformation("Created missing profile for {Email}", email);
            }
            else if (email == "owner@demo.com" && string.IsNullOrWhiteSpace(existingProfile.BusinessAddress))
            {
                existingProfile.BusinessAddress = "500 Terry A Francois Blvd, San Francisco, CA 94158";
                _context.Set<UserProfile>().Update(existingProfile);
                _logger.LogInformation("Set business address for {Email}", email);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("User {Email} already exists, reset password", email);
            return;
        }

        var user = new User
        {
            Name = name,
            Email = email,
            Role = role,
            IsActive = true,
            BusinessName = businessName,
            Industry = industry,
            OnboardingCompleted = true,
            OnboardingStep = 999,
            CreatedAt = DateTime.UtcNow.AddDays(-14)
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        await _context.Set<UserAuth>().AddAsync(new UserAuth
        {
            UserId = user.Id,
            HashedPassword = hashedPassword,
            PasswordMustChange = false,
            EmailVerified = true
        });

        await _context.Set<UserSubscription>().AddAsync(new UserSubscription
        {
            UserId = user.Id,
            Plan = plan,
            HasActivePayment = true,
            RequiresPayment = false,
            EnforceHardLimits = plan == "starter",
            AIMinutesIncluded = plan == "business" ? 500 : 100,
            WhatsAppMessagesIncluded = plan == "business" ? 1000 : 200,
            SMSMessagesIncluded = plan == "business" ? 200 : 50,
            ImagesIncluded = plan == "business" ? 100 : 20,
            BillingCycleStartDate = DateTime.UtcNow.AddDays(-15),
            NextBillingDate = DateTime.UtcNow.AddDays(15)
        });

        await _context.Set<UserProfile>().AddAsync(new UserProfile
        {
            UserId = user.Id,
            BusinessName = businessName,
            BusinessAddress = email == "owner@demo.com" ? "500 Terry A Francois Blvd, San Francisco, CA 94158" : null,
            Industry = industry
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded user: {Email}", email);
    }

    private async Task SeedNotificationsAsync()
    {
        var ownerUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "owner@demo.com");
        if (ownerUser == null) return;

        var existingCount = await _context.Set<Notification>().CountAsync(n => n.UserId == ownerUser.Id);
        if (existingCount > 0)
        {
            _logger.LogInformation("Notifications already seeded for owner, skipping");
            return;
        }

        var now = DateTime.UtcNow;
        var notifications = new[]
        {
            new Notification
            {
                UserId = ownerUser.Id,
                Title = "New Appointment Booked",
                Message = "A new HVAC maintenance appointment has been scheduled for tomorrow at 10:00 AM.",
                Type = "appointment",
                ActionUrl = "/dashboard/appointments",
                IsRead = false,
                CreatedAt = now.AddMinutes(-15)
            },
            new Notification
            {
                UserId = ownerUser.Id,
                Title = "Missed Call",
                Message = "You missed a call from +1 (650) 555-2222. The caller left a voicemail about plumbing services.",
                Type = "call",
                IsRead = false,
                CreatedAt = now.AddHours(-2)
            },
            new Notification
            {
                UserId = ownerUser.Id,
                Title = "Call Completed",
                Message = "Call with +1 (212) 555-9999 completed. Duration: 6 minutes. Appointment was scheduled.",
                Type = "call",
                ActionUrl = "/dashboard/calls",
                IsRead = false,
                CreatedAt = now.AddHours(-4)
            },
            new Notification
            {
                UserId = ownerUser.Id,
                Title = "New Customer Added",
                Message = "John Doe has been added as a new customer from an inbound call.",
                Type = "info",
                ActionUrl = "/dashboard/customers",
                IsRead = true,
                ReadAt = now.AddHours(-5),
                CreatedAt = now.AddHours(-6)
            },
            new Notification
            {
                UserId = ownerUser.Id,
                Title = "Weekly Summary",
                Message = "This week: 28 calls handled, 5 appointments booked, 3 new customers. Great job!",
                Type = "info",
                IsRead = true,
                ReadAt = now.AddDays(-1),
                CreatedAt = now.AddDays(-1)
            },
        };

        await _context.Set<Notification>().AddRangeAsync(notifications);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} notifications for owner", notifications.Length);
    }

    private async Task SeedFeatureFlagsAsync()
    {
        if (await _context.FeatureFlags.AnyAsync())
        {
            _logger.LogInformation("Feature flags already exist, skipping seed");
            return;
        }

        var featureFlags = new[]
        {
            new FeatureFlag
            {
                Name = "ai_voice_assistant",
                Description = "Enable AI-powered voice assistant for calls",
                IsEnabled = true,
                RolloutPercentage = 100,
                CreatedAt = DateTime.UtcNow
            },
            new FeatureFlag
            {
                Name = "whatsapp_integration",
                Description = "Enable WhatsApp messaging integration",
                IsEnabled = true,
                RolloutPercentage = 100,
                CreatedAt = DateTime.UtcNow
            },
            new FeatureFlag
            {
                Name = "advanced_analytics",
                Description = "Enable advanced analytics dashboard",
                IsEnabled = true,
                RolloutPercentage = 100,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.FeatureFlags.AddRangeAsync(featureFlags);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} feature flags", featureFlags.Length);
    }
    private async Task SeedDemoBusinessDataAsync()
    {
        var ownerUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "owner@demo.com");
        if (ownerUser == null) return;

        var userId = ownerUser.Id;

        // ─── STEP 1: DELETE ALL existing demo data for a clean slate ───
        var existingLineItems = await _context.Set<InvoiceLineItem>()
            .Where(li => _context.Set<Invoice>().Where(inv => inv.UserId == userId).Select(inv => inv.Id).Contains(li.InvoiceId))
            .ToListAsync();
        if (existingLineItems.Count > 0) _context.Set<InvoiceLineItem>().RemoveRange(existingLineItems);

        var existingInvoices = await _context.Set<Invoice>().Where(i => i.UserId == userId).ToListAsync();
        if (existingInvoices.Count > 0) _context.Set<Invoice>().RemoveRange(existingInvoices);

        var existingCatalog = await _context.Set<ServiceCatalogItem>().Where(s => s.UserId == userId).ToListAsync();
        if (existingCatalog.Count > 0) _context.Set<ServiceCatalogItem>().RemoveRange(existingCatalog);

        var existingAppointments = await _context.Set<Appointment>().Where(a => a.UserId == userId).ToListAsync();
        if (existingAppointments.Count > 0) _context.Set<Appointment>().RemoveRange(existingAppointments);

        // Delete transcript entries for calls first (child FK)
        var callIds = await _context.Set<Call>().Where(c => c.UserId == userId).Select(c => c.Id).ToListAsync();
        if (callIds.Count > 0)
        {
            var transcripts = await _context.Set<TranscriptEntry>().Where(t => callIds.Contains(t.CallId)).ToListAsync();
            if (transcripts.Count > 0) _context.Set<TranscriptEntry>().RemoveRange(transcripts);
        }

        var existingCalls = await _context.Set<Call>().Where(c => c.UserId == userId).ToListAsync();
        if (existingCalls.Count > 0) _context.Set<Call>().RemoveRange(existingCalls);

        var existingCustomers = await _context.Customers.Where(c => c.UserId == userId).ToListAsync();
        if (existingCustomers.Count > 0) _context.Customers.RemoveRange(existingCustomers);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Cleared all demo business data for owner user");

        // ─── STEP 2: SEED FRESH DATA ───
        var now = DateTime.UtcNow;
        var random = new Random(42); // fixed seed for reproducible data

        // ── Customers ──
        var customers = new[]
        {
            new Customer { UserId = userId, Name = "John Martinez", Phone = "+16505551001", Email = "john.m@example.com", Address = "123 Oak St", City = "San Francisco", State = "CA", ZipCode = "94102", Latitude = 37.7749, Longitude = -122.4194, LeadStatus = LeadStatus.Converted, LeadScore = 85, TotalSpend = 1250m, JobCount = 4, Source = "referral", ConvertedAt = now.AddDays(-45), LastContactedAt = now.AddDays(-3), LastServiceDate = now.AddDays(-7), CreatedAt = now.AddDays(-60) },
            new Customer { UserId = userId, Name = "Emily Chen", Phone = "+16505551002", Email = "emily.c@example.com", Address = "456 Pine Ave", City = "Oakland", State = "CA", ZipCode = "94610", Latitude = 37.8044, Longitude = -122.2712, LeadStatus = LeadStatus.Converted, LeadScore = 92, TotalSpend = 3200m, JobCount = 8, Source = "google", ConvertedAt = now.AddDays(-90), LastContactedAt = now.AddDays(-1), LastServiceDate = now.AddDays(-5), CreatedAt = now.AddDays(-120) },
            new Customer { UserId = userId, Name = "Robert Wilson", Phone = "+16505551003", Email = "rwilson@example.com", Address = "789 Maple Dr", City = "Berkeley", State = "CA", ZipCode = "94704", Latitude = 37.8716, Longitude = -122.2727, LeadStatus = LeadStatus.Converted, LeadScore = 78, TotalSpend = 950m, JobCount = 3, Source = "website", ConvertedAt = now.AddDays(-30), LastContactedAt = now.AddDays(-5), LastServiceDate = now.AddDays(-14), CreatedAt = now.AddDays(-50) },
            new Customer { UserId = userId, Name = "Sarah Johnson", Phone = "+16505551004", Email = "sarah.j@example.com", Address = "321 Elm Blvd", City = "San Jose", State = "CA", ZipCode = "95112", Latitude = 37.3382, Longitude = -121.8863, LeadStatus = LeadStatus.Converted, LeadScore = 88, TotalSpend = 2100m, JobCount = 6, Source = "referral", ConvertedAt = now.AddDays(-75), LastContactedAt = now.AddDays(-2), LastServiceDate = now.AddDays(-10), CreatedAt = now.AddDays(-100) },
            new Customer { UserId = userId, Name = "Michael Brown", Phone = "+16505551005", Email = "mbrown@example.com", Address = "654 Cedar Ln", City = "Palo Alto", State = "CA", ZipCode = "94301", Latitude = 37.4419, Longitude = -122.1430, LeadStatus = LeadStatus.Qualified, LeadScore = 65, TotalSpend = 450m, JobCount = 2, Source = "yelp", ConvertedAt = now.AddDays(-20), LastContactedAt = now.AddDays(-4), CreatedAt = now.AddDays(-35) },
            new Customer { UserId = userId, Name = "Lisa Davis", Phone = "+16505551006", Email = "lisa.d@example.com", Address = "987 Birch Way", City = "Mountain View", State = "CA", ZipCode = "94040", Latitude = 37.3861, Longitude = -122.0839, LeadStatus = LeadStatus.New, LeadScore = 40, Source = "call", LastContactedAt = now.AddDays(-1), CreatedAt = now.AddDays(-3) },
            new Customer { UserId = userId, Name = "David Kim", Phone = "+16505551007", Email = "dkim@example.com", Address = "147 Walnut St", City = "Fremont", State = "CA", ZipCode = "94538", Latitude = 37.5485, Longitude = -121.9886, LeadStatus = LeadStatus.Converted, LeadScore = 90, TotalSpend = 4500m, JobCount = 12, Source = "google", ConvertedAt = now.AddDays(-150), LastContactedAt = now.AddDays(-2), LastServiceDate = now.AddDays(-3), CreatedAt = now.AddDays(-180) },
            new Customer { UserId = userId, Name = "Amanda Lee", Phone = "+16505551008", Email = "alee@example.com", Address = "258 Spruce Ct", City = "Sunnyvale", State = "CA", ZipCode = "94086", Latitude = 37.3688, Longitude = -122.0363, LeadStatus = LeadStatus.Contacted, LeadScore = 55, Source = "website", LastContactedAt = now.AddDays(-6), CreatedAt = now.AddDays(-15) },
        };

        await _context.Customers.AddRangeAsync(customers);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} customers", customers.Length);

        // ── Calls (65 calls spread over last 60 days - ALL timestamps strictly in the past) ──
        var calls = new List<Call>();
        var callerPhones = customers.Select(c => c.Phone).ToArray();
        var outcomes = new[] { CallOutcome.Booked, CallOutcome.Callback, CallOutcome.Voicemail, CallOutcome.Missed, CallOutcome.Booked, CallOutcome.Booked };
        var sentiments = new[] { CallSentiment.Positive, CallSentiment.Neutral, CallSentiment.Positive, CallSentiment.Neutral, CallSentiment.Negative };
        var serviceTypes = new[] { ServiceType.Cleaning, ServiceType.HomeServices, ServiceType.Plumbing, ServiceType.Electrical, ServiceType.Landscaping };

        for (int i = 0; i < 65; i++)
        {
            // Always at least 1 day ago to guarantee past timestamps
            var daysAgo = random.Next(1, 61);
            var hour = random.Next(8, 19);
            var minute = random.Next(0, 60);
            var callDate = now.AddDays(-daysAgo).Date.AddHours(hour).AddMinutes(minute);
            var customerIdx = random.Next(customers.Length);
            var duration = random.Next(30, 600);
            var outcome = outcomes[random.Next(outcomes.Length)];
            var isAnswered = outcome != CallOutcome.Missed && outcome != CallOutcome.Voicemail;

            calls.Add(new Call
            {
                UserId = userId,
                CustomerId = customers[customerIdx].Id,
                CallerPhone = callerPhones[customerIdx],
                CallerName = customers[customerIdx].Name,
                RecipientPhone = "+14155550100",
                Direction = random.Next(3) == 0 ? "outbound" : "inbound",
                DurationSeconds = isAnswered ? duration : 0,
                Cost = Math.Round((decimal)(duration / 60.0) * 0.015m, 4),
                Outcome = outcome,
                Sentiment = isAnswered ? sentiments[random.Next(sentiments.Length)] : null,
                ServiceType = serviceTypes[random.Next(serviceTypes.Length)],
                Summary = isAnswered ? $"Call about {serviceTypes[random.Next(serviceTypes.Length)].ToString().ToLower()} services" : null,
                AppointmentScheduled = outcome == CallOutcome.Booked,
                QuoteRequested = random.Next(4) == 0,
                CreatedAt = callDate,
                EndedAt = isAnswered ? callDate.AddSeconds(duration) : callDate.AddSeconds(15),
            });
        }

        await _context.Set<Call>().AddRangeAsync(calls);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} calls (all past timestamps verified)", calls.Count);

        // ── Appointments (30 appointments - past + some future scheduled) ──
        var appointments = new List<Appointment>();
        // Realistic distribution: ~60% Completed, ~10% NoShow, ~10% Cancelled, ~20% Scheduled/future
        var pastStatuses = new[] { AppointmentStatus.Completed, AppointmentStatus.Completed, AppointmentStatus.Completed, AppointmentStatus.Completed, AppointmentStatus.Completed, AppointmentStatus.Completed, AppointmentStatus.NoShow, AppointmentStatus.Cancelled };

        for (int i = 0; i < 30; i++)
        {
            var daysOffset = random.Next(-45, 15); // some past, some future
            var hour = random.Next(8, 17);
            var scheduledAt = now.AddDays(daysOffset).Date.AddHours(hour);
            var status = daysOffset > 0 ? AppointmentStatus.Scheduled : pastStatuses[random.Next(pastStatuses.Length)];
            var customerIdx = random.Next(customers.Length);
            var svcType = serviceTypes[random.Next(serviceTypes.Length)];
            var cost = Math.Round((decimal)(random.Next(50, 500)), 2);
            // CreatedAt is always in the past (before the scheduled date or now, whichever is earlier)
            var createdAt = scheduledAt.AddDays(-random.Next(1, 7));
            if (createdAt > now) createdAt = now.AddDays(-random.Next(1, 5));

            appointments.Add(new Appointment
            {
                UserId = userId,
                CustomerId = customers[customerIdx].Id,
                ScheduledAt = scheduledAt,
                CompletedAt = status == AppointmentStatus.Completed ? scheduledAt.AddHours(random.Next(1, 4)) : null,
                DurationMinutes = random.Next(30, 180),
                Status = status,
                ServiceType = svcType,
                Title = $"{svcType} Service - {customers[customerIdx].Name}",
                Location = customers[customerIdx].Address,
                EstimatedCost = cost,
                ActualCost = status == AppointmentStatus.Completed ? cost + random.Next(-20, 50) : null,
                CancelledAt = status == AppointmentStatus.Cancelled ? scheduledAt.AddDays(-1) : null,
                CancellationReason = status == AppointmentStatus.Cancelled ? "Customer requested reschedule" : null,
                CreatedAt = createdAt,
            });
        }

        await _context.Set<Appointment>().AddRangeAsync(appointments);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} appointments", appointments.Count);

        // ── Service Catalog Items ──
        var catalogItems = new[]
        {
            new ServiceCatalogItem { UserId = userId, Name = "Basic House Cleaning", Description = "Standard cleaning for rooms up to 1500 sqft", BasePrice = 120m, Category = "Cleaning", EstimatedDurationMinutes = 120, IsActive = true, CreatedAt = now.AddDays(-30) },
            new ServiceCatalogItem { UserId = userId, Name = "Deep Cleaning", Description = "Thorough deep cleaning including appliances, baseboards, and windows", BasePrice = 250m, Category = "Cleaning", EstimatedDurationMinutes = 240, IsActive = true, CreatedAt = now.AddDays(-30) },
            new ServiceCatalogItem { UserId = userId, Name = "Move-Out Cleaning", Description = "Full property cleaning for move-out/move-in", BasePrice = 350m, Category = "Cleaning", EstimatedDurationMinutes = 300, IsActive = true, CreatedAt = now.AddDays(-28) },
            new ServiceCatalogItem { UserId = userId, Name = "Carpet Cleaning", Description = "Professional steam carpet cleaning per room", BasePrice = 75m, Category = "Carpet", EstimatedDurationMinutes = 45, IsActive = true, CreatedAt = now.AddDays(-25) },
            new ServiceCatalogItem { UserId = userId, Name = "Window Cleaning", Description = "Interior and exterior window cleaning", BasePrice = 150m, Category = "Windows", EstimatedDurationMinutes = 90, IsActive = true, CreatedAt = now.AddDays(-25) },
            new ServiceCatalogItem { UserId = userId, Name = "Office Cleaning", Description = "Commercial office cleaning (up to 2000 sqft)", BasePrice = 200m, Category = "Commercial", EstimatedDurationMinutes = 150, IsActive = true, CreatedAt = now.AddDays(-20) },
            new ServiceCatalogItem { UserId = userId, Name = "Post-Construction Cleanup", Description = "Heavy-duty cleanup after renovation or construction", BasePrice = 500m, Category = "Specialty", EstimatedDurationMinutes = 480, IsActive = false, CreatedAt = now.AddDays(-15) },
        };

        await _context.Set<ServiceCatalogItem>().AddRangeAsync(catalogItems);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} service catalog items", catalogItems.Length);

        // ── Invoices with Line Items ──
        var invoiceDefs = new[]
        {
            new { Customer = customers[0], Status = "paid", DaysAgo = 25, Items = new[] { (catalogItems[0], 1), (catalogItems[3], 2) }, PaidDaysAgo = 22 },
            new { Customer = customers[1], Status = "paid", DaysAgo = 20, Items = new[] { (catalogItems[1], 1) }, PaidDaysAgo = 18 },
            new { Customer = customers[3], Status = "paid", DaysAgo = 15, Items = new[] { (catalogItems[0], 1), (catalogItems[4], 1) }, PaidDaysAgo = 13 },
            new { Customer = customers[6], Status = "paid", DaysAgo = 12, Items = new[] { (catalogItems[5], 2) }, PaidDaysAgo = 10 },
            new { Customer = customers[2], Status = "unpaid", DaysAgo = 7, Items = new[] { (catalogItems[1], 1), (catalogItems[3], 3) }, PaidDaysAgo = 0 },
            new { Customer = customers[4], Status = "unpaid", DaysAgo = 3, Items = new[] { (catalogItems[0], 2) }, PaidDaysAgo = 0 },
            new { Customer = customers[1], Status = "unpaid", DaysAgo = 1, Items = new[] { (catalogItems[2], 1), (catalogItems[4], 1), (catalogItems[3], 2) }, PaidDaysAgo = 0 },
        };

        for (int i = 0; i < invoiceDefs.Length; i++)
        {
            var def = invoiceDefs[i];
            var invoiceDate = now.AddDays(-def.DaysAgo);

            var lineItems = def.Items.Select(item => new InvoiceLineItem
            {
                Description = item.Item1.Name,
                Quantity = item.Item2,
                UnitPrice = item.Item1.BasePrice,
                Amount = item.Item1.BasePrice * item.Item2,
                Category = item.Item1.Category.ToLower()
            }).ToList();

            var subtotal = lineItems.Sum(li => li.Amount);
            var taxRate = 0.0875m;
            var tax = Math.Round(subtotal * taxRate, 2);

            var invoice = new Invoice
            {
                UserId = userId,
                CustomerId = def.Customer.Id,
                InvoiceNumber = $"INV-{now.Year}{now.Month:D2}-{(i + 1):D4}",
                PeriodStart = invoiceDate,
                PeriodEnd = invoiceDate,
                Subtotal = subtotal,
                Tax = tax,
                Total = subtotal + tax,
                Status = def.Status,
                PaidAt = def.Status == "paid" ? now.AddDays(-def.PaidDaysAgo) : null,
                PaymentMethod = def.Status == "paid" ? (i % 2 == 0 ? "Credit Card" : "Cash") : null,
                Notes = def.Status == "unpaid" ? "Payment pending" : null,
                CreatedAt = invoiceDate,
            };

            await _context.Set<Invoice>().AddAsync(invoice);
            await _context.SaveChangesAsync();

            foreach (var li in lineItems)
                li.InvoiceId = invoice.Id;
            await _context.Set<InvoiceLineItem>().AddRangeAsync(lineItems);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Seeded {Count} invoices with line items", invoiceDefs.Length);
    }
}

#pragma warning restore CS0618
