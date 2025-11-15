-- Add TriageNotes column to appointments table
-- Run this SQL script directly in your MySQL database if the automatic migration fails

USE QuickClinique;

-- Check if column exists before adding
SET @exist := (SELECT COUNT(*) 
               FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_SCHEMA = DATABASE() 
               AND TABLE_NAME = 'appointments' 
               AND COLUMN_NAME = 'TriageNotes');

SET @sqlstmt := IF(@exist = 0, 
    'ALTER TABLE `appointments` ADD COLUMN `TriageNotes` longtext NOT NULL DEFAULT '''' CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci',
    'SELECT ''Column TriageNotes already exists'' AS Result');

PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

