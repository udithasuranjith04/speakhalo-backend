#!/bin/bash
# SpeakHalo Database Deployment Script
# This script applies the database migrations to the production PostgreSQL database

set -e  # Exit on error

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Database connection details
DB_HOST="your-db-host.example.com"
DB_PORT="25060"
DB_NAME="speakhalo"
DB_USER="doadmin"
DB_PASSWORD="${DB_PASSWORD:?Set DB_PASSWORD before running}"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  SpeakHalo Database Deployment${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Step 1: Test connection
echo -e "${YELLOW}Step 1: Testing database connection...${NC}"
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT version();" > /dev/null 2>&1

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Database connection successful${NC}"
else
    echo -e "${RED}✗ Database connection failed${NC}"
    echo -e "${RED}Please check your connection details and try again.${NC}"
    exit 1
fi
echo ""

# Step 2: Check if database has existing tables
echo -e "${YELLOW}Step 2: Checking existing database schema...${NC}"
TABLE_COUNT=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';" | tr -d ' ')

echo -e "Found ${YELLOW}${TABLE_COUNT}${NC} existing tables"
echo ""

# Step 3: Backup database (if tables exist)
if [ "$TABLE_COUNT" -gt 0 ]; then
    echo -e "${YELLOW}Step 3: Creating database backup...${NC}"
    BACKUP_FILE="speakhalo_backup_$(date +%Y%m%d_%H%M%S).sql"

    PGPASSWORD=$DB_PASSWORD pg_dump -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -F p -f $BACKUP_FILE

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Backup created: ${BACKUP_FILE}${NC}"
    else
        echo -e "${RED}✗ Backup failed${NC}"
        exit 1
    fi
else
    echo -e "${YELLOW}Step 3: No existing tables, skipping backup...${NC}"
fi
echo ""

# Step 4: Apply migrations using SQL script
echo -e "${YELLOW}Step 4: Applying database migrations...${NC}"
if [ -f "InitialMigration.sql" ]; then
    PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f InitialMigration.sql

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Migrations applied successfully${NC}"
    else
        echo -e "${RED}✗ Migration failed${NC}"
        exit 1
    fi
else
    echo -e "${RED}✗ InitialMigration.sql not found${NC}"
    echo -e "${YELLOW}Please generate the migration script first:${NC}"
    echo -e "${YELLOW}dotnet ef migrations script --project SpeakHalo.Infrastructure --startup-project SpeakHalo.API --output InitialMigration.sql${NC}"
    exit 1
fi
echo ""

# Step 5: Verify database schema
echo -e "${YELLOW}Step 5: Verifying database schema...${NC}"
TABLE_COUNT_AFTER=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';" | tr -d ' ')

echo -e "Total tables: ${GREEN}${TABLE_COUNT_AFTER}${NC}"

# Check critical tables
CRITICAL_TABLES=("Users" "TrialRequests" "Calls" "Customers" "Appointments" "PhoneNumbers")

for table in "${CRITICAL_TABLES[@]}"; do
    EXISTS=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '${table}');" | tr -d ' ')

    if [ "$EXISTS" = "t" ]; then
        echo -e "  ${GREEN}✓${NC} ${table}"
    else
        echo -e "  ${RED}✗${NC} ${table}"
    fi
done
echo ""

# Step 6: Run database seeder (optional)
echo -e "${YELLOW}Step 6: Would you like to seed initial data? (y/n)${NC}"
read -r SEED_RESPONSE

if [ "$SEED_RESPONSE" = "y" ]; then
    echo -e "${YELLOW}Running database seeder...${NC}"
    cd SpeakHalo.API
    dotnet run --seed
    cd ..
    echo -e "${GREEN}✓ Database seeded successfully${NC}"
else
    echo -e "${YELLOW}Skipping database seeding${NC}"
fi
echo ""

# Summary
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "Database: ${GREEN}${DB_NAME}${NC}"
echo -e "Host: ${GREEN}${DB_HOST}${NC}"
echo -e "Tables: ${GREEN}${TABLE_COUNT_AFTER}${NC}"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo -e "1. Update production environment variables"
echo -e "2. Deploy API application"
echo -e "3. Run health check: curl https://api.speakhalo.com/health"
echo -e "4. Monitor logs for any errors"
echo ""
echo -e "${GREEN}Database is ready for production use!${NC}"
