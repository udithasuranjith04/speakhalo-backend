# SpeakHalo - Secrets Management (.NET Standard Way)

This guide follows Microsoft's official recommendations for managing secrets in .NET applications.

## 🎯 The .NET Way

| Environment | Method | Committed to Git? |
|-------------|--------|-------------------|
| **Development** | User Secrets | ❌ Never |
| **Production** | Environment Variables or Azure Key Vault | ❌ Never |
| **Configuration** | appsettings.json | ✅ Yes (no secrets!) |

---

## 🔧 Development Setup (User Secrets)

User Secrets stores sensitive data **outside your project folder** in your user profile, so it's never committed to Git.

### Already Initialized!

Your project already has User Secrets initialized with these placeholder values. Update them with your actual development credentials.

### View Current Secrets

```bash
cd SpeakHalo.API
dotnet user-secrets list
```

### Update Development Secrets

**Database Connection (Development):**
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=speakhalo_dev;Username=postgres;Password=YOUR_DEV_PASSWORD"
```

**JWT Secret (Development):**
```bash
# Generate a development key
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 64)"
```

**Gemini API Key:**
```bash
dotnet user-secrets set "Gemini:ApiKey" "YOUR-ACTUAL-GEMINI-KEY"
```

**Twilio Credentials:**
```bash
dotnet user-secrets set "Twilio:AccountSid" "AC-YOUR-TWILIO-SID"
dotnet user-secrets set "Twilio:AuthToken" "your-twilio-token"
dotnet user-secrets set "Twilio:PhoneNumber" "+1234567890"
```

**SendGrid API Key:**
```bash
dotnet user-secrets set "SendGrid:ApiKey" "SG.YOUR-SENDGRID-KEY"
```

**Lemon Squeezy (Payment Provider):**
```bash
# Get these from: https://app.lemonsqueezy.com
# 1. Settings → API → Create API Key
# 2. Settings → Webhooks → Signing Secret (webhook you created)
# 3. Settings → General → Store ID (or check URL when viewing products)

dotnet user-secrets set "LemonSqueezy:ApiKey" "YOUR-LEMON-SQUEEZY-API-KEY"
dotnet user-secrets set "LemonSqueezy:WebhookSecret" "whsec_YOUR-WEBHOOK-SIGNING-SECRET"
dotnet user-secrets set "LemonSqueezy:StoreId" "YOUR-STORE-ID"
```

**Google Calendar:**
```bash
dotnet user-secrets set "GoogleCalendar:CredentialsPath" "/path/to/credentials.json"
```

### Remove a Secret

```bash
dotnet user-secrets remove "ConnectionStrings:DefaultConnection"
```

### Clear All Secrets

```bash
dotnet user-secrets clear
```

### Where Are User Secrets Stored?

**macOS/Linux:**
```
~/.microsoft/usersecrets/<user_secrets_id>/secrets.json
```

**Windows:**
```
%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json
```

Your User Secrets ID: `dcf84ea2-8086-4faf-9e1e-b9a1a709ed6a`

---

## 🚀 Production Setup (Environment Variables)

### Option 1: Set Environment Variables on Server

**Linux/macOS (bash):**

Add to `/etc/environment` or `~/.bashrc`:

```bash
export ASPNETCORE_ENVIRONMENT="Production"
export ConnectionStrings__DefaultConnection="Host=your-db-host.example.com;Port=25060;Database=speakhalo;Username=doadmin;Password=YOUR_PROD_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;Timeout=30;Command Timeout=30;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100"
export Jwt__SecretKey="YOUR_PRODUCTION_JWT_SECRET_64_CHARS_MINIMUM"
export Gemini__ApiKey="YOUR-GEMINI-API-KEY"
export Twilio__AccountSid="AC-YOUR-TWILIO-SID"
export Twilio__AuthToken="your-twilio-token"
export Twilio__PhoneNumber="+1234567890"
export SendGrid__ApiKey="SG.YOUR-SENDGRID-KEY"
export LemonSqueezy__ApiKey="YOUR-LEMON-SQUEEZY-API-KEY"
export LemonSqueezy__WebhookSecret="whsec_YOUR-WEBHOOK-SECRET"
export LemonSqueezy__StoreId="YOUR-STORE-ID"
export GoogleCalendar__CredentialsPath="/path/to/prod/credentials.json"
```

**Windows (PowerShell):**
```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
$env:ConnectionStrings__DefaultConnection="Host=...;Password=..."
$env:Jwt__SecretKey="YOUR_PRODUCTION_JWT_SECRET"
# ... etc
```

**Note:** Use double underscore `__` to represent nested JSON structure!

### Option 2: Azure App Service

1. Go to Azure Portal → Your App Service
2. Settings → Configuration → Application Settings
3. Add each secret as a setting:

| Name | Value |
|------|-------|
| `ConnectionStrings__DefaultConnection` | Your connection string |
| `Jwt__SecretKey` | Your JWT secret |
| `Gemini__ApiKey` | Your Gemini key |
| `Twilio__AccountSid` | Your Twilio SID |
| ... | ... |

### Option 3: Docker / Docker Compose

**docker-compose.yml:**
```yaml
version: '3.8'
services:
  api:
    image: speakhalo-api:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
      - Jwt__SecretKey=${JWT_SECRET}
      - Gemini__ApiKey=${GEMINI_KEY}
      - Twilio__AccountSid=${TWILIO_SID}
      - Twilio__AuthToken=${TWILIO_TOKEN}
      - SendGrid__ApiKey=${SENDGRID_KEY}
      - LemonSqueezy__ApiKey=${LEMONSQUEEZY_KEY}
      - LemonSqueezy__WebhookSecret=${LEMONSQUEEZY_WEBHOOK_SECRET}
      - LemonSqueezy__StoreId=${LEMONSQUEEZY_STORE_ID}
    ports:
      - "5098:5098"
