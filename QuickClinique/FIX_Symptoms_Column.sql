-- Quick fix script to add Symptoms column to appointments table
-- Run this in your MySQL database if the column is missing

-- Check if column exists first (case-insensitive, safe)
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'appointments' 
    AND UPPER(COLUMN_NAME) = 'SYMPTOMS'
);

-- Add column if it doesn't exist
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE `appointments` ADD COLUMN `Symptoms` longtext CHARACTER SET utf8mb4 NOT NULL DEFAULT ''''',
    'SELECT "Symptoms column already exists" AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verify the column was added
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    COLUMN_DEFAULT,
    IS_NULLABLE,
    CHARACTER_SET_NAME
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
AND TABLE_NAME = 'appointments' 
AND UPPER(COLUMN_NAME) = 'SYMPTOMS';

