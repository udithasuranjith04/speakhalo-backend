using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        // Read credentials from environment variables
        var host = Environment.GetEnvironmentVariable("DATABASE_HOST")
            ?? throw new InvalidOperationException("DATABASE_HOST environment variable is not set");
        var port = Environment.GetEnvironmentVariable("DATABASE_PORT")
            ?? throw new InvalidOperationException("DATABASE_PORT environment variable is not set");
        var username = Environment.GetEnvironmentVariable("DATABASE_USER")
            ?? throw new InvalidOperationException("DATABASE_USER environment variable is not set");
        var password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD")
            ?? throw new InvalidOperationException("DATABASE_PASSWORD environment variable is not set");

        // Try connecting to defaultdb first (DigitalOcean default)
        var defaultConnectionString = $"Host={host};Port={port};Database=defaultdb;Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Timeout=60";
        var speakhaloConnectionString = $"Host={host};Port={port};Database=speakhalo;Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Timeout=60";

        Console.WriteLine("===========================================");
        Console.WriteLine("  SpeakHalo Database Creator");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        try
        {
            // Step 1: Connect to defaultdb and create speakhalo database if it doesn't exist
            Console.WriteLine("Step 1: Connecting to default database...");
            await using var defaultConn = new NpgsqlConnection(defaultConnectionString);
            await defaultConn.OpenAsync();
            Console.WriteLine("[SUCCESS] Connected to defaultdb!");
            Console.WriteLine($"PostgreSQL Version: {defaultConn.PostgreSqlVersion}");
            Console.WriteLine();

            // Check if speakhalo database exists
            Console.WriteLine("Step 2: Checking if 'speakhalo' database exists...");
            var checkDbCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = 'speakhalo'", defaultConn);
            var dbExists = await checkDbCmd.ExecuteScalarAsync();

            if (dbExists == null)
            {
                Console.WriteLine("Database 'speakhalo' does not exist. Creating it...");
                var createDbCmd = new NpgsqlCommand("CREATE DATABASE speakhalo", defaultConn);
                await createDbCmd.ExecuteNonQueryAsync();
                Console.WriteLine("[SUCCESS] Database 'speakhalo' created!");
            }
            else
            {
                Console.WriteLine("[SUCCESS] Database 'speakhalo' already exists.");
            }

            await defaultConn.CloseAsync();
            Console.WriteLine();

            // Step 3: Connect to speakhalo database
            Console.WriteLine("Step 3: Connecting to 'speakhalo' database...");
            await using var conn = new NpgsqlConnection(speakhaloConnectionString);
            await conn.OpenAsync();
            Console.WriteLine("[SUCCESS] Connected to speakhalo database!");
            Console.WriteLine();

            // Step 4: Check if tables already exist
            Console.WriteLine("Step 4: Checking existing schema...");
            var checkTablesCmd = new NpgsqlCommand(@"
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_schema = 'public'
                AND table_type = 'BASE TABLE'", conn);

            var existingTables = (long)(await checkTablesCmd.ExecuteScalarAsync() ?? 0L);
            Console.WriteLine($"Found {existingTables} existing tables");

            if (existingTables > 0)
            {
                Console.WriteLine("[WARNING] Database already has tables. Skipping migration.");
                Console.WriteLine("If you want to recreate the database, drop it first.");
                Console.WriteLine();

                // List existing tables
                Console.WriteLine("Existing tables:");
                var listCmd = new NpgsqlCommand(@"
                    SELECT table_name
                    FROM information_schema.tables
                    WHERE table_schema = 'public'
                    AND table_type = 'BASE TABLE'
                    ORDER BY table_name", conn);

                await using var reader = await listCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"  - {reader.GetString(0)}");
                }

                return;
            }

            // Step 5: Read SQL file
            Console.WriteLine("Step 5: Reading migration SQL file...");
            var sqlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "InitialMigration.sql");

            if (!File.Exists(sqlFilePath))
            {
                Console.WriteLine($"[ERROR] SQL file not found at: {sqlFilePath}");
                return;
            }

            var sql = await File.ReadAllTextAsync(sqlFilePath);
            Console.WriteLine($"[SUCCESS] SQL file loaded ({sql.Length} characters)");
            Console.WriteLine();

            // Step 6: Execute SQL
            Console.WriteLine("Step 6: Executing migration SQL (this may take a minute)...");
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 300; // 5 minutes timeout

            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine("[SUCCESS] Migration executed successfully!");
            Console.WriteLine();

            // Step 7: Verify tables
            Console.WriteLine("Step 7: Verifying database schema...");
            var verifyCmd = new NpgsqlCommand(@"
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_schema = 'public'
                AND table_type = 'BASE TABLE'", conn);

            var tableCount = (long)(await verifyCmd.ExecuteScalarAsync() ?? 0L);
            Console.WriteLine($"[SUCCESS] Total tables created: {tableCount}");
            Console.WriteLine();

            // List all tables
            Console.WriteLine("Tables created:");
            var listTablesCmd = new NpgsqlCommand(@"
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public'
                AND table_type = 'BASE TABLE'
                ORDER BY table_name", conn);

            await using (var reader = await listTablesCmd.ExecuteReaderAsync())
            {
                int count = 0;
                while (await reader.ReadAsync())
                {
                    count++;
                    Console.WriteLine($"  {count}. {reader.GetString(0)}");
                }
            }

            await conn.CloseAsync();

            Console.WriteLine();
            Console.WriteLine("===========================================");
            Console.WriteLine("  Database Setup Complete!");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine($"Database: speakhalo");
            Console.WriteLine($"Host: {host}");
            Console.WriteLine($"Tables: {tableCount}");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("1. Deploy the API application");
            Console.WriteLine("2. Test health endpoint: curl https://api.speakhalo.com/health");
            Console.WriteLine("3. Create initial backup");
            Console.WriteLine("4. Optionally seed initial data");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("[ERROR] Error occurred:");
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            if (ex.InnerException != null)
            {
                Console.WriteLine("Inner Exception:");
                Console.WriteLine(ex.InnerException.Message);
                Console.WriteLine();
            }
            Console.WriteLine("Stack Trace:");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
