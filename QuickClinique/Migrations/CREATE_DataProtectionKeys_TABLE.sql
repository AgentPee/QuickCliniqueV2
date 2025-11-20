-- SQL script to create DataProtectionKeys table for ASP.NET Core Data Protection
-- This table stores encryption keys that persist across application restarts
-- Run this script if migrations don't create the table automatically

CREATE TABLE IF NOT EXISTS `DataProtectionKeys` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `FriendlyName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
    `Xml` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    CONSTRAINT `PRIMARY` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4 COLLATE=utf8mb4_general_ci;

