using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Serilog;
using SpeakHalo.API.Infrastructure;
using SpeakHalo.API.Middleware;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Application.Services;
using SpeakHalo.Application.Services.Auth;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Infrastructure.Data;
using SpeakHalo.Infrastructure.Services;
using SpeakHalo.Infrastructure.Repositories;
using SpeakHalo.Shared.Helpers;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/speakhalo-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console()
    .WriteTo.File("logs/speakhalo-.txt", rollingInterval: RollingInterval.Day));

var baseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var mainConnectionBuilder = new Npgsql.NpgsqlConnectionStringBuilder(baseConnectionString)
{
    Pooling = true,
    MinPoolSize = 5,
    MaxPoolSize = 100,
    ConnectionIdleLifetime = 300,
    ConnectionPruningInterval = 10,
    Timeout = 30,
    CommandTimeout = 30,
    NoResetOnClose = false,
    Multiplexing = true,
    MaxAutoPrepare = 10,
    AutoPrepareMinUsages = 5
};

var connectionString = mainConnectionBuilder.ConnectionString;

// Hangfire requires multiplexing disabled
var hangfireConnectionBuilder = new Npgsql.NpgsqlConnectionStringBuilder(baseConnectionString)
{
    Pooling = true,
    MinPoolSize = 2,
    MaxPoolSize = 20,
    ConnectionIdleLifetime = 300,
    ConnectionPruningInterval = 10,
    Timeout = 30,
    CommandTimeout = 120,
    KeepAlive = 60,
    TcpKeepAlive = true,
    TcpKeepAliveInterval = 60,
    TcpKeepAliveTime = 60,
    NoResetOnClose = false,
    Multiplexing = false,
    MaxAutoPrepare = 10,
    AutoPrepareMinUsages = 5
};

var hangfireConnectionString = hangfireConnectionBuilder.ConnectionString;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // EF migrations require multiplexing disabled; runtime uses the main (multiplexing-enabled) connection
    var isDesignTime = AppDomain.CurrentDomain.GetAssemblies()
        .Any(a => a.FullName?.StartsWith("Microsoft.EntityFrameworkCore.Design") == true);
    var dbConnectionString = isDesignTime ? hangfireConnectionString : connectionString;

    options.UseNpgsql(dbConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    });

    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddAutoMapper(typeof(SpeakHalo.Application.Mappings.UserMappingProfile).Assembly);

builder.Services.AddValidatorsFromAssemblyContaining<SpeakHalo.Application.Validators.LoginRequestValidator>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICallRepository, CallRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ISMSConversationRepository, SMSConversationRepository>();
builder.Services.AddScoped<ISMSMessageRepository, SMSMessageRepository>();
builder.Services.AddScoped<IWhatsAppConversationRepository, WhatsAppConversationRepository>();
builder.Services.AddScoped<IWhatsAppMessageRepository, WhatsAppMessageRepository>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
builder.Services.AddScoped<IProductKnowledgeRepository, ProductKnowledgeRepository>();
builder.Services.AddScoped<IAIConversationContextRepository, AIConversationContextRepository>();
builder.Services.AddScoped<IAITrainingExampleRepository, AITrainingExampleRepository>();
builder.Services.AddScoped<IMediaFileRepository, MediaFileRepository>();
builder.Services.AddScoped<IKnowledgeBaseArticleRepository, KnowledgeBaseArticleRepository>();
builder.Services.AddScoped<ICommonIssueRepository, CommonIssueRepository>();
builder.Services.AddScoped<IPhoneNumberRepository, PhoneNumberRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

