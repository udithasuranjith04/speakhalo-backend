using System.Text.Json;

namespace SpeakHalo.IntegrationTests.TestHelpers;

/// <summary>
/// Factory methods for creating test data payloads for API requests.
/// All methods return anonymous objects ready for PostAsJsonAsync/PutAsJsonAsync.
/// </summary>
public static class DataFactory
{
    private static int _phoneCounter;

    /// <summary>Generate a unique US phone number for each call.</summary>
    private static string UniquePhone() =>
        $"+1202555{Interlocked.Increment(ref _phoneCounter):D4}";

    // --- Calls ---
    // Required: UserId, FromNumber, ToNumber, Direction
    public static object CreateCallRequest(string? fromNumber = null, string? toNumber = null, string? customerId = null) => new
    {
        UserId = "will-be-overridden-by-server",
        FromNumber = fromNumber ?? UniquePhone(),
        ToNumber = toNumber ?? UniquePhone(),
        Direction = "inbound",
        CustomerId = customerId
    };

    public static object UpdateCallRequest() => new
    {
        Summary = "Updated call summary",
        DurationSeconds = 120
    };

    // --- Appointments ---
    // Required: CustomerId, ScheduledAt
    public static object CreateAppointmentRequest(string customerId, DateTime? scheduledAt = null) => new
    {
        CustomerId = customerId,
        ScheduledAt = (scheduledAt ?? DateTime.UtcNow.AddDays(7)).ToString("o"),
        DurationMinutes = 60,
        ServiceType = "Cleaning",
        Location = "123 Test St",
        Notes = "Test appointment"
    };

    public static object UpdateAppointmentRequest() => new
    {
        DurationMinutes = 90,
        Location = "456 Updated St",
        Notes = "Updated appointment"
    };

    // --- Customers ---
    // Required: Phone (must be unique per user)
    public static object CreateCustomerRequest(string? name = null, string? phone = null) => new
    {
        Name = name ?? "Test Customer",
        Phone = phone ?? UniquePhone(),
        Email = $"customer{Guid.NewGuid():N}@test.com",
        Address = "123 Main St",
        City = "New York",
        State = "NY",
        ZipCode = "10001",
        Notes = "Test customer notes"
    };

    public static object UpdateCustomerRequest(string? phone = null) => new
    {
        Name = "Updated Customer",
        Phone = phone ?? UniquePhone(),
        Email = "updated@test.com",
        Address = "456 Updated Ave",
        City = "Los Angeles",
        State = "CA",
        ZipCode = "90001"
    };

    // --- Voice Agents ---
    public static object CreateVoiceAgentRequest(string? name = null) => new
    {
        Name = name ?? "Test Voice Agent",
        SystemPrompt = "You are a helpful AI voice agent for testing purposes.",
        VoiceName = "Kore",
        Language = "en-US",
        MaxCallDurationMinutes = 15,
        LocationName = "Test Location",
        LocationAddress = "123 Test St, New York, NY",
        LocationPhone = "+12025551000"
    };

    public static object UpdateVoiceAgentRequest() => new
    {
        Name = "Updated Voice Agent",
        SystemPrompt = "Updated system prompt for testing.",
        VoiceName = "Puck"
    };

    // --- Knowledge Base ---
    public static object CreateKnowledgeBaseArticleRequest(string? title = null) => new
    {
        Title = title ?? "Test Article",
        Content = "This is test content for the knowledge base article.",
        Category = "General",
        Tags = new[] { "test", "article" }
    };

    // --- Service Catalog ---
    public static object CreateServiceCatalogItemRequest(string? name = null) => new
    {
        Name = name ?? "Test Service",
        Description = "A test service for integration testing",
        BasePrice = 99.99,
        Category = "General",
        EstimatedDurationMinutes = 60,
        IsActive = true
    };

    // --- SMS ---
    // Required: UserId, ToNumber, Content
    public static object CreateSendSmsRequest(string? toNumber = null) => new
    {
        UserId = "will-be-overridden-by-server",
        ToNumber = toNumber ?? "+12025551234",
        Content = "Test SMS message"
    };

    // --- WhatsApp ---
    // Required: UserId, ToNumber, Content
    public static object CreateSendWhatsAppRequest(string? toNumber = null) => new
    {
        UserId = "will-be-overridden-by-server",
        ToNumber = toNumber ?? "+12025551234",
        Content = "Test WhatsApp message"
    };

