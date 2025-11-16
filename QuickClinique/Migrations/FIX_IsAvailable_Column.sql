-- Fix IsAvailable column type from TEXT to VARCHAR(10)
-- This script fixes the isAvailable column in the schedules table

-- If the table doesn't exist, this will be handled by migrations
-- If the table exists with TEXT type, run this to fix it

-- Check if column exists and alter it
ALTER TABLE `schedules` 
MODIFY COLUMN `isAvailable` VARCHAR(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'Yes';

-- Verify the change
-- SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH 
-- FROM INFORMATION_SCHEMA.COLUMNS 
-- WHERE TABLE_SCHEMA = DATABASE() 
-- AND TABLE_NAME = 'schedules' 
-- AND COLUMN_NAME = 'isAvailable';

