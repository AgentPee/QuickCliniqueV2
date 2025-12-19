-- ============================================
-- ADD ACKNOWLEDGMENT FIELDS TO EMERGENCIES TABLE
-- ============================================
-- Run this SQL in MySQL/MariaDB to add the new fields for emergency acknowledgment tracking

USE QuickClinique;

-- Add IsAcknowledged field (tracks when clinic staff responds/acknowledges)
ALTER TABLE `emergencies` 
ADD COLUMN `IsAcknowledged` tinyint(1) NOT NULL DEFAULT 0;

-- Add IsHelpReceivedRequested field (tracks when student clicks "Help is Received")
ALTER TABLE `emergencies` 
ADD COLUMN `IsHelpReceivedRequested` tinyint(1) NOT NULL DEFAULT 0;

-- Verify the columns were added (optional - just to confirm)
DESCRIBE emergencies;