```

**.env file (NOT committed to Git):**
```
DB_CONNECTION=Host=...;Password=...
JWT_SECRET=your-jwt-secret
GEMINI_KEY=your-gemini-api-key
TWILIO_SID=AC...
TWILIO_TOKEN=...
SENDGRID_KEY=SG...
```

### Option 4: Kubernetes Secrets

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: speakhalo-secrets
type: Opaque
stringData:
  connection-string: "Host=...;Password=..."
  jwt-secret: "your-jwt-secret"
  gemini-key: "your-gemini-api-key"
  twilio-sid: "AC..."
  twilio-token: "..."
  sendgrid-key: "SG..."
```

Apply and mount in deployment:
```yaml
env:
  - name: ConnectionStrings__DefaultConnection
    valueFrom:
      secretKeyRef:
        name: speakhalo-secrets
        key: connection-string
```

---

## 🔐 Option 5: Azure Key Vault (Recommended for Enterprise)

Azure Key Vault is the most secure option for production secrets.

### 1. Install Package

```bash
cd SpeakHalo.API
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

### 2. Update Program.cs

```csharp
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = new Uri("https://speakhalo-vault.vault.azure.net/");
    builder.Configuration.AddAzureKeyVault(
        keyVaultUrl,
        new DefaultAzureCredential());
}

// Rest of configuration...
```

### 3. Store Secrets in Key Vault

```bash
# Install Azure CLI
az login

# Create secrets (replace -- with : in .NET notation)
az keyvault secret set --vault-name speakhalo-vault \
  --name "ConnectionStrings--DefaultConnection" \
  --value "Host=...;Password=..."

az keyvault secret set --vault-name speakhalo-vault \
  --name "Jwt--SecretKey" \
  --value "your-jwt-secret"

az keyvault secret set --vault-name speakhalo-vault \
  --name "Gemini--ApiKey" \
  --value "your-gemini-api-key"
```

**Note:** Azure Key Vault uses `--` (double dash) instead of `:` (colon) in secret names.

### 4. Grant Access

```bash
# For your App Service (using Managed Identity)
az webapp identity assign --resource-group myResourceGroup --name speakhalo-api

# Grant Key Vault access
az keyvault set-policy --name speakhalo-vault \
  --object-id <app-service-managed-identity-id> \
  --secret-permissions get list
