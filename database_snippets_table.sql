-- Create Snippets table for storing user shortcuts and their replacements
-- This table allows users to create shortcut words that expand to longer text
-- Example: 'Twitter' shortcut expands to 'https://twitter.com/username'

CREATE TABLE Snippets (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL,
    Shortcut NVARCHAR(100) NOT NULL,
    Replacement NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);

-- Add index on Username for faster lookups
CREATE INDEX IX_Snippets_Username ON Snippets(Username);

-- Add unique constraint to prevent duplicate shortcuts per user
CREATE UNIQUE INDEX IX_Snippets_Username_Shortcut ON Snippets(Username, Shortcut);
