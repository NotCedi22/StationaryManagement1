-- SQL Script to add missing columns to the database
-- Run this script if migrations don't work automatically

USE StationeryDB;
GO

-- Add EmployeeNumber column to Employees table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND name = 'EmployeeNumber')
BEGIN
    ALTER TABLE [dbo].[Employees]
    ADD [EmployeeNumber] nvarchar(max) NULL;
    PRINT 'Added EmployeeNumber column to Employees table';
END
ELSE
BEGIN
    PRINT 'EmployeeNumber column already exists in Employees table';
END
GO

-- Add ReportsToRoleId column to Roles table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND name = 'ReportsToRoleId')
BEGIN
    ALTER TABLE [dbo].[Roles]
    ADD [ReportsToRoleId] int NULL;
    PRINT 'Added ReportsToRoleId column to Roles table';
    
    -- Create index for ReportsToRoleId
    CREATE INDEX [IX_Roles_ReportsToRoleId] ON [dbo].[Roles] ([ReportsToRoleId]);
    PRINT 'Created index IX_Roles_ReportsToRoleId';
    
    -- Add foreign key constraint
    ALTER TABLE [dbo].[Roles]
    ADD CONSTRAINT [FK_Roles_Roles_ReportsToRoleId] 
    FOREIGN KEY ([ReportsToRoleId]) 
    REFERENCES [dbo].[Roles] ([RoleId]) 
    ON DELETE NO ACTION;
    PRINT 'Created foreign key constraint FK_Roles_Roles_ReportsToRoleId';
END
ELSE
BEGIN
    PRINT 'ReportsToRoleId column already exists in Roles table';
END
GO

PRINT 'Script completed successfully!';
GO

