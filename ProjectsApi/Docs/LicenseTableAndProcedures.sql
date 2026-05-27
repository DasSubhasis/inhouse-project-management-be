-- ============================================================
-- WhatsApp License Management — Database Migration Script
-- Safe to run: all statements are idempotent (IF NOT EXISTS)
-- ============================================================

USE [DB_Projects]
GO

-- ============================================================
-- STEP 1: Create tbl_License (skip if already exists)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_License]
    (
        [license_id]      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [project_id]      UNIQUEIDENTIFIER NULL,
        [project_name]    NVARCHAR(255) NULL,
        [tally_serial]    NVARCHAR(100) NULL,
        [license_no]      NVARCHAR(100) NULL,
        [initiation_date] DATE NULL,
        [expiry_date]     DATE NULL,
        [created_date]    DATETIME DEFAULT GETDATE(),
        [created_by]      UNIQUEIDENTIFIER NULL,
        [modified_date]   DATETIME NULL,
        [modified_by]     UNIQUEIDENTIFIER NULL,
        [delete_date]     DATETIME NULL,
        [deleted_by]      UNIQUEIDENTIFIER NULL
    );
    PRINT 'tbl_License created';
END
ELSE
    PRINT 'tbl_License already exists — skipped';
GO

PRINT 'STEP 1 complete: tbl_License checked/created';
GO

-- ============================================================
-- STEP 2: Add 13 extended columns to tbl_License
--         (skip each if already present)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'customer_name')
    ALTER TABLE [dbo].[tbl_License] ADD [customer_name] NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'agent_name')
    ALTER TABLE [dbo].[tbl_License] ADD [agent_name] NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'contact_name')
    ALTER TABLE [dbo].[tbl_License] ADD [contact_name] NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'contact_number')
    ALTER TABLE [dbo].[tbl_License] ADD [contact_number] NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'contact_email')
    ALTER TABLE [dbo].[tbl_License] ADD [contact_email] NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'sales_price')
    ALTER TABLE [dbo].[tbl_License] ADD [sales_price] DECIMAL(18,2) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'voucher_number')
    ALTER TABLE [dbo].[tbl_License] ADD [voucher_number] NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'voucher_date')
    ALTER TABLE [dbo].[tbl_License] ADD [voucher_date] DATE NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'remarks')
    ALTER TABLE [dbo].[tbl_License] ADD [remarks] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'interakt_id')
    ALTER TABLE [dbo].[tbl_License] ADD [interakt_id] NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'interakt_password')
    ALTER TABLE [dbo].[tbl_License] ADD [interakt_password] NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'tally_net_id')
    ALTER TABLE [dbo].[tbl_License] ADD [tally_net_id] NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_License]') AND name = 'tally_net_password')
    ALTER TABLE [dbo].[tbl_License] ADD [tally_net_password] NVARCHAR(255) NULL;
GO

PRINT 'STEP 2 complete: extended columns checked/added';
GO

-- ============================================================
-- STEP 3: Fix date column types
--         initiation_date and expiry_date must be DATE not DATETIME
--         (existing data is safe — time portion 00:00:00 is dropped)
-- ============================================================
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'tbl_License' AND COLUMN_NAME = 'initiation_date' AND DATA_TYPE = 'datetime'
)
BEGIN
    ALTER TABLE [dbo].[tbl_License] ALTER COLUMN [initiation_date] DATE NULL;
    PRINT 'initiation_date: converted DATETIME -> DATE';
END
ELSE
    PRINT 'initiation_date: already DATE or not found — skipped';
GO

IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'tbl_License' AND COLUMN_NAME = 'expiry_date' AND DATA_TYPE = 'datetime'
)
BEGIN
    ALTER TABLE [dbo].[tbl_License] ALTER COLUMN [expiry_date] DATE NULL;
    PRINT 'expiry_date: converted DATETIME -> DATE';
END
ELSE
    PRINT 'expiry_date: already DATE or not found — skipped';
GO

PRINT 'STEP 3 complete: date types checked/fixed';
GO

-- ============================================================
-- STEP 4: Create tbl_LicenseCommission (skip if already exists)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_LicenseCommission]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[tbl_LicenseCommission]
    (
        [commission_id]   UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [license_id]      UNIQUEIDENTIFIER NOT NULL,
        [commission_date] DATE NULL,
        [amount]          DECIMAL(18,2) NULL,
        [created_date]    DATETIME DEFAULT GETDATE(),
        [created_by]      UNIQUEIDENTIFIER NULL
    );
    PRINT 'tbl_LicenseCommission created';
END
ELSE
    PRINT 'tbl_LicenseCommission already exists — skipped';
GO

PRINT 'STEP 4 complete: commission table checked/created';
GO

-- ============================================================
-- STEP 5: Performance index on tbl_License (skip if exists)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_License_TallySerial_ProjectId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_tbl_License_TallySerial_ProjectId
    ON [dbo].[tbl_License] ([tally_serial], [project_id]);
    PRINT 'Index IX_tbl_License_TallySerial_ProjectId created';
END
ELSE
    PRINT 'Index IX_tbl_License_TallySerial_ProjectId already exists — skipped';
GO

PRINT 'STEP 5 complete: index checked/created';
GO

-- ============================================================
-- VERIFICATION — confirms final state of both tables
-- ============================================================
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('tbl_License', 'tbl_LicenseCommission')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
GO

PRINT '';
PRINT '========================================';
PRINT 'Migration complete. Review results above.';
PRINT '========================================';
