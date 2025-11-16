-- Quick fix script to add IsActive column to clinicstaff table
-- Run this in your MySQL database if the column is missing

-- Check if column exists first (optional, but safe)
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'clinicstaff' 
    AND COLUMN_NAME = 'IsActive'
);

-- Add column if it doesn't exist
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE `clinicstaff` ADD COLUMN `IsActive` tinyint(1) NOT NULL DEFAULT 1',
    'SELECT "IsActive column already exists" AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verify the column was added
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    COLUMN_DEFAULT,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE()
AND TABLE_NAME = 'clinicstaff'
AND COLUMN_NAME = 'IsActive';

