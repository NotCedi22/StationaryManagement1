# Stationery Management System - Setup Guide

## Quick Start

### 1. Database Setup

#### Option A: Using SQL Server Express/LocalDB (Recommended for Development)
1. Install SQL Server Express or LocalDB
2. Update `appsettings.json` with your connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=StationeryDB;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```

#### Option B: Using Environment Variables (Recommended for Production)
Set the environment variable:
```bash
# Windows PowerShell
$env:StationeryDB_ConnectionString = "Server=YOUR_SERVER;Database=StationeryDB;User Id=USER;Password=PASSWORD;TrustServerCertificate=True"

# Windows CMD
set StationeryDB_ConnectionString=Server=YOUR_SERVER;Database=StationeryDB;User Id=USER;Password=PASSWORD;TrustServerCertificate=True

# Linux/Mac
export StationeryDB_ConnectionString="Server=YOUR_SERVER;Database=StationeryDB;User Id=USER;Password=PASSWORD;TrustServerCertificate=True"
```

#### Option C: Restore from Backup
1. Open SQL Server Management Studio (SSMS)
2. Right-click "Databases" > "Restore Database..."
3. Select "Device" and browse to `Database (File)/StationeryDB.bak`
4. Set database name to `StationeryDB`
5. Click "OK" to restore

### 2. Run Migrations

The application will automatically apply migrations on startup. If you encounter issues:

1. **Automatic Migration**: The app will try to apply migrations automatically
2. **Manual SQL Script**: Run `Database (File)/AddMissingColumns.sql` if needed

### 3. Build and Run

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

The application will be available at:
- HTTP: `http://localhost:5289`
- HTTPS: `https://localhost:7055`

### 4. Default Admin Account

On first run, the application creates a default admin account:
- **Email**: `admin@example.com`
- **Password**: `admin123`

**⚠️ IMPORTANT**: Change this password immediately after first login!

## Connection String Examples

### Windows Authentication (Trusted Connection)
```
Server=YOUR_SERVER_NAME;Database=StationeryDB;Trusted_Connection=True;TrustServerCertificate=True
```

### SQL Server Authentication
```
Server=YOUR_SERVER_NAME;Database=StationeryDB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True
```

### LocalDB (Default Development)
```
Server=(localdb)\mssqllocaldb;Database=StationeryDB;Trusted_Connection=True;TrustServerCertificate=True
```

### SQL Server Express
```
Server=.\SQLEXPRESS;Database=StationeryDB;Trusted_Connection=True;TrustServerCertificate=True
```

## Remember Me Feature

The "Remember Me" functionality:
- ✅ Works across all modern browsers (Chrome, Firefox, Safari, Edge)
- ✅ Uses secure cookies with SameSite=Lax for cross-browser compatibility
- ✅ Validates user is still active on each session restore
- ✅ Automatically clears cookies if user account is deactivated
- ✅ 30-day expiration period

## Browser Compatibility

- ✅ Chrome/Edge (Chromium)
- ✅ Firefox
- ✅ Safari
- ✅ Opera

## Troubleshooting

### Database Connection Issues
1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. Ensure database exists or restore from backup
4. Check firewall settings

### Migration Issues
- The app will automatically try to add missing columns
- If automatic migration fails, run `Database (File)/AddMissingColumns.sql` manually

### Remember Me Not Working
- Clear browser cookies and try again
- Ensure cookies are enabled in browser settings
- Check browser console for cookie-related errors

## Portability

This project is designed to be portable across systems:
- ✅ No hardcoded paths
- ✅ Environment variable support for connection strings
- ✅ Automatic database migration
- ✅ Cross-platform compatible (.NET 8.0)

## Security Notes

- Change default admin password immediately
- Use HTTPS in production
- Store connection strings securely (use environment variables or Azure Key Vault)
- Regularly update dependencies

