-- Migration script to add new settings columns to UserSettings table
-- Run this script in your SQL Server database

USE WinFormTest;
GO

-- Check if columns exist before adding them (safe to run multiple times)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserSettings' AND COLUMN_NAME = 'MicrophoneDeviceId')
BEGIN
    ALTER TABLE UserSettings ADD MicrophoneDeviceId NVARCHAR(100) NULL;
    PRINT 'Added MicrophoneDeviceId column';
END
ELSE
    PRINT 'MicrophoneDeviceId column already exists';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserSettings' AND COLUMN_NAME = 'OverlayPosition')
BEGIN
    ALTER TABLE UserSettings ADD OverlayPosition NVARCHAR(50) DEFAULT 'bottom_center';
    PRINT 'Added OverlayPosition column';
END
ELSE
    PRINT 'OverlayPosition column already exists';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserSettings' AND COLUMN_NAME = 'OverlayOpacity')
BEGIN
    ALTER TABLE UserSettings ADD OverlayOpacity INT DEFAULT 100;
    PRINT 'Added OverlayOpacity column';
END
ELSE
    PRINT 'OverlayOpacity column already exists';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserSettings' AND COLUMN_NAME = 'ShowOverlay')
BEGIN
    ALTER TABLE UserSettings ADD ShowOverlay BIT DEFAULT 1;
    PRINT 'Added ShowOverlay column';
END
ELSE
    PRINT 'ShowOverlay column already exists';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserSettings' AND COLUMN_NAME = 'StartWithWindows')
BEGIN
    ALTER TABLE UserSettings ADD StartWithWindows BIT DEFAULT 0;
    PRINT 'Added StartWithWindows column';
END
ELSE
    PRINT 'StartWithWindows column already exists';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserSettings' AND COLUMN_NAME = 'StartMinimized')
BEGIN
    ALTER TABLE UserSettings ADD StartMinimized BIT DEFAULT 0;
    PRINT 'Added StartMinimized column';
END
ELSE
    PRINT 'StartMinimized column already exists';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserSettings' AND COLUMN_NAME = 'MinimizeToTray')
BEGIN
    ALTER TABLE UserSettings ADD MinimizeToTray BIT DEFAULT 0;
    PRINT 'Added MinimizeToTray column';
END
ELSE
    PRINT 'MinimizeToTray column already exists';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserSettings' AND COLUMN_NAME = 'RecognitionSensitivity')
BEGIN
    ALTER TABLE UserSettings ADD RecognitionSensitivity NVARCHAR(20) DEFAULT 'medium';
    PRINT 'Added RecognitionSensitivity column';
END
ELSE
    PRINT 'RecognitionSensitivity column already exists';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserSettings' AND COLUMN_NAME = 'AutoInjectDelay')
BEGIN
    ALTER TABLE UserSettings ADD AutoInjectDelay INT DEFAULT 0;
    PRINT 'Added AutoInjectDelay column';
END
ELSE
    PRINT 'AutoInjectDelay column already exists';

PRINT 'Migration completed successfully!';
GO
