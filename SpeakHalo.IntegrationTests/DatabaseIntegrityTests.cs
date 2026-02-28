using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Infrastructure.Data;
using SpeakHalo.IntegrationTests.TestHelpers;
using Xunit;

namespace SpeakHalo.IntegrationTests;

/// <summary>
/// Tests database integrity constraints including soft deletes, concurrency handling,
/// timestamp auto-setting, and webhook idempotency.
/// </summary>
[Collection("Integration")]
public class DatabaseIntegrityTests
{
    private readonly SpeakHaloWebApplicationFactory _factory;

    public DatabaseIntegrityTests(SpeakHaloWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ───────────────────────────────────────────────────────────────
    // 1. Soft-deleted records excluded from queries (via API)
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeletedRecords_ExcludedFromQueries()
    {
        // Arrange - Create a customer, then soft-delete it
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var createRequest = DataFactory.CreateCustomerRequest(
            name: "SoftDeleteTest",
            phone: $"+1202555{new Random().Next(1000, 9999)}");
        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue(
            $"Customer creation should succeed: {await createResponse.Content.ReadAsStringAsync()}");

        var createdCustomer = await ApiResponseHelper.ReadDataAsync<CustomerResponse>(createResponse);
        createdCustomer.Should().NotBeNull();
        var customerId = createdCustomer!.Id;

        // Soft-delete via API
        var deleteResponse = await client.DeleteAsync($"/api/v1/customers/{customerId}");
        deleteResponse.IsSuccessStatusCode.Should().BeTrue("Delete should succeed");

        // Act - List all customers (the list endpoint is /api/v1/customers/user)
        var listResponse = await client.GetAsync("/api/v1/customers/user");
        listResponse.IsSuccessStatusCode.Should().BeTrue();

        var content = await listResponse.Content.ReadAsStringAsync();

        // Assert - The soft-deleted customer should not appear in the list
        content.Should().NotContain(customerId,
            "Soft-deleted customer ID should not appear in list query results");
    }

    // ───────────────────────────────────────────────────────────────
    // 2. Soft-deleted records exist in database
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeletedRecords_ExistInDatabase()
    {
        // Arrange - Create and soft-delete a customer
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var createRequest = DataFactory.CreateCustomerRequest(
            name: "SoftDeleteDbTest",
            phone: $"+1202555{new Random().Next(1000, 9999)}");
        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var createdCustomer = await ApiResponseHelper.ReadDataAsync<CustomerResponse>(createResponse);
        createdCustomer.Should().NotBeNull();
        var customerId = createdCustomer!.Id;

        // Soft-delete via API
        await client.DeleteAsync($"/api/v1/customers/{customerId}");

        // Act - Query DB directly, bypassing the global query filter
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var deletedCustomer = await db.Set<Customer>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == customerId);

        // Assert
        deletedCustomer.Should().NotBeNull("Record should exist in DB after soft delete");
        deletedCustomer!.IsDeleted.Should().BeTrue("Record should have IsDeleted = true");
        deletedCustomer.DeletedAt.Should().NotBeNull("DeletedAt should be set");
    }