```

---

## 🔍 How .NET Reads Configuration

.NET reads configuration in this order (later sources override earlier):

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (Development, Production, etc.)
3. **User Secrets** (Development only)
4. **Environment Variables** (overrides everything except command-line)
5. **Azure Key Vault** (if configured)
6. Command-line arguments (highest priority)

### Example Configuration Lookup

For `ConnectionStrings:DefaultConnection`:

1. Check `appsettings.json` → Found but empty: `""`
2. Check `appsettings.Production.json` → Found but empty: `""`
3. Check User Secrets (Dev only) → Found: `"Host=localhost..."`
4. Check Environment Variable → Found: `ConnectionStrings__DefaultConnection`
5. **Use Environment Variable** (highest priority wins)

---

## ✅ Best Practices

### DO:
- ✅ Use User Secrets for development
- ✅ Use Environment Variables or Key Vault for production
- ✅ Keep `appsettings.json` in source control (without secrets)
- ✅ Use different secrets for each environment
- ✅ Rotate secrets regularly
- ✅ Use strong, randomly generated secrets (64+ chars for JWT)
- ✅ Limit access to production secrets

### DON'T:
- ❌ Never commit secrets to Git
- ❌ Never use production secrets in development
- ❌ Never share secrets via email or chat
- ❌ Never hardcode secrets in source code
- ❌ Never commit `.env` files
- ❌ Never log secret values

---

## 🧪 Testing Configuration

### Test in Development

```bash
cd SpeakHalo.API
dotnet run
# Check logs to see if secrets are loaded
```

### Test Environment Variables

```bash
# Set test variable
export ConnectionStrings__DefaultConnection="test-connection-string"

# Run and check if loaded
dotnet run

# Verify
echo $ConnectionStrings__DefaultConnection
```

### Verify Secrets Are Not in Git

```bash
# Check what's being tracked
git ls-files | grep -E "secret|appsettings"

# Should show:
# - appsettings.json (YES - no secrets)
# - appsettings.Production.json (YES - no secrets)
# - appsettings.Development.json (YES - no secrets)
# Should NOT show:
# - secrets.json
# - .env
# - start-production.sh
```

---

## 🆘 Troubleshooting

### Issue: Can't find connection string

**Solution:** Check environment variable name uses double underscore:
```bash
# WRONG
export ConnectionStrings:DefaultConnection="..."

# CORRECT
export ConnectionStrings__DefaultConnection="..."
```

### Issue: User Secrets not loading in development

**Solution:** Check User Secrets are initialized:
```bash
dotnet user-secrets list
# If empty or error, reinitialize:
dotnet user-secrets init
```

### Issue: Production secrets not loading

**Solution:** Verify environment variables are set:
```bash
# Linux/macOS
printenv | grep -E "ConnectionStrings|Jwt|Gemini"

# Windows PowerShell
Get-ChildItem Env: | Where-Object {$_.Name -match "ConnectionStrings|Jwt|Gemini"}
```

---

## 📚 Additional Resources

- [Safe Storage of App Secrets - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Key Vault Configuration - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)
- [How to Store Secrets Securely in .NET](https://adrianbailador.github.io/blog/23-store-secrets-dotnet/)
- [Securing Connection Strings in ASP.NET Core](https://www.c-sharpcorner.com/article/securing-connection-strings-and-appsettings-in-asp-net-core/)

---

## 🎯 Quick Reference

### Development (Local Machine)
```bash
# Update your development credentials
cd SpeakHalo.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_DEV_DB"
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_KEY"
dotnet run
```

### Production (Server/Cloud)
```bash
# Set environment variables on server
export ConnectionStrings__DefaultConnection="YOUR_PROD_DB"
export Gemini__ApiKey="YOUR_GEMINI_KEY"
cd SpeakHalo.API && dotnet run
```

### Production (Azure)
```bash
# Use Azure Key Vault (most secure)
az keyvault secret set --vault-name speakhalo-vault \
  --name "Gemini--ApiKey" \
  --value "YOUR_GEMINI_KEY"
```

---

**Remember:** The .NET way is User Secrets for development, Environment Variables or Key Vault for production. Never commit secrets to Git!
