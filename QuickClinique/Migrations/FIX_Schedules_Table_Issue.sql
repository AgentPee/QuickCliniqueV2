-- Fix for: Migration failed: Table 'schedules' already exists
-- This script fixes the existing schedules table and marks migrations as applied

-- Step 1: Alter the isAvailable column type from TEXT to VARCHAR(10) if it's currently TEXT
-- (This will only change it if it's currently TEXT, otherwise it will error which is fine)
ALTER TABLE `schedules` 
MODIFY COLUMN `isAvailable` VARCHAR(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'Yes';

-- Step 2: Check if the migration is already in the history table
-- If not, insert it manually to mark it as applied
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251022121734_InitialCreate', '9.0.9');

-- If you have other migrations that were applied, add them too:
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251022123215_AddSymptomsToAppointment', '9.0.9');

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251022124000_AddIsActiveToStudent', '9.0.9');

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251022130000_AddTriageNotesToAppointment', '9.0.9');

-- Verify the column type
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
AND TABLE_NAME = 'schedules' 
AND COLUMN_NAME = 'isAvailable';

