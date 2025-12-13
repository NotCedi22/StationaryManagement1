# Troubleshooting Database Column Issues

## Problem: "Invalid column name 'ReportsToRoleId'" Error

This error occurs when the database schema doesn't match the Entity Framework models.

## Quick Fix Steps

### Step 1: Verify Your Connection String

Check `appsettings.json` to see which database you're connecting to:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=StationeryDB;..."
  }
}
```

**Important**: Make sure you run the SQL script on the SAME database that your connection string points to!

### Step 2: Run the Diagnostic Script

1. Open SQL Server Management Studio (SSMS)
2. Connect to the server specified in your connection string
3. Run `Database (File)/CheckDatabaseColumns.sql`
4. This will tell you which columns are missing

### Step 3: Apply the Fix

#### Option A: Let the App Fix It Automatically (Recommended)
1. **Stop the application** if it's running
2. **Restart the application**
3. The app will now automatically add missing columns on startup (moved to run before any database queries)

#### Option B: Run the SQL Script Manually
1. Open SSMS
2. Connect to the **SAME server** as your connection string
3. Make sure you're in the **StationeryDB** database
4. Run `Database (File)/AddMissingColumns.sql`
5. Verify with `Database (File)/CheckDatabaseColumns.sql`

### Step 4: Verify the Fix

Run the diagnostic script again to confirm all columns exist:
```sql
-- Should show all ✓ (checkmarks)
```

## Common Issues

### Issue 1: Script Ran on Wrong Database
**Symptom**: Script says "completed successfully" but error persists

**Solution**: 
- Check your connection string in `appsettings.json`
- Make sure you ran the script on the database specified in the connection string
- The connection string shows: `Database=StationeryDB` - make sure you ran the script on that database

### Issue 2: Multiple SQL Server Instances
**Symptom**: You have SQL Server Express AND LocalDB installed

**Solution**:
- If your connection string says `(localdb)\mssqllocaldb`, run the script on LocalDB
- If your connection string says `.\SQLEXPRESS`, run the script on SQL Server Express
- If your connection string says `DJRIZC\SQLEXPRESS`, run the script on that specific server

### Issue 3: Database Doesn't Exist
**Symptom**: Error about database not found

**Solution**:
1. Create the database first:
   ```sql
   CREATE DATABASE StationeryDB;
   ```
2. Then run the migration script or restore from backup

### Issue 4: Connection String Mismatch
**Symptom**: App connects to one database, script ran on another

**Solution**:
1. Check `appsettings.json` for the connection string
2. Update it to match where you ran the script, OR
3. Run the script on the database specified in the connection string

## Connection String Examples

### LocalDB (Default)
```
Server=(localdb)\mssqllocaldb;Database=StationeryDB;Trusted_Connection=True;TrustServerCertificate=True
```
**To run script**: Connect to `(localdb)\mssqllocaldb` in SSMS

### SQL Server Express
```
Server=.\SQLEXPRESS;Database=StationeryDB;Trusted_Connection=True;TrustServerCertificate=True
```
**To run script**: Connect to `.\SQLEXPRESS` in SSMS

### Named SQL Server Instance
```
Server=DJRIZC\SQLEXPRESS;Database=StationeryDB;Trusted_Connection=True;TrustServerCertificate=True
```
**To run script**: Connect to `DJRIZC\SQLEXPRESS` in SSMS

## Automatic Fix (New in Latest Version)

The application now automatically adds missing columns on startup **before** any database queries. This means:

1. ✅ The migration runs **first** (before middleware)
2. ✅ If migration fails, it tries direct SQL
3. ✅ If that fails, it shows a clear error message
4. ✅ App won't start with invalid database schema

**To use automatic fix**:
1. Make sure you have the latest `Program.cs` (migration code moved to run early)
2. Restart the application
3. Check the console/logs for migration messages

## Still Having Issues?

1. **Check the logs**: Look for migration messages in the console output
2. **Verify database**: Run `CheckDatabaseColumns.sql` to see what's missing
3. **Check connection**: Make sure SQL Server is running and accessible
4. **Verify permissions**: Make sure your user has ALTER TABLE permissions

## Manual Verification Query

Run this in SSMS to check your database:

```sql
USE StationeryDB;
GO

-- Check Employees table
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Employees' AND COLUMN_NAME = 'EmployeeNumber';

-- Check Roles table
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Roles' AND COLUMN_NAME = 'ReportsToRoleId';
```

If these return no rows, the columns don't exist and you need to run `AddMissingColumns.sql`.

