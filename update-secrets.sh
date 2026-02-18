#!/bin/bash
# Quick script to update User Secrets interactively
# Run this from SpeakHalo.API directory

set -e

echo "======================================"
echo "  SpeakHalo User Secrets Updater"
echo "======================================"
echo ""
echo "This will help you update your development credentials."
echo "Press Enter to keep current value, or type new value."
echo ""

cd "$(dirname "$0")/SpeakHalo.API"

# Database
echo "📦 Database Configuration"
echo "Current: Host=localhost;Port=5432;Database=speakhalo_dev;Username=postgres;Password=postgres"
read -p "New Database Connection String (or press Enter to keep): " DB_CONN
if [ ! -z "$DB_CONN" ]; then
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "$DB_CONN"
    echo "✅ Database connection updated"
else
    echo "⏭️  Skipped"
fi
echo ""

# JWT Secret
echo "🔐 JWT Secret Key"
echo "Current: DEVELOPMENT-SECRET-KEY-CHANGE-IN-PRODUCTION-64-CHARS-MINIMUM"
read -p "Generate new random key? (y/n): " GEN_JWT
if [ "$GEN_JWT" = "y" ]; then
    NEW_JWT=$(openssl rand -base64 64 | tr -d '\n')
    dotnet user-secrets set "Jwt:SecretKey" "$NEW_JWT"
    echo "✅ Generated and saved new JWT secret"
else
    read -p "Enter JWT Secret (or press Enter to keep): " JWT_SECRET
    if [ ! -z "$JWT_SECRET" ]; then
        dotnet user-secrets set "Jwt:SecretKey" "$JWT_SECRET"
        echo "✅ JWT secret updated"
    else
        echo "⏭️  Skipped"
    fi
fi
echo ""

# Gemini
echo "🤖 Google Gemini API Key"
echo "Get from: https://aistudio.google.com/apikey"
echo "Current: YOUR-GEMINI-API-KEY-FOR-DEVELOPMENT"
read -p "Gemini API Key (or press Enter to keep): " GEMINI_KEY
if [ ! -z "$GEMINI_KEY" ]; then
    dotnet user-secrets set "Gemini:ApiKey" "$GEMINI_KEY"
    echo "✅ Gemini key updated"
else
    echo "⏭️  Skipped"
fi
echo ""

# Twilio
echo "📞 Twilio Configuration"
echo "Get from: https://console.twilio.com/"
echo "Current Account SID: AC-YOUR-TWILIO-ACCOUNT-SID-FOR-DEV"
read -p "Twilio Account SID (or press Enter to keep): " TWILIO_SID
if [ ! -z "$TWILIO_SID" ]; then
    dotnet user-secrets set "Twilio:AccountSid" "$TWILIO_SID"
    echo "✅ Twilio SID updated"
else
    echo "⏭️  Skipped"
fi

echo "Current Auth Token: your-twilio-auth-token-for-dev"
read -p "Twilio Auth Token (or press Enter to keep): " TWILIO_TOKEN
if [ ! -z "$TWILIO_TOKEN" ]; then
    dotnet user-secrets set "Twilio:AuthToken" "$TWILIO_TOKEN"
    echo "✅ Twilio token updated"
else
    echo "⏭️  Skipped"
fi

echo "Current Phone Number: +1234567890"
read -p "Twilio Phone Number (or press Enter to keep): " TWILIO_PHONE
if [ ! -z "$TWILIO_PHONE" ]; then
    dotnet user-secrets set "Twilio:PhoneNumber" "$TWILIO_PHONE"
    echo "✅ Twilio phone updated"
else
    echo "⏭️  Skipped"
fi
echo ""

# SendGrid
echo "📧 SendGrid API Key"
echo "Get from: https://app.sendgrid.com/settings/api_keys"
echo "Current: SG-YOUR-SENDGRID-API-KEY-FOR-DEV"
read -p "SendGrid API Key (or press Enter to keep): " SENDGRID_KEY
if [ ! -z "$SENDGRID_KEY" ]; then
    dotnet user-secrets set "SendGrid:ApiKey" "$SENDGRID_KEY"
    echo "✅ SendGrid key updated"
else
    echo "⏭️  Skipped"
fi
echo ""

# Google Calendar
echo "📅 Google Calendar Credentials"
echo "Current: /path/to/dev/credentials.json"
read -p "Path to Google credentials JSON (or press Enter to keep): " GOOGLE_CREDS
if [ ! -z "$GOOGLE_CREDS" ]; then
    dotnet user-secrets set "GoogleCalendar:CredentialsPath" "$GOOGLE_CREDS"
    echo "✅ Google credentials path updated"
else
    echo "⏭️  Skipped"
fi
echo ""

echo "======================================"
echo "✅ User Secrets Update Complete!"
echo "======================================"
echo ""
echo "View all secrets with:"
echo "  cd SpeakHalo.API"
echo "  dotnet user-secrets list"
echo ""
echo "Test your app with:"
echo "  cd SpeakHalo.API"
echo "  dotnet run"
echo ""