    // --- Notifications ---
    public static object CreateNotificationRequest() => new
    {
        Title = "Test Notification",
        Message = "This is a test notification",
        Type = "info"
    };

    // --- Trial Invites ---
    public static object CreateTrialInviteRequest(string? email = null) => new
    {
        Email = email ?? $"trial{Guid.NewGuid():N}@test.com",
        PricingPlanId = SpeakHaloWebApplicationFactory.PricingPlanId,
        TrialDays = 30,
        Notes = "Test trial invite"
    };

    // --- Register Trial ---
    public static object CreateRegisterTrialRequest(string token, string? email = null) => new
    {
        Email = email ?? $"trial{Guid.NewGuid():N}@test.com",
        Name = "Trial User",
        Password = "TrialPassword123!",
        BusinessName = "Trial Business",
        Token = token
    };

    // --- Invoices ---
    public static object CreateInvoiceRequest(string? customerId = null) => new
    {
        CustomerId = customerId,
        PeriodStart = DateTime.UtcNow.AddDays(-30).ToString("o"),
        PeriodEnd = DateTime.UtcNow.ToString("o"),
        Notes = "Test invoice",
        LineItems = new[]
        {
            new { Description = "Service A", Quantity = 1, UnitPrice = 100.00 },
            new { Description = "Service B", Quantity = 2, UnitPrice = 50.00 }
        }
    };

    // --- Team ---
    public static object CreateTeamRequest() => new
    {
        Name = "Test Team"
    };

    public static object CreateTeamInviteRequest(string? email = null) => new
    {
        Email = email ?? $"invite{Guid.NewGuid():N}@test.com",
        Role = "Member"
    };

    // --- Cookie Consent ---
    public static object CreateCookieConsentRequest(bool analytics = true, bool marketing = false) => new
    {
        Essential = true,
        Functional = true,
        Analytics = analytics,
        Performance = true,
        Marketing = marketing,
        Advertising = false,
        SocialMedia = false
    };

    // --- Branding ---
    public static object CreateBrandingRequest() => new
    {
        BrandName = "Test Brand",
        Tagline = "Test Tagline",
        PrimaryColor = "#FF5733",
        LogoUrl = "https://example.com/logo.png"
    };

    // --- SQL Injection payloads ---
    public static IEnumerable<string> SqlInjectionPayloads => new[]
    {
        "'; DROP TABLE Users; --",
        "1' OR '1'='1",
        "admin'--",
        "1; SELECT * FROM Users",
        "' UNION SELECT NULL, NULL, NULL --",
        "1' AND 1=1 --",
        "' OR 1=1#",
        "admin\" OR \"\"=\""
    };

    // --- XSS payloads ---
    public static IEnumerable<string> XssPayloads => new[]
    {
        "<script>alert('xss')</script>",
        "<img src=x onerror=alert('xss')>",
        "javascript:alert('xss')",
        "<svg onload=alert('xss')>",
        "'\"><script>alert('xss')</script>",
        "<iframe src=\"javascript:alert('xss')\">",
        "<body onload=alert('xss')>"
    };

    // --- Admin User Management ---
    public static object CreateAdminUserRequest(string? email = null) => new
    {
        Email = email ?? $"newuser{Guid.NewGuid():N}@test.com",
        Name = "New Admin User",
        BusinessName = "New Business",
        Industry = "Cleaning",
        Plan = "Professional"
    };

    public static object SetUserLimitsRequest() => new
    {
        AIMinutesIncluded = 2000,
        WhatsAppMessagesIncluded = 1000,
        SMSMessagesIncluded = 500,
        ImagesIncluded = 200,
        MaxPhoneNumbers = 5,
        EnforceHardLimits = true
    };

    // --- Pricing Plans ---
    public static object CreatePricingPlanRequest(string? name = null) => new
    {
        Name = name ?? "Test Plan",
        Description = "A test pricing plan",
        DisplayPrice = 49.99,
        TrialDays = 7,
        AIMinutesIncluded = 500,
        WhatsAppMessagesIncluded = 250,
        SMSMessagesIncluded = 100,
        ImagesIncluded = 50,
        MaxPhoneNumbers = 1,
        EnforceHardLimits = false,
        IsActive = true,
        SortOrder = 10,
        TierSlug = "test-plan"
    };
}