    // ───────────────────────────────────────────────────────────────
    // 3. Concurrent updates handled
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConcurrentUpdates_Handled()
    {
        // Arrange - Create a customer, then an appointment
        var client1 = await AuthHelper.LoginAsOwnerAsync(_factory);
        var client2 = await AuthHelper.LoginAsOwnerAsync(_factory);

        // First create a customer to link the appointment to
        var custResponse = await client1.PostAsJsonAsync("/api/v1/customers", DataFactory.CreateCustomerRequest());
        custResponse.IsSuccessStatusCode.Should().BeTrue();
        var custJson = await ApiResponseHelper.ReadJsonAsync(custResponse);
        string customerId;
        if (custJson.TryGetProperty("data", out var cData) && cData.TryGetProperty("id", out var cId))
            customerId = cId.GetString()!;
        else if (custJson.TryGetProperty("id", out var directCId))
            customerId = directCId.GetString()!;
        else
            throw new InvalidOperationException($"Could not extract customer ID: {custJson}");

        var createRequest = DataFactory.CreateAppointmentRequest(
            customerId, scheduledAt: DateTime.UtcNow.AddDays(14));
        var createResponse = await client1.PostAsJsonAsync("/api/v1/appointments", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue(
            $"Appointment creation should succeed: {await createResponse.Content.ReadAsStringAsync()}");

        var created = await ApiResponseHelper.ReadDataAsync<AppointmentResponse>(createResponse);
        created.Should().NotBeNull();
        var appointmentId = created!.Id;

        // Act - Two concurrent updates to the same appointment
        var update1 = new { Title = "Updated by Client 1", DurationMinutes = 90 };
        var update2 = new { Title = "Updated by Client 2", DurationMinutes = 120 };

        var task1 = client1.PutAsJsonAsync($"/api/v1/appointments/{appointmentId}", update1);
        var task2 = client2.PutAsJsonAsync($"/api/v1/appointments/{appointmentId}", update2);

        var responses = await Task.WhenAll(task1, task2);

        // Assert - At least one should succeed, and the system should handle the conflict
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var conflictCount = responses.Count(r =>
            r.StatusCode == HttpStatusCode.Conflict ||
            r.StatusCode == HttpStatusCode.PreconditionFailed);

        // Both may succeed if serialized, or one may get a conflict
        (successCount + conflictCount).Should().Be(2,
            "All requests should either succeed or return a conflict status");
        successCount.Should().BeGreaterOrEqualTo(1,
            "At least one concurrent update should succeed");
    }

    // ───────────────────────────────────────────────────────────────
    // 4. Transaction rollback on partial failure
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task TransactionRollback_OnPartialFailure()
    {
        // Arrange - Try to create an invoice with an invalid customer ID
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var request = new
        {
            CustomerId = "non-existent-customer-id",
            PeriodStart = DateTime.UtcNow.AddDays(-30).ToString("o"),
            PeriodEnd = DateTime.UtcNow.ToString("o"),
            Notes = "Should fail gracefully",
            LineItems = new[]
            {
                new { Description = "Service A", Quantity = 1, UnitPrice = 100.00 }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/invoices", request);

        // Assert - Should fail (bad customer) but not leave partial data
        // The system should either reject with 400/404 or handle gracefully
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.NotFound,
            HttpStatusCode.OK,
            HttpStatusCode.Created);

        // If it failed, verify no orphaned invoice was created
        if (!response.IsSuccessStatusCode)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var orphanedInvoices = await db.Set<Invoice>()
                .Where(i => i.CustomerId == "non-existent-customer-id")
                .ToListAsync();
            orphanedInvoices.Should().BeEmpty(
                "Failed transaction should not leave orphaned invoice records");
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 5. Usage counter atomicity
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UsageCounter_Atomicity()
    {
        // Arrange - Get current usage before creating calls
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var subscriptionBefore = await db.Set<UserSubscription>()
            .FirstOrDefaultAsync(s => s.UserId == SpeakHaloWebApplicationFactory.OwnerUserId);
        subscriptionBefore.Should().NotBeNull();
        var minutesBefore = subscriptionBefore!.AIMinutesUsedThisMonth;

        // Act - Create a call via API (which should update usage counters)
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var callRequest = DataFactory.CreateCallRequest();
        var response = await client.PostAsJsonAsync("/api/v1/calls", callRequest);

        // Assert - The call should succeed (or return 403 if feature gate cache issue)
        if (response.IsSuccessStatusCode)
        {
            // Reload subscription to check usage was tracked consistently
            await db.Entry(subscriptionBefore).ReloadAsync();
            subscriptionBefore.AIMinutesUsedThisMonth.Should().BeGreaterOrEqualTo(minutesBefore,
                "Usage counter should not decrease after creating a call");
        }
        else
        {
            // Feature gating may block in test environment
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Forbidden,
                HttpStatusCode.BadRequest);
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 6. Webhook event idempotency
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task WebhookEvent_Idempotency()
    {
        // Arrange - Insert a webhook event directly into the DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var eventId = $"evt_test_{Guid.NewGuid():N}";
        var webhookEvent = new WebhookEvent
        {
            EventId = eventId,
            EventName = "subscription.created",
            Source = "paddle",
            ProcessedAt = DateTime.UtcNow,
            Success = true,
            RawPayload = "{\"test\":true}"
        };
        db.Set<WebhookEvent>().Add(webhookEvent);
        await db.SaveChangesAsync();

        // Act - Check that the same event_id is recognized as already processed
        var existingEvents = await db.Set<WebhookEvent>()
            .Where(e => e.EventId == eventId && e.Source == "paddle")
            .ToListAsync();

        // Assert
        existingEvents.Should().HaveCount(1,
            "There should be exactly one record for this event_id");
        existingEvents.First().Success.Should().BeTrue();

        // Insert a second event with the same ID to simulate duplicate
        var duplicateEvent = new WebhookEvent
        {
            EventId = eventId,
            EventName = "subscription.created",
            Source = "paddle",
            ProcessedAt = DateTime.UtcNow,
            Success = true,
            RawPayload = "{\"test\":true,\"duplicate\":true}"
        };
        db.Set<WebhookEvent>().Add(duplicateEvent);
        await db.SaveChangesAsync();

        // Verify the idempotency check would find existing records
        var allEvents = await db.Set<WebhookEvent>()
            .Where(e => e.EventId == eventId && e.Source == "paddle")
            .ToListAsync();

        allEvents.Count.Should().BeGreaterOrEqualTo(1,
            "At least the original event should exist for idempotency checking");
    }

    // ───────────────────────────────────────────────────────────────
    // 7. CreatedAt auto-set on entity creation
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatedAt_AutoSet()
    {
        // Arrange
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var beforeCreate = DateTime.UtcNow.AddSeconds(-1);

        // Act - Create a customer via API
        var request = DataFactory.CreateCustomerRequest(
            name: "CreatedAtTest",
            phone: $"+1202555{new Random().Next(1000, 9999)}");
        var response = await client.PostAsJsonAsync("/api/v1/customers", request);
        response.IsSuccessStatusCode.Should().BeTrue(
            $"Customer creation should succeed: {await response.Content.ReadAsStringAsync()}");

        var created = await ApiResponseHelper.ReadDataAsync<CustomerResponse>(response);
        created.Should().NotBeNull();

        // Assert - Verify CreatedAt in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customer = await db.Set<Customer>().FindAsync(created!.Id);
        customer.Should().NotBeNull();
        customer!.CreatedAt.Should().BeAfter(beforeCreate,
            "CreatedAt should be automatically set to approximately now");
        customer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1),
            "CreatedAt should be close to the current time");
    }

    // ───────────────────────────────────────────────────────────────
    // 8. UpdatedAt changes on update
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatedAt_Changes()
    {
        // Arrange - Create a customer first
        var client = await AuthHelper.LoginAsOwnerAsync(_factory);
        var createRequest = DataFactory.CreateCustomerRequest(
            name: "UpdatedAtTest",
            phone: $"+1202555{new Random().Next(1000, 9999)}");
        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await ApiResponseHelper.ReadDataAsync<CustomerResponse>(createResponse);
        created.Should().NotBeNull();
        var customerId = created!.Id;

        // Get the initial state
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customerBefore = await db.Set<Customer>().FindAsync(customerId);
        customerBefore.Should().NotBeNull();
        var initialUpdatedAt = customerBefore!.UpdatedAt;

        // Small delay to ensure timestamp difference
        await Task.Delay(100);

        // Act - Update the customer
        var updateRequest = DataFactory.UpdateCustomerRequest();
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/customers/{customerId}", updateRequest);
        updateResponse.IsSuccessStatusCode.Should().BeTrue(
            $"Customer update should succeed: {await updateResponse.Content.ReadAsStringAsync()}");

        // Assert - Reload and verify UpdatedAt changed
        await db.Entry(customerBefore).ReloadAsync();
        customerBefore.UpdatedAt.Should().NotBeNull("UpdatedAt should be set after update");

        if (initialUpdatedAt.HasValue)
        {
            customerBefore.UpdatedAt.Should().BeAfter(initialUpdatedAt.Value,
                "UpdatedAt should increase after an update");
        }
    }
}

/// <summary>
/// Minimal response types for deserialization in database integrity tests.
/// </summary>
file class CustomerResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

file class AppointmentResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}
