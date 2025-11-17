-- Create emergencies table if it doesn't exist
-- This script can be run manually if the migration doesn't create the table

CREATE TABLE IF NOT EXISTS `emergencies` (
    `EmergencyID` int(100) NOT NULL AUTO_INCREMENT,
    `Location` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `Needs` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `CreatedAt` timestamp(6) NOT NULL DEFAULT current_timestamp(6),
    CONSTRAINT `PRIMARY` PRIMARY KEY (`EmergencyID`)
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Verify the table was created
SELECT 
    TABLE_NAME,
    TABLE_TYPE,
    ENGINE,
    TABLE_COLLATION
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = DATABASE() 
AND TABLE_NAME = 'emergencies';

-- Show table structure
DESCRIBE `emergencies`;

