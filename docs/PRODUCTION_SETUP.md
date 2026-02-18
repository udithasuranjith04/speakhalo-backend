# SpeakHalo Production Configuration - .NET Standard Approach

## Overview

This guide follows Microsoft's recommended practices for ASP.NET Core configuration:
- Non-sensitive settings in `appsettings.Production.json`
- Sensitive credentials via **Environment Variables**
- No credentials committed to source control

## Configuration Hierarchy

.NET reads configuration in this order (later sources override earlier):
1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific)
3. **Environment Variables** (highest priority for secrets)
4. Command-line arguments

## Required Environment Variables for Production

Set these environment variables on your production server/container:

### Database Configuration
```bash
export ConnectionStrings__DefaultConnection="Host=your-db-host.example.com;Port=25060;Database=speakhalo;Username=doadmin;Password=YOUR_DATABASE_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;Timeout=30;Command Timeout=30;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100"
```

### JWT Authentication
```bash
export Jwt__SecretKey="YOUR_64_CHARACTER_SECRET_KEY_HERE"
```

**Generate a secure JWT secret:**
```bash
openssl rand -base64 64
```

### Gemini Configuration
```bash
export Gemini__ApiKey="YOUR_GEMINI_API_KEY"
```

### Twilio Configuration
```bash
export Twilio__AccountSid="ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
export Twilio__AuthToken="your_twilio_auth_token"
export Twilio__PhoneNumber="+1234567890"
```

### SendGrid Configuration
```bash
export SendGrid__ApiKey="SG.YOUR_SENDGRID_API_KEY"
```

### Google Calendar Configuration
```bash
export GoogleCalendar__CredentialsPath="/path/to/google-credentials.json"
```

### ASP.NET Core Environment
```bash
export ASPNETCORE_ENVIRONMENT="Production"
```

## How to Start the Application

### Option 1: Set Environment Variables Manually (Recommended for Production)

On your production server:

1. Set all environment variables (add to `/etc/environment` or similar):
```bash
export ASPNETCORE_ENVIRONMENT="Production"
export ConnectionStrings__DefaultConnection="Host=...;Password=..."
export Jwt__SecretKey="..."
export Gemini__ApiKey="..."
export Twilio__AccountSid="..."
export Twilio__AuthToken="..."
export Twilio__PhoneNumber="..."
export SendGrid__ApiKey="..."
```

2. Start the application:
```bash
cd SpeakHalo.API
dotnet run
```

### Option 2: Use a Startup Script (Development/Testing)

Create `start-prod.sh`:
```bash
#!/bin/bash
export ASPNETCORE_ENVIRONMENT="Production"
export ConnectionStrings__DefaultConnection="Host=...;Password=..."
export Jwt__SecretKey="..."
export Gemini__ApiKey="..."
export Twilio__AccountSid="..."
export Twilio__AuthToken="..."
export SendGrid__ApiKey="..."

cd SpeakHalo.API
dotnet run
```

Make executable and run:
```bash
chmod +x start-prod.sh
./start-prod.sh
```

### Option 3: Docker/Kubernetes (Production Deployment)

**Docker Compose:**
```yaml
version: '3.8'
services:
  api:
    image: speakhalo-api:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=...;Password=...
      - Jwt__SecretKey=${JWT_SECRET}
      - Gemini__ApiKey=${GEMINI_API_KEY}
      - Twilio__AccountSid=${TWILIO_SID}
      - Twilio__AuthToken=${TWILIO_TOKEN}
      - SendGrid__ApiKey=${SENDGRID_KEY}
    ports:
      - "5098:5098"
```

**Kubernetes Secret:**
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
  sendgrid-key: "SG...."
```

### Option 4: Azure App Service

Configure in Azure Portal → Configuration → Application Settings:
- `ConnectionStrings__DefaultConnection`
- `Jwt__SecretKey`
- `Gemini__ApiKey`
- `Twilio__AccountSid`
- `Twilio__AuthToken`
- `SendGrid__ApiKey`

### Option 5: AWS Elastic Beanstalk

Configure in EB Console → Configuration → Software → Environment Properties:
- Same environment variable names as above

## Using Azure Key Vault (Enterprise Recommended)

For enterprise production deployments, use Azure Key Vault:

1. Install package:
```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
```

2. Update `Program.cs`:
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

3. Store secrets in Key Vault:
```bash
az keyvault secret set --vault-name your-keyvault --name "ConnectionStrings--DefaultConnection" --value "Host=..."
az keyvault secret set --vault-name your-keyvault --name "Jwt--SecretKey" --value "..."
az keyvault secret set --vault-name your-keyvault --name "Gemini--ApiKey" --value "your-gemini-api-key"
```

## Verifying Configuration

Check that environment variables are loaded correctly:

```bash
# Start the application and check logs
cd SpeakHalo.API
dotnet run

# In another terminal, test health endpoint
curl http://localhost:5098/health
```

Expected output: `Healthy`

## Security Best Practices

✅ **DO:**
- Use environment variables for all sensitive data
- Store connection strings with credentials in environment variables only
- Use Azure Key Vault or AWS Secrets Manager for production
- Rotate secrets regularly
- Use managed identities when possible (Azure/AWS)

❌ **DON'T:**
- Never commit appsettings files with real credentials
- Never hardcode passwords in source code
- Never commit `.env` files to git
- Don't log sensitive configuration values
- Don't share environment variables via insecure channels

## Troubleshooting

### Issue: Application can't find configuration

**Solution:** Verify environment variables are set:
```bash
printenv | grep -E "ConnectionStrings|Jwt|Gemini|Twilio|SendGrid"
```

### Issue: Database connection fails

**Solution:** Check connection string format:
```bash
echo $ConnectionStrings__DefaultConnection
```

Ensure it includes:
- Host
- Port
- Database
- Username
- Password
- SSL Mode=Require

### Issue: JWT authentication fails

**Solution:** Verify JWT secret is at least 64 characters:
```bash
echo $Jwt__SecretKey | wc -c
```

## Production Checklist

Before deploying to production:

- [ ] All environment variables configured on production server
- [ ] JWT secret key is 64+ characters and randomly generated
- [ ] Database credentials are correct and tested
- [ ] All API keys (Gemini, Twilio, SendGrid) are production keys
- [ ] `ASPNETCORE_ENVIRONMENT` is set to "Production"
- [ ] SSL/TLS is enabled for database connections
- [ ] Application logs are configured for production (Warning level)
- [ ] Health check endpoint is accessible
- [ ] Swagger is disabled or protected in production
- [ ] CORS is configured correctly
- [ ] Rate limiting is enabled
- [ ] Secrets are stored securely (not in source control)

## Additional Resources

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Safe Storage of App Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Key Vault Configuration](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)
- [Environment Variables in .NET](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables)
