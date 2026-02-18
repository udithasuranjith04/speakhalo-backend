# SpeakHalo Database - Quick Start Guide 🚀

## Deploy Database (One Command)

```bash
cd /Users/udithasuranjith/Documents/SpeakHalo.Net/SpeakHalo
./deploy-database.sh
```

---

## Database Details

| Item | Value |
|------|-------|
| **Host** | your-db-host.example.com |
| **Port** | 25060 |
| **Database** | speakhalo |
| **Username** | doadmin |
| **Password** | YOUR_DB_PASSWORD |
| **SSL** | Required |

---

## Quick Commands

### Connect to Database
```bash
psql "postgresql://doadmin:YOUR_DB_PASSWORD@your-db-host.example.com:25060/speakhalo?sslmode=require"
```

### Apply Migrations (Manual)
```bash
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update --project SpeakHalo.Infrastructure --startup-project SpeakHalo.API
```

### Check Database Health
```bash
curl https://api.speakhalo.com/health
```

### Create Backup
```bash
pg_dump -h your-db-host.example.com \
        -p 25060 -U doadmin -d speakhalo \
        -F c -f backup_$(date +%Y%m%d).dump
```

---

## What Was Created

✅ **35 Tables** - Complete database schema
✅ **80+ Indexes** - Optimized performance
✅ **SSL/TLS** - Secure connections
✅ **Connection Pooling** - Max 100 connections
✅ **TrialRequest Table** - Admin-controlled trials
✅ **Audit Logging** - Complete trail
✅ **Migrations** - Generated and ready

---

## Files Created

1. **appsettings.Production.json** - Production config
2. **InitialMigration.sql** - 874 lines of SQL
3. **deploy-database.sh** - Automated deployment
4. **DATABASE_SETUP.md** - Complete guide
5. **DATABASE_MIGRATION_COMPLETE.md** - Full summary

---

## Next Steps

1. ✅ Run: `./deploy-database.sh`
2. ✅ Verify: All 35 tables created
3. ✅ Deploy: API application
4. ✅ Test: Health endpoint
5. ✅ Backup: Create initial backup

---

## Support

📖 **Full Documentation**: DATABASE_SETUP.md
📊 **Summary**: DATABASE_MIGRATION_COMPLETE.md
🔧 **Script**: deploy-database.sh

---

**Status**: READY TO DEPLOY ✅
