# WinFormTest - Speech Recognition Application

A Windows Forms application that provides speech-to-text functionality with global hotkey support. Speak into your microphone and have the recognized text automatically injected into any active application.

## Features

- **Speech Recognition**: Convert speech to text using Whisper.net offline speech recognition
- **Global Hotkey**: Press `Ctrl + Win` to start/stop speech recognition from anywhere
- **Text Injection**: Automatically injects recognized text into the active window
- **Text Formatting**: Automatic filler word removal and style-based formatting (formal, casual, very casual)
- **Speech History**: View and copy your previous speech transcriptions
- **User Authentication**: Secure login system with SQL Server database
- **Modern UI**: Clean, draggable interface with visual feedback

## Requirements

- **Windows 10/11** (Windows Forms application)
- **.NET 10.0 SDK** or later
- **SQL Server Express** (or any SQL Server instance)
- **Microphone** for speech input
- **Whisper Language Model** (GGML base model ~150MB, downloaded automatically on first run)

## Setup

### 1. Install .NET SDK

Download and install the .NET SDK from [Microsoft's website](https://dotnet.microsoft.com/download).

### 2. Setup SQL Server Database

1. Install SQL Server Express (or use an existing SQL Server instance)
2. Create a database named `WinFormTest`
3. Run the following SQL scripts to create the required tables:

```sql
-- Create Users table
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    UserPass NVARCHAR(100) NOT NULL
);

-- Create Speeches table
CREATE TABLE Speeches (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL,
    SpeechText NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    Duration INT NULL
);

-- Create Dictionary table
CREATE TABLE Dictionary (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL,
    Word NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);

-- Create Snippets table
CREATE TABLE Snippets (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL,
    Shortcut NVARCHAR(100) NOT NULL,
    Replacement NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);

-- Create UserSettings table
CREATE TABLE UserSettings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL,
    StylePreference NVARCHAR(20) NOT NULL DEFAULT 'formal',
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);

-- Add indexes for better performance
CREATE INDEX IX_Snippets_Username ON Snippets(Username);
CREATE UNIQUE INDEX IX_Snippets_Username_Shortcut ON Snippets(Username, Shortcut);
CREATE UNIQUE INDEX IX_UserSettings_Username ON UserSettings(Username);
```

4. (Optional) Create a test user:
```sql
INSERT INTO Users (Username, UserPass) VALUES ('testuser', 'testpass');
```

### 3. Whisper Language Model

The Whisper model will be automatically downloaded on first run (~150MB for base model). The model will be saved to:
   ```
   WinFormTest/
   └── models/
       └── ggml-base.bin
   ```

**Manual Download** (optional): If you prefer to download manually or use a different model:
1. Download a GGML model from [Hugging Face](https://huggingface.co/ggerganov/whisper.cpp)
2. Place the `.bin` file (e.g., `ggml-base.bin`) in the `models` folder
3. Supported models: `ggml-tiny.bin`, `ggml-base.bin`, `ggml-small.bin`, `ggml-medium.bin`, `ggml-large.bin`
4. Update the model filename in `SpeechRecognitionService.cs` if using a different model

**Note**: Larger models provide better accuracy but require more processing time and memory.

### 4. Configure Database Connection

If your SQL Server instance is not `localhost\SQLEXPRESS`, update the connection string in:
- `DatabaseService.cs` (line 11)
- `Form1.cs` (line 8)

Change the connection string to match your SQL Server instance:
```csharp
connectionString = @"Data Source=YOUR_SERVER\INSTANCE;Initial Catalog=WinFormTest;Integrated Security=True;TrustServerCertificate=True;";
```

## Building and Running

### Build the Application

```bash
dotnet build
```

### Run the Application

```bash
dotnet run
```

Or run the executable directly:
```bash
.\bin\Debug\net10.0-windows\WinFormTest.exe
```

## Usage

1. **Launch the application** - The login form will appear
2. **Login** - Enter your username and password
3. **Dashboard** - After login, the dashboard will open showing your speech history
4. **Start Speech Recognition** - Press `Ctrl + Win` (hold both keys)
5. **Speak** - While holding the hotkey, speak into your microphone
6. **Release Hotkey** - Release `Ctrl + Win` to stop listening and inject the text
7. **View History** - Your speech transcriptions are saved and displayed in the dashboard
8. **Copy Text** - Click the 📋 button next to any speech entry to copy it to clipboard

## Project Structure

```
WinFormTest/
├── Form1.cs                    # Login form
├── DashboardForm.cs            # Main dashboard with speech history
├── SpeechOverlayForm.cs        # Visual overlay during speech recognition
├── SpeechRecognitionService.cs # Handles Whisper.net speech recognition
├── TranscriptionFormattingService.cs # Formats transcriptions (filler removal, punctuation, capitalization)
├── TextInjectionService.cs     # Injects text into active windows
├── GlobalHotkeyManager.cs      # Manages global hotkey registration
├── DatabaseService.cs          # Database operations
├── WindowsApiHelper.cs         # Windows API interop
└── assets/
    └── cp-black.ico           # Application icon
```

## Dependencies

- `Microsoft.Data.SqlClient` (v5.2.2) - SQL Server connectivity
- `Whisper.net` (v1.9.0) - Offline speech recognition using OpenAI Whisper
- `Whisper.net.Runtime` (v1.9.0) - Whisper.net runtime libraries
- `NAudio` (v2.2.1) - Audio capture for microphone input

## Troubleshooting

### Speech Recognition Not Working
- Ensure your microphone is connected and working
- Verify the Whisper model (`ggml-base.bin`) is downloaded and placed in the `models` folder
- Check that you have an internet connection for the first run (model auto-download)
- Grant microphone permissions to the application
- Ensure NAudio can access your default audio input device
- For Windows: Ensure Microsoft Visual C++ Redistributable 2022 (x64) is installed (required by Whisper.net.Runtime)

### Database Connection Errors
- Verify SQL Server is running
- Check that the database `WinFormTest` exists
- Ensure Windows Authentication is enabled or update connection string
- Verify the connection string matches your SQL Server instance

### Hotkey Not Responding
- Make sure the application window is not minimized
- Try restarting the application
- Check if another application is using the same hotkey combination

## License

This project is provided as-is for educational and personal use.
