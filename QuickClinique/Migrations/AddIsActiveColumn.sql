-- SQL script to add IsActive column to students table
-- Run this script in your MySQL database if the migration doesn't apply automatically

ALTER TABLE `students` 
ADD COLUMN `IsActive` tinyint(1) NOT NULL DEFAULT 1;

