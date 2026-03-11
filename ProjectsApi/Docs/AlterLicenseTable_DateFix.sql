-- =============================================
-- Alter License Table - Fix Date Column Types
-- =============================================
-- This script changes initiation_date and expiry_date
-- from DATETIME to DATE to match the API requirements
-- =============================================

USE [DB_Projects]
GO

-- Check current column types
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tbl_License'
AND COLUMN_NAME IN ('initiation_date', 'expiry_date');
GO

-- Alter initiation_date to DATE if it's DATETIME
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'tbl_License' 
    AND COLUMN_NAME = 'initiation_date' 
    AND DATA_TYPE = 'datetime'
)
BEGIN
    ALTER TABLE [dbo].[tbl_License]
    ALTER COLUMN [initiation_date] DATE NULL;
    
    PRINT 'Column initiation_date changed from DATETIME to DATE';
END
ELSE
BEGIN
    PRINT 'Column initiation_date is already DATE type or does not exist';
END
GO

-- Alter expiry_date to DATE if it's DATETIME
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'tbl_License' 
    AND COLUMN_NAME = 'expiry_date' 
    AND DATA_TYPE = 'datetime'
)
BEGIN
    ALTER TABLE [dbo].[tbl_License]
    ALTER COLUMN [expiry_date] DATE NULL;
    
    PRINT 'Column expiry_date changed from DATETIME to DATE';
END
ELSE
BEGIN
    PRINT 'Column expiry_date is already DATE type or does not exist';
END
GO

-- Verify the changes
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tbl_License'
AND COLUMN_NAME IN ('initiation_date', 'expiry_date');
GO

PRINT '';
PRINT '========================================';
PRINT 'Date column types updated successfully!';
PRINT '========================================';
GO
