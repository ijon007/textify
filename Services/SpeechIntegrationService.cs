namespace WinFormTest.Services;

public class SpeechIntegrationService
{
    private readonly SpeechRecognitionService speechService;
    private readonly GlobalHotkeyManager hotkeyManager;
    private readonly TextInjectionService textInjectionService;
    private readonly TranscriptionFormattingService transcriptionFormattingService;
    private readonly TranscriptionCorrectionService? transcriptionCorrectionService;
    private readonly DatabaseService? databaseService;
    private readonly SpeechOverlayForm overlayForm;
    private readonly HomePageService homePageService;
    private readonly StylePageService? stylePageService;
    private readonly string username;
    
    private string? recognizedText;
    private DateTime? recordingStartTime;

    public SpeechIntegrationService(
        SpeechRecognitionService speechService,
        GlobalHotkeyManager hotkeyManager,
        TextInjectionService textInjectionService,
        TranscriptionFormattingService transcriptionFormattingService,
        TranscriptionCorrectionService? transcriptionCorrectionService,
        DatabaseService? databaseService,
        SpeechOverlayForm overlayForm,
        HomePageService homePageService,
        StylePageService? stylePageService,
        string username)
    {
        this.speechService = speechService;
        this.hotkeyManager = hotkeyManager;
        this.textInjectionService = textInjectionService;
        this.transcriptionFormattingService = transcriptionFormattingService;
        this.transcriptionCorrectionService = transcriptionCorrectionService;
        this.databaseService = databaseService;
        this.overlayForm = overlayForm;
        this.homePageService = homePageService;
        this.stylePageService = stylePageService;
        this.username = username;
        
        // Wire up event handlers
        speechService.SpeechRecognized += SpeechService_SpeechRecognized;
        speechService.SpeechPartialResult += SpeechService_SpeechPartialResult;
        hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;
        hotkeyManager.HotkeyReleased += HotkeyManager_HotkeyReleased;
    }

    private void HotkeyManager_HotkeyPressed(object? sender, EventArgs e)
    {
        // Check if model is loaded
        if (!speechService.IsModelLoaded)
        {
            MessageBox.Show("Speech recognition model is still loading. Please wait...", "Loading",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            recognizedText = null;
            recordingStartTime = DateTime.Now;
            
            // Save the foreground window BEFORE showing overlay
            IntPtr activeWindow = WindowsApiHelper.GetForegroundWindow();
            textInjectionService.SaveForegroundWindow(activeWindow);
            
            speechService.StartListening();
            
            overlayForm.SetState(SpeechOverlayForm.OverlayState.Listening);
            overlayForm.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start speech recognition: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void HotkeyManager_HotkeyReleased(object? sender, EventArgs e)
    {
        try
        {
            speechService.StopListening();
            
            // Show processing state
            overlayForm.SetState(SpeechOverlayForm.OverlayState.Recognizing);
            
            // Wait for recognition processing - give it up to 5 seconds
            int waitTime = 0;
            while (string.IsNullOrEmpty(recognizedText) && waitTime < 5000)
            {
                await Task.Delay(200);
                waitTime += 200;
            }
            
            // Calculate duration if we have a start time
            int? duration = null;
            if (recordingStartTime.HasValue)
            {
                duration = (int)(DateTime.Now - recordingStartTime.Value).TotalMilliseconds;
            }
            
            // Process recognized text if available
            if (!string.IsNullOrEmpty(recognizedText))
            {
                // Trim whitespace
                string finalText = recognizedText.Trim();
                
                // Apply corrections (dictionary and snippets) before formatting
                if (transcriptionCorrectionService != null)
                {
                    finalText = transcriptionCorrectionService.CorrectTranscription(finalText, username);
                }
                
                // Apply formatting based on style preference
                string stylePreference = stylePageService?.GetSelectedStylePreference() ?? 
                    databaseService?.GetUserStylePreference(username) ?? "formal";
                
                finalText = transcriptionFormattingService.FormatTranscription(finalText, stylePreference);
                
                // Set clipboard explicitly
                try
                {
                    Clipboard.SetText(finalText);
                }
                catch (Exception clipEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to set clipboard: {clipEx.Message}");
                }
                
                // Inject text into active input field
                textInjectionService.InjectText(finalText);
                
                // Save to database
                if (databaseService != null && !string.IsNullOrWhiteSpace(finalText))
                {
                    databaseService.SaveSpeech(username, finalText, duration);
                }
                
                // Add to history
                homePageService.AddSpeechToHistory(finalText);
            }
            else
            {
                // No text recognized - show message briefly
                overlayForm.SetRecognizedText("No speech detected");
                await Task.Delay(1000);
            }

            // Return to idle state (thin pill) instead of hiding
            overlayForm.SetState(SpeechOverlayForm.OverlayState.Idle);
            recognizedText = null;
            recordingStartTime = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HotkeyManager_HotkeyReleased error: {ex.Message}\n{ex.StackTrace}");
            overlayForm.SetState(SpeechOverlayForm.OverlayState.Idle);
        }
    }

    private void SpeechService_SpeechPartialResult(object? sender, string text)
    {
        // Partial results are for real-time display only - don't accumulate
        // Show current accumulated text + partial result for preview
        if (!string.IsNullOrWhiteSpace(text))
        {
            string displayText = string.IsNullOrEmpty(recognizedText) 
                ? text 
                : recognizedText + " " + text;
            overlayForm.SetRecognizedText(displayText);
        }
    }

    private void SpeechService_SpeechRecognized(object? sender, string text)
    {
        // Accumulate final recognized text (append with space if there's existing text)
        if (string.IsNullOrEmpty(recognizedText))
        {
            recognizedText = text;
        }
        else
        {
            recognizedText = recognizedText + " " + text;
        }
        
        // Ensure UI updates happen on the UI thread
        if (overlayForm.InvokeRequired)
        {
            overlayForm.Invoke(new Action(() =>
            {
                // Ensure overlay is visible
                if (!overlayForm.Visible)
                {
                    overlayForm.Show();
                }
                overlayForm.SetRecognizedText(recognizedText);
            }));
        }
        else
        {
            // Ensure overlay is visible
            if (!overlayForm.Visible)
            {
                overlayForm.Show();
            }
            overlayForm.SetRecognizedText(recognizedText);
        }
    }
}