builder.Services.AddScoped<ICookieDefinitionRepository, CookieDefinitionRepository>();
builder.Services.AddScoped<ICookiePolicyRepository, CookiePolicyRepository>();
builder.Services.AddScoped<IUserCookieConsentRepository, UserCookieConsentRepository>();
builder.Services.AddScoped<ICookieAuditLogRepository, CookieAuditLogRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICallService, SpeakHalo.Application.Services.CallService>();
builder.Services.AddScoped<IAppointmentService, SpeakHalo.Application.Services.AppointmentService>();
builder.Services.AddScoped<ICustomerService, SpeakHalo.Application.Services.CustomerService>();
builder.Services.AddScoped<IUserService, SpeakHalo.Application.Services.UserService>();
builder.Services.AddScoped<ISMSConversationService, SpeakHalo.Application.Services.SMSConversationService>();
builder.Services.AddScoped<IWhatsAppService, SpeakHalo.Application.Services.WhatsAppService>();
builder.Services.AddScoped<IWhatsAppPhotoQuoteService, WhatsAppPhotoQuoteService>();
builder.Services.AddScoped<IQuoteService, SpeakHalo.Application.Services.QuoteService>();
builder.Services.AddSingleton<IDisposableEmailValidator, SpeakHalo.Application.Services.DisposableEmailValidator>();
builder.Services.AddScoped<IEmailVerificationService, SpeakHalo.Application.Services.EmailVerificationService>();
builder.Services.AddScoped<INotificationService, SpeakHalo.Application.Services.NotificationService>();
builder.Services.AddScoped<IFeatureFlagService, SpeakHalo.Application.Services.FeatureFlagService>();
builder.Services.AddSingleton<IFeatureGatingService, SpeakHalo.Application.Services.FeatureGatingService>();
builder.Services.AddScoped<IApiKeyService, SpeakHalo.Application.Services.ApiKeyService>();
builder.Services.AddScoped<IAuditLogService, SpeakHalo.Application.Services.AuditLogService>();
builder.Services.AddScoped<IUsageMeteringService, SpeakHalo.Application.Services.UsageMeteringService>();
builder.Services.AddScoped<IUsageTrackingService, SpeakHalo.Infrastructure.Services.UsageTrackingService>();
builder.Services.AddScoped<IAIFollowUpService, SpeakHalo.Infrastructure.Services.AIFollowUpService>();
builder.Services.AddScoped<IInvoiceService, SpeakHalo.Application.Services.InvoiceService>();
builder.Services.AddScoped<IServiceCatalogService, SpeakHalo.Application.Services.ServiceCatalogService>();
builder.Services.AddScoped<ITeamService, SpeakHalo.Application.Services.TeamService>();
builder.Services.AddScoped<ICallerRecognitionService, SpeakHalo.Application.Services.CallerRecognitionService>();

builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<IAiChatService, GeminiChatService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddStandardResilienceHandler();
builder.Services.AddHttpClient<IAiVisionService, GeminiVisionService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
}).AddStandardResilienceHandler();
builder.Services.AddScoped<IGeminiVoiceService, SpeakHalo.Infrastructure.Services.Voice.GeminiLiveVoiceService>();


builder.Services.AddScoped<ITwilioService, TwilioService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IDataExportService, SpeakHalo.Application.Services.DataExportService>();
builder.Services.AddScoped<IAnalyticsService, SpeakHalo.Application.Services.AnalyticsService>();
builder.Services.AddScoped<IAIFunctionToolsService, SpeakHalo.Application.Services.AIFunctionToolsService>();
builder.Services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
builder.Services.AddScoped<IAIAssistantService, AIAssistantService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<ISystemStatusService, SpeakHalo.Application.Services.SystemStatusService>();
builder.Services.AddScoped<OnDemandPhoneNumberService>();
builder.Services.AddScoped<ILanguageDetectionService, SpeakHalo.Application.Services.LanguageDetectionService>();
builder.Services.AddScoped<ISentimentAnalysisService, SpeakHalo.Application.Services.SentimentAnalysisService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<CookieManagementService>();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<SpeakHalo.API.Controllers.PhoneNumberSettings>(
    builder.Configuration.GetSection("PhoneNumbers"));

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "SpeakHalo_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddScoped<SpeakHalo.Application.BackgroundJobs.AppointmentReminderJob>();
builder.Services.AddScoped<SpeakHalo.Application.BackgroundJobs.DataCleanupJob>();
builder.Services.AddScoped<SpeakHalo.Application.BackgroundJobs.MediaProcessingJob>();
builder.Services.AddScoped<SpeakHalo.Application.BackgroundJobs.TrialEnforcementJob>();

builder.Services.AddHostedService<FollowUpProcessingBackgroundService>();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new JwtTokenGenerator(
        config["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"),
        config["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured"),
        config["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured")
    );
});

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
if (Encoding.UTF8.GetByteCount(jwtSecretKey) < 32)
    throw new InvalidOperationException("JWT SecretKey must be at least 32 bytes (256 bits) for HS256 security. Current key is too short.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(5)
    };
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
        new Asp.Versioning.UrlSegmentApiVersionReader(),
        new Asp.Versioning.HeaderApiVersionReader("X-Api-Version"),
        new Asp.Versioning.MediaTypeApiVersionReader("version")
    );
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddControllers(options =>
{
    options.MaxModelValidationErrors = 50;
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600;
});

builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 104857600;
    options.Limits.MaxRequestHeaderCount = 100;
    options.Limits.MaxRequestHeadersTotalSize = 32768;
    options.Limits.MaxRequestLineSize = 8192;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSignalR();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/plain", "application/problem+json" });
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(5)));

    options.AddPolicy("Dashboard", builder => builder
        .Expire(TimeSpan.FromMinutes(2))
        .SetVaryByQuery("*"));

    options.AddPolicy("Analytics", builder => builder
        .Expire(TimeSpan.FromMinutes(10))
        .SetVaryByQuery("period", "months", "days"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? throw new InvalidOperationException("Cors:AllowedOrigins must be configured in appsettings");

builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .WithExposedHeaders("Content-Disposition", "X-Total-Count");
        });
    }
    else
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .WithExposedHeaders("Content-Disposition", "X-Total-Count");
        });

        options.AddPolicy("WebhookPolicy", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database", tags: new[] { "ready", "db" })
    .AddCheck<SpeakHalo.API.HealthChecks.GeminiHealthCheck>("gemini", tags: new[] { "ready", "external" })
    .AddCheck<SpeakHalo.API.HealthChecks.TwilioHealthCheck>("twilio", tags: new[] { "ready", "external" })
    .AddCheck<SpeakHalo.API.HealthChecks.RedisHealthCheck>("redis", tags: new[] { "ready", "cache" })
    .AddCheck<SpeakHalo.API.HealthChecks.DiskSpaceHealthCheck>("disk_space", tags: new[] { "ready", "resources" })
    .AddCheck<SpeakHalo.API.HealthChecks.MemoryHealthCheck>("memory", tags: new[] { "ready", "resources" });

builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(hangfireConnectionString));
});

builder.Services.AddHangfireServer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        var provider = app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"SpeakHalo API {description.GroupName.ToUpperInvariant()}");
        }
    });
}

// Only use app-level compression in development; in production, Cloudflare handles compression
if (!app.Environment.IsProduction())
{
    app.UseResponseCompression();
}

// Forward headers from Cloudflare → Azure App Service reverse proxy chain
// Required for Twilio signature validation and correct client IP detection
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    // Cloudflare and Azure both act as trusted proxies in the chain
    ForwardLimit = 2
});

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// CORS must be before rate limiting so preflight OPTIONS requests aren't rate-limited
app.UseCors();

app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

app.UseCookieConsentValidation(options =>
{
    options.Enabled = true;
    options.AddConsentHeaders = app.Environment.IsDevelopment();
    options.UseDefaultRules = true;

    options.PathConsentRules["/api/v1/analytics"] = CookieCategory.Analytics;
    options.PathConsentRules["/api/v1/marketing"] = CookieCategory.Marketing;
    options.PathConsentRules["/api/v1/social"] = CookieCategory.SocialMedia;

    options.EnforcementMode = app.Environment.IsProduction()
        ? ConsentEnforcementMode.Block
        : ConsentEnforcementMode.LogOnly;
});

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "SpeakHalo Background Jobs",
    StatsPollingInterval = 5000
});

app.UseWebSockets();

app.MapControllers();

// Secured with a one-time stream token generated by TwilioVoiceWebhookController
app.Map("/api/v1/voice-stream/{agentId}", async (HttpContext context, string agentId) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("VoiceStream");

        var streamToken = context.Request.Query["token"].FirstOrDefault();
        if (string.IsNullOrEmpty(streamToken))
        {
            logger.LogWarning("WebSocket connection rejected: missing stream token for agent {AgentId}", agentId);
            context.Response.StatusCode = 401;
            return;
        }

        var streamTokenHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(streamToken))).ToLowerInvariant();

        var unitOfWork = context.RequestServices.GetRequiredService<SpeakHalo.Domain.Interfaces.Repositories.IUnitOfWork>();
        var calls = await unitOfWork.Repository<SpeakHalo.Domain.Entities.Call>()
            .FindAsync(c => c.Notes != null && c.Notes.Contains($"StreamToken: {streamTokenHash}") && !c.IsDeleted);
        if (!calls.Any())
        {
            logger.LogWarning("WebSocket connection rejected: invalid stream token for agent {AgentId}", agentId);
            context.Response.StatusCode = 401;
            return;
        }

        try
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var voiceService = context.RequestServices.GetRequiredService<SpeakHalo.Application.Interfaces.IGeminiVoiceService>();
            var callSid = context.Request.Query["CallSid"].FirstOrDefault() ?? "unknown";
            var callerPhone = context.Request.Query["callerPhone"].FirstOrDefault();
            await voiceService.HandleMediaStreamAsync(webSocket, callSid, agentId, callerPhone, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            // Normal — client disconnected
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Voice stream error for agent {AgentId}", agentId);
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapHub<SpeakHalo.API.Hubs.NotificationHub>("/hubs/notifications");

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow
        }));
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                data = entry.Value.Data
            })
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await context.Response.WriteAsync(result);
    }
});

