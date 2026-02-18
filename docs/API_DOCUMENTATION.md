# SpeakHalo API Documentation

## Table of Contents
- [Overview](#overview)
- [Authentication](#authentication)
- [Base URLs](#base-urls)
- [Billing & Webhooks](#billing--webhooks)
  - [Lemon Squeezy Webhooks](#lemon-squeezy-webhooks)
  - [Invoice Endpoints](#invoice-endpoints)
  - [Usage Metering](#usage-metering)
- [Analytics](#analytics)
- [Error Handling](#error-handling)
- [Rate Limiting](#rate-limiting)

---

## Overview

SpeakHalo provides a RESTful API for managing AI-powered communication services. The API uses JSON for requests and responses.

**API Version:** v1
**Base URL:** `https://api.speakhalo.com/api/v1`
**Development URL:** `http://localhost:5098/api/v1`

---

## Authentication

All API endpoints (except webhooks) require JWT authentication.

### Obtaining a Token

**POST** `/auth/login`

```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userId": "user-id-123",
    "email": "user@example.com",
    "expiresAt": "2026-02-09T12:00:00Z"
  }
}
```

### Using the Token

Include the JWT token in the `Authorization` header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Base URLs

| Environment | URL |
|------------|-----|
| Production | `https://api.speakhalo.com` |
| Development | `http://localhost:5098` |

---

## Billing & Webhooks

### Lemon Squeezy Webhooks

SpeakHalo uses Lemon Squeezy for payment processing. Webhooks are used to synchronize subscription and payment events.

#### Webhook Endpoint

**POST** `/billing/webhooks/lemonsqueezy`

**Authentication:** HMAC-SHA256 signature verification (no JWT required)

**Headers:**
- `X-Signature` (required): HMAC-SHA256 signature of the payload
- `X-Event-Name` (required): Event type (e.g., `subscription_created`)

#### Supported Events

| Event Name | Description |
|-----------|-------------|
| `order_created` | New order/purchase created |
| `subscription_created` | New subscription started |
| `subscription_updated` | Subscription details changed |
| `subscription_cancelled` | Subscription cancelled by user |
| `subscription_resumed` | Cancelled subscription reactivated |
| `subscription_expired` | Subscription expired (end of billing period) |
| `subscription_paused` | Subscription temporarily paused |
| `subscription_unpaused` | Paused subscription resumed |
| `subscription_payment_success` | Subscription payment succeeded |
| `subscription_payment_failed` | Subscription payment failed |
| `subscription_payment_recovered` | Failed payment recovered |
| `license_key_created` | License key generated |
| `license_key_updated` | License key updated |

#### Example: Subscription Created

**Request:**
```json
{
  "meta": {
    "event_name": "subscription_created"
  },
  "data": {
    "type": "subscriptions",
    "id": "12345",
    "attributes": {
      "store_id": 1,
      "customer_id": 67890,
      "subscription_id": 12345,
      "product_name": "Professional Plan",
      "variant_name": "Professional Monthly",
      "user_email": "user@example.com",
      "status": "active",
      "price": 49.99,
      "total_price": 49.99
    }
  }
}
```

**Response:**
```json
200 OK
```

#### Webhook Security

All webhooks are verified using HMAC-SHA256 signature:

```csharp
// Signature verification (simplified)
var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

if (computedSignature != providedSignature)
{
    return BadRequest("Invalid signature");
}
```

#### Webhook Features

✅ **Idempotency Protection**
- Duplicate webhooks with the same event ID are automatically detected
- Prevents duplicate processing and ensures data consistency

✅ **Transaction Management**
- All webhook processing is wrapped in database transactions
- Ensures atomic operations (all succeed or all rollback)

✅ **Smart Retry Handling**
- Maximum 5 retry attempts for transient failures
- Returns 200 OK after max retries to stop provider from retrying
- Returns 200 OK for validation errors (permanent failures)

✅ **Email Notifications**
- Automatic welcome email on subscription creation
- Payment failure notifications
- Subscription cancellation confirmations
- Subscription expiration notices

---

### Invoice Endpoints

#### Get User Invoices

**GET** `/billing/invoices`

**Authentication:** Required (JWT)

**Query Parameters:**
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 10)

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "inv-123",
        "invoiceNumber": "LS-98765",
        "periodStart": "2026-01-01T00:00:00Z",
        "periodEnd": "2026-01-31T23:59:59Z",
        "subtotal": 49.99,
        "tax": 0.00,
        "total": 49.99,
        "status": "paid",
        "paidAt": "2026-01-01T10:30:00Z",
        "paymentMethod": "lemon_squeezy"
      }
    ],
    "totalItems": 15,
    "currentPage": 1,
    "totalPages": 2
  }
}
```

#### Get Invoice by ID

**GET** `/billing/invoices/{invoiceId}`

**Authentication:** Required (JWT)

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "id": "inv-123",
    "invoiceNumber": "LS-98765",
    "lineItems": [
      {
        "description": "Professional Plan - Monthly",
        "quantity": 1,
        "unitPrice": 49.99,
        "amount": 49.99
      }
    ],
    "subtotal": 49.99,
    "tax": 0.00,
    "total": 49.99,
    "status": "paid"
  }
}
```

---

### Usage Metering

Track API usage for billing purposes.

#### Record Usage

**POST** `/billing/usage`

**Authentication:** Required (JWT)

**Request:**
```json
{
  "service": "gemini",
  "operation": "chat_completion",
  "quantity": 1000,
  "metadata": {
    "model": "gemini-2.0-flash",
    "tokens": 1000
  }
}
```

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "usageId": "usage-123",
    "service": "gemini",
    "quantity": 1000,
    "cost": 0.02,
    "recordedAt": "2026-02-08T15:30:00Z"
  }
}
```

#### Get Usage Report

**GET** `/billing/usage/report`

**Authentication:** Required (JWT)

**Query Parameters:**
- `startDate` (optional): Start date (ISO 8601)
- `endDate` (optional): End date (ISO 8601)

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "period": {
      "start": "2026-02-01T00:00:00Z",
      "end": "2026-02-28T23:59:59Z"
    },
    "totalCost": 15.75,
    "breakdown": {
      "gemini": 10.50,
      "twilio": 3.25,
      "sendgrid": 2.00
    }
  }
}
```

---

## Analytics

### Get Churn Rate

**GET** `/analytics/churn-rate`

**Authentication:** Required (JWT, Admin role)

**Query Parameters:**
- `months` (optional): Number of months to analyze (default: 12)

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "2025-02": 2.5,
    "2025-03": 3.1,
    "2025-04": 2.8,
    "2025-05": 2.2
  }
}
```

**Churn Rate Calculation:**
```
Churn Rate = (Churned Subscriptions / Active Subscriptions at Start) × 100
```

Churned subscriptions include:
- Voluntary cancellations (`subscription_cancelled`)
- Expired subscriptions (`subscription_expired`)
- Payment failures after 5 retry attempts

---

## Error Handling

All errors follow a consistent format:

```json
{
  "isSuccess": false,
  "errors": [
    "Error message 1",
    "Error message 2"
  ],
  "message": "Operation failed"
}
```

### HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request (validation error) |
| 401 | Unauthorized (missing/invalid token) |
| 403 | Forbidden (insufficient permissions) |
| 404 | Not Found |
| 429 | Too Many Requests (rate limit exceeded) |
| 500 | Internal Server Error |

### Common Error Responses

#### 401 Unauthorized
```json
{
  "isSuccess": false,
  "errors": ["Unauthorized access"],
  "message": "Authentication required"
}
```

#### 400 Bad Request
```json
{
  "isSuccess": false,
  "errors": [
    "Email is required",
    "Password must be at least 8 characters"
  ],
  "message": "Validation failed"
}
```

#### 429 Rate Limit Exceeded
```json
{
  "isSuccess": false,
  "errors": ["Rate limit exceeded"],
  "message": "Too many requests. Please try again later."
}
```

---

## Rate Limiting

API endpoints are rate-limited to prevent abuse.

**Default Limits:**
- Authenticated users: **100 requests per minute**
- Unauthenticated endpoints: **20 requests per minute**

**Headers:**
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 87
X-RateLimit-Reset: 1707401400
```

---

## Webhook Development & Testing

### Local Testing with ngrok

1. **Start your local API:**
   ```bash
   cd SpeakHalo.API
   dotnet run
   ```

2. **Expose localhost with ngrok:**
   ```bash
   ngrok http 5098
   ```

3. **Configure Lemon Squeezy webhook:**
   - Go to https://app.lemonsqueezy.com
   - Settings → Webhooks → Create Webhook
   - URL: `https://your-ngrok-url.ngrok.io/api/v1/billing/webhooks/lemonsqueezy`
   - Select events to subscribe

4. **Test webhook manually:**
   ```bash
   # Compute signature
   PAYLOAD='{"meta":{"event_name":"subscription_created"},"data":{"id":"test"}}'
   SECRET="your_webhook_secret"
   SIGNATURE=$(echo -n "$PAYLOAD" | openssl dgst -sha256 -hmac "$SECRET" | sed 's/^.* //')

   # Send webhook
   curl -X POST http://localhost:5098/api/v1/billing/webhooks/lemonsqueezy \
     -H "Content-Type: application/json" \
     -H "X-Signature: $SIGNATURE" \
     -H "X-Event-Name: subscription_created" \
     -d "$PAYLOAD"
   ```

### Webhook Monitoring

Monitor webhook processing via Hangfire dashboard:

**URL:** `http://localhost:5098/hangfire`
**Authentication:** Required (authenticated users in dev, admins in production)

---

## Best Practices

### Security
✅ Always use HTTPS in production
✅ Store API keys in environment variables or Azure Key Vault
✅ Rotate webhook secrets regularly
✅ Validate all webhook signatures
✅ Implement rate limiting for public endpoints

### Performance
✅ Use pagination for list endpoints
✅ Cache frequently accessed data
✅ Implement database indexes for common queries
✅ Use async/await for all I/O operations

### Error Handling
✅ Return appropriate HTTP status codes
✅ Provide meaningful error messages
✅ Log errors with context for debugging
✅ Don't expose sensitive information in errors

---

## Support

**Email:** support@speakhalo.com
**Documentation:** https://docs.speakhalo.com
**API Status:** https://status.speakhalo.com

---

**Last Updated:** February 8, 2026
**API Version:** 1.0
