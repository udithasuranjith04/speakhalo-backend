#!/bin/bash
# Create SpeakHalo Database - Execute SQL Script

# Database connection details
DB_HOST="your-db-host.example.com"
DB_PORT="25060"
DB_NAME="speakhalo"
DB_USER="doadmin"
DB_PASSWORD="${DB_PASSWORD:?Set DB_PASSWORD before running}"

echo "Creating SpeakHalo database tables..."
echo "Host: $DB_HOST"
echo "Database: $DB_NAME"
echo ""

# Check if psql is available
if command -v psql &> /dev/null; then
    echo "Using psql to execute migration..."
    PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f InitialMigration.sql

    if [ $? -eq 0 ]; then
        echo ""
        echo "✅ Database created successfully!"
        echo ""
        echo "Verifying tables..."
        PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "\dt"
    else
        echo "❌ Database creation failed!"
        exit 1
    fi
else
    echo "psql not found. Using dotnet ef instead..."
    cd "$(dirname "$0")"
    export ASPNETCORE_ENVIRONMENT=Production

    # Try to apply migrations using EF Core
    dotnet ef database update --project SpeakHalo.Infrastructure --startup-project SpeakHalo.API --connection "Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"

    if [ $? -eq 0 ]; then
        echo ""
        echo "✅ Database created successfully using EF Core!"
    else
        echo "❌ Database creation failed!"
        echo ""
        echo "Manual instructions:"
        echo "1. Install PostgreSQL client: brew install postgresql"
        echo "2. Run: PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f InitialMigration.sql"
        exit 1
    fi
fi
