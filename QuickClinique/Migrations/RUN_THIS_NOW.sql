-- ============================================
-- RUN THIS SQL IN MYSQL/MARIADB IMMEDIATELY TO FIX THE ERROR
-- ============================================
-- Copy and paste this entire script into MySQL Workbench, phpMyAdmin, or MariaDB
-- and execute it. This will add the TriageNotes column.

USE QuickClinique;

-- Add TriageNotes column to appointments table (simplified for MariaDB compatibility)
ALTER TABLE `appointments` 
ADD COLUMN `TriageNotes` longtext NOT NULL DEFAULT '';

-- Verify the column was added (optional - just to confirm)
DESCRIBE appointments;
