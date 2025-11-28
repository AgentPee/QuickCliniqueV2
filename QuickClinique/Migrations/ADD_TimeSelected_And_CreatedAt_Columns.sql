-- ============================================
-- ADD TimeSelected and CreatedAt COLUMNS TO APPOINTMENTS TABLE
-- ============================================
-- Run this SQL script in MySQL/MariaDB to add the new columns
-- This can be run manually if migrations haven't been applied yet

USE QuickClinique;

-- Add TimeSelected column if it doesn't exist
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'appointments' 
    AND UPPER(COLUMN_NAME) = 'TIMESELECTED'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE `appointments` ADD COLUMN `TimeSelected` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP',
    'SELECT ''TimeSelected column already exists'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add CreatedAt column if it doesn't exist
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'appointments' 
    AND UPPER(COLUMN_NAME) = 'CREATEDAT'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE `appointments` ADD COLUMN `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP',
    'SELECT ''CreatedAt column already exists'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Update existing appointments to have default values
-- Set TimeSelected based on Schedule date and start time
UPDATE `appointments` a
INNER JOIN `schedules` s ON a.ScheduleID = s.ScheduleID
SET a.TimeSelected = CONCAT(s.Date, ' ', s.StartTime),
    a.CreatedAt = NOW()
WHERE a.TimeSelected = '0000-00-00 00:00:00' OR a.TimeSelected IS NULL
   OR a.CreatedAt = '0000-00-00 00:00:00' OR a.CreatedAt IS NULL;

-- Verify the columns were added
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
AND TABLE_NAME = 'appointments' 
AND COLUMN_NAME IN ('TimeSelected', 'CreatedAt');

