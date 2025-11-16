-- QUICK FIX: Run this SQL directly in MySQL to add TriageNotes column
-- This will immediately fix the "Unknown column 'a.TriageNotes' in 'field list'" error

USE QuickClinique;

-- Check if column exists
SET @exists = (SELECT COUNT(*) 
               FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_SCHEMA = DATABASE() 
               AND TABLE_NAME = 'appointments' 
               AND COLUMN_NAME = 'TriageNotes');

-- Add column if it doesn't exist
SET @sql = IF(@exists = 0,
    'ALTER TABLE `appointments` ADD COLUMN `TriageNotes` longtext NOT NULL DEFAULT '''' CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci',
    'SELECT ''Column TriageNotes already exists'' AS Result');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Verify column was added
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_SET_NAME, COLLATION_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() 
AND TABLE_NAME = 'appointments' 
AND COLUMN_NAME = 'TriageNotes';

