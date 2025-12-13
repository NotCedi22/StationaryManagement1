-- Diagnostic script to check if columns exist
-- Run this to verify your database has the required columns

USE StationeryDB;
GO

-- Check EmployeeNumber column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = 'EmployeeNumber')
BEGIN
    PRINT '✓ EmployeeNumber column EXISTS in Employees table';
END
ELSE
BEGIN
    PRINT '✗ EmployeeNumber column DOES NOT EXIST in Employees table';
END
GO

-- Check ReportsToRoleId column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND name = 'ReportsToRoleId')
BEGIN
    PRINT '✓ ReportsToRoleId column EXISTS in Roles table';
END
ELSE
BEGIN
    PRINT '✗ ReportsToRoleId column DOES NOT EXIST in Roles table';
END
GO

-- Check index
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Roles_ReportsToRoleId' AND object_id = OBJECT_ID(N'[dbo].[Roles]'))
BEGIN
    PRINT '✓ IX_Roles_ReportsToRoleId index EXISTS';
END
ELSE
BEGIN
    PRINT '✗ IX_Roles_ReportsToRoleId index DOES NOT EXIST';
END
GO

-- Check foreign key
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Roles_Roles_ReportsToRoleId')
BEGIN
    PRINT '✓ FK_Roles_Roles_ReportsToRoleId foreign key EXISTS';
END
ELSE
BEGIN
    PRINT '✗ FK_Roles_Roles_ReportsToRoleId foreign key DOES NOT EXIST';
END
GO

-- Show current database name
PRINT '';
PRINT 'Current Database: ' + DB_NAME();
PRINT 'Current Server: ' + @@SERVERNAME;
GO