// Exception details only shown in development to prevent infrastructure info leaks
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var isDev = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                exception = isDev ? entry.Value.Exception?.Message : null,
                data = entry.Value.Data,
                tags = entry.Value.Tags
            })
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await context.Response.WriteAsync(result);
    }
});

app.MapGet("/", () => new
{
    name = "SpeakHalo API",
    version = "1.0.0",
    status = "running",
    timestamp = DateTime.UtcNow
});

RecurringJob.AddOrUpdate<SpeakHalo.Application.BackgroundJobs.AppointmentReminderJob>(
    "appointment-reminders",
    job => job.SendUpcomingRemindersAsync(CancellationToken.None),
    "*/15 * * * *");

RecurringJob.AddOrUpdate<SpeakHalo.Application.BackgroundJobs.DataCleanupJob>(
    "data-cleanup",
    job => job.CleanupOldDataAsync(CancellationToken.None),
    Cron.Weekly);

RecurringJob.AddOrUpdate<SpeakHalo.Application.BackgroundJobs.DataCleanupJob>(
    "failed-calls-cleanup",
    job => job.CleanupFailedCallsAsync(CancellationToken.None),
    Cron.Daily);

RecurringJob.AddOrUpdate<SpeakHalo.Application.BackgroundJobs.MediaProcessingJob>(
    "process-pending-media",
    job => job.ProcessPendingDownloadsAsync(CancellationToken.None),
    "*/5 * * * *");

RecurringJob.AddOrUpdate<SpeakHalo.Application.BackgroundJobs.MediaProcessingJob>(
    "cleanup-old-media",
    job => job.CleanupOldMediaAsync(CancellationToken.None),
    Cron.Daily);

RecurringJob.AddOrUpdate<SpeakHalo.Application.BackgroundJobs.TrialEnforcementJob>(
    "expire-trials",
    job => job.ExpireTrialsAsync(CancellationToken.None),
    Cron.Hourly);

RecurringJob.AddOrUpdate<SpeakHalo.Application.BackgroundJobs.TrialEnforcementJob>(
    "trial-expiration-warnings",
    job => job.SendTrialWarningsAsync(CancellationToken.None),
    Cron.Daily);

if (app.Environment.IsDevelopment() || args.Contains("--seed"))
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            Log.Information("🌱 Seeding cookie management data...");
            await SpeakHalo.Infrastructure.Data.Seeders.DatabaseSeeder.SeedCookieManagementAsync(scope.ServiceProvider);
            Log.Information("✅ Cookie management data seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Cookie management data may already be seeded or seeding failed");
        }

        try
        {
            Log.Information("🌱 Seeding initial users (admin + demo)...");
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SpeakHalo.Infrastructure.Data.DatabaseSeeder>>();
            var userSeeder = new SpeakHalo.Infrastructure.Data.DatabaseSeeder(context, logger);
            await userSeeder.SeedAsync();
            Log.Information("✅ Initial users seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Users may already be seeded or seeding failed");
        }

        try
        {
            Log.Information("🌱 Seeding industry data...");
            var industryContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SpeakHalo.Infrastructure.Data.IndustrySeedData.SeedIndustriesAsync(industryContext);
            Log.Information("✅ Industry data seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Industry data may already be seeded or seeding failed");
        }

        try
        {
            Log.Information("🌱 Seeding industry knowledge templates...");
            var knowledgeContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SpeakHalo.Infrastructure.Data.IndustryKnowledgeTemplateSeedData.SeedAsync(knowledgeContext);
            Log.Information("✅ Industry knowledge templates seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Knowledge templates may already be seeded or seeding failed");
        }
    }
}

try
{
    Log.Information("Starting SpeakHalo API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
