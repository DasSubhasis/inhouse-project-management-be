-- =============================================
-- License Management System - Database Table
-- =============================================
-- NOTE: This system uses inline queries in the controller.
-- No stored procedures are required.
-- =============================================

-- =============================================
-- 1. Create License Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_License]
    (
        [license_id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [project_id] UNIQUEIDENTIFIER NULL,
        [project_name] NVARCHAR(255) NULL,
        [tally_serial] NVARCHAR(100) NULL,
        [license_no] NVARCHAR(100) NULL,
        [initiation_date] DATE NULL,
        [expiry_date] DATE NULL,
        [created_date] DATETIME DEFAULT GETDATE(),
        [created_by] UNIQUEIDENTIFIER NULL,
        [modified_date] DATETIME NULL,
        [modified_by] UNIQUEIDENTIFIER NULL,
        [delete_date] DATETIME NULL,
        [deleted_by] UNIQUEIDENTIFIER NULL
    );

    PRINT 'Table tbl_License created successfully';
END
ELSE
BEGIN
    PRINT 'Table tbl_License already exists';
END
GO

-- =============================================
-- 2. Create Index for Performance
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_License_TallySerial_ProjectId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_License_TallySerial_ProjectId
    ON [dbo].[tbl_License] ([tally_serial], [project_id]);
    
    PRINT 'Index IX_tbl_License_TallySerial_ProjectId created successfully';
END
GO

-- =============================================
-- Script Execution Summary
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'License Management System Setup Complete';
PRINT '========================================';
PRINT 'Objects Created:';
PRINT '- Table: tbl_License';
PRINT '- Index: IX_tbl_License_TallySerial_ProjectId';
PRINT '';
PRINT 'All CRUD operations use inline SQL queries';
PRINT 'in the LicenseController.';
PRINT '========================================';
GO
