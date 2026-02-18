#!/bin/bash
# Create SpeakHalo Database - Execute SQL Script

# Database connection details (set via environment variables)
DB_HOST="${DATABASE_HOST:?DATABASE_HOST environment variable is required}"
DB_PORT="${DATABASE_PORT:-5432}"
DB_NAME="${DATABASE_NAME:-speakhalo}"
DB_USER="${DATABASE_USER:?DATABASE_USER environment variable is required}"
DB_PASSWORD="${DATABASE_PASSWORD:?DATABASE_PASSWORD environment variable is required}"

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
        echo "Database created successfully!"
        echo ""
        echo "Verifying tables..."
        PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "\dt"
    else
        echo "Database creation failed!"
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
        echo "Database created successfully using EF Core!"
    else
        echo "Database creation failed!"
        echo ""
        echo "Manual instructions:"
        echo "1. Install PostgreSQL client: brew install postgresql"
        echo "2. Set environment variables: DATABASE_HOST, DATABASE_USER, DATABASE_PASSWORD"
        echo "3. Run: PGPASSWORD=\$DATABASE_PASSWORD psql -h \$DATABASE_HOST -p \$DATABASE_PORT -U \$DATABASE_USER -d \$DATABASE_NAME -f InitialMigration.sql"
        exit 1
    fi
fi
