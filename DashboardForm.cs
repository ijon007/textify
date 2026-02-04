using System.Linq;
using WinFormTest.Services;

namespace WinFormTest;

public partial class DashboardForm : Form
{

  private string username;
  private SpeechRecognitionService? speechService;
  private GlobalHotkeyManager? hotkeyManager;
  private TextInjectionService? textInjectionService;
  private TranscriptionFormattingService? transcriptionFormattingService;
  private TranscriptionCorrectionService? transcriptionCorrectionService;
  private SpeechOverlayForm? overlayForm;
  private DatabaseService? databaseService;
  
  // Services
  private NavigationService? navigationService;
  private WindowControlsService? windowControlsService;
  private LoadingUIService? loadingUIService;
  private SystemTrayService? systemTrayService;
  private HomePageService? homePageService;
  private DictionaryPageService? dictionaryPageService;
  private SnippetsPageService? snippetsPageService;
  private StylePageService? stylePageService;
  private SettingsPageService? settingsPageService;
  private SpeechIntegrationService? speechIntegrationService;
  
  // Note: Page-specific UI components are now managed by their respective page services
  // (DictionaryPageService, SnippetsPageService, StylePageService, SettingsPageService)

  public DashboardForm(string username)
  {
    this.username = username;
    InitializeComponent();
    
    // Prevent focus rectangles on buttons
    btnClose.TabStop = false;
    btnMinimize.TabStop = false;
    
    // Set custom icon from assets folder
    string iconPath = Path.Combine(Application.StartupPath, "assets", "cp-black.ico");
    if (File.Exists(iconPath))
    {
      this.Icon = new Icon(iconPath);
      // Load logo into PictureBox
      try
      {
        using (Icon icon = new Icon(iconPath, 32, 32))
        {
          picLogo.Image = icon.ToBitmap();
        }
      }
      catch
      {
        // If loading fails, try loading as image directly
        try
        {
          picLogo.Image = Image.FromFile(iconPath);
        }
        catch
        {
          // If both methods fail, leave PictureBox empty
        }
      }
    }
    
    databaseService = new DatabaseService();
    
    // Initialize services
    InitializeServices();
    
    // Subscribe to resize events to detect minimize
    this.Resize += DashboardForm_Resize;
  }

  // UI styling methods are now handled by UIStylingService and HomePageService

  private void InitializeServices()
  {
    // Initialize UI helper services
    windowControlsService = new WindowControlsService(this, panelTopRibbon, btnClose, btnMinimize);
    loadingUIService = new LoadingUIService(this);
    loadingUIService.Initialize();
    systemTrayService = new SystemTrayService(this, databaseService, username);
    systemTrayService.Initialize();
    
    // Initialize transcription correction service early (needed by page services)
    transcriptionCorrectionService = new TranscriptionCorrectionService(databaseService);
    
    // Initialize page services
    homePageService = new HomePageService(databaseService, username, panelHomePage, panelSpeechHistory, 
        lblWelcome, lblStatWeeks, lblStatWords, lblStatWPM);
    
    dictionaryPageService = new DictionaryPageService(databaseService, transcriptionCorrectionService, 
        username, this, panelDictionaryPage);
    
    snippetsPageService = new SnippetsPageService(databaseService, transcriptionCorrectionService, 
        username, this, panelSnippetsPage);
    
    stylePageService = new StylePageService(databaseService, username, panelStylePage);
    
    // SettingsPageService will be initialized after speechService and overlayForm are created in InitializeSpeechServicesAsync
    settingsPageService = null;
    
    // Initialize navigation service
    navigationService = new NavigationService(panelHomePage, panelDictionaryPage, panelSnippetsPage, 
        panelStylePage, panelSettingsPage, lblNavHome, lblNavDictionary, lblNavSnippets, 
        lblNavStyle, lblNavSettings);
    
    // Setup rounded corners for pages
    Panel[] pagePanels = { panelHomePage, panelDictionaryPage, panelSnippetsPage, panelStylePage };
    foreach (Panel panel in pagePanels)
    {
      panel.Paint += (s, e) => UIStylingService.DrawRoundedBorder(panel, e.Graphics, UILayout.BorderRadius, UIColors.BorderGray);
      panel.Resize += (s, e) => UIStylingService.ApplyRoundedCorners(panel, UILayout.BorderRadius);
      UIStylingService.ApplyRoundedCorners(panel, UILayout.BorderRadius);
    }
    
    panelSettingsPage.Paint += (s, e) =>
    {
      UIStylingService.DrawRoundedBorder(panelSettingsPage, e.Graphics, UILayout.BorderRadius, UIColors.BorderGray);
    };
    panelSettingsPage.Resize += (s, e) => UIStylingService.ApplyRoundedCorners(panelSettingsPage, UILayout.BorderRadius);
    UIStylingService.ApplyRoundedCorners(panelSettingsPage, UILayout.BorderRadius);
    
    // Initialize pages
    InitializePages();
    
    // Load dashboard data
    homePageService.LoadDashboardData();
    
    // Initialize speech services
    InitializeSpeechServicesAsync();
  }
  

  private async void InitializeSpeechServicesAsync()
  {
    try
    {
      // Initialize speech recognition service (model loading happens async)
      speechService = new SpeechRecognitionService();

      // Initialize text injection service
      textInjectionService = new TextInjectionService();

      // Initialize transcription formatting service
      transcriptionFormattingService = new TranscriptionFormattingService();

      // Note: transcriptionCorrectionService is already initialized in InitializeServices()

      // Initialize hotkey manager
      hotkeyManager = new GlobalHotkeyManager(this.Handle);
      
      // Load and apply saved hotkey preference
      LoadAndApplyHotkeyPreference();
      
      // Load and apply microphone preference
      LoadAndApplyMicrophonePreference();
      
      hotkeyManager.InstallKeyboardHook();

      // Initialize overlay form
      overlayForm = new SpeechOverlayForm();
      
      // Load and apply overlay settings
      if (databaseService != null)
      {
        try
        {
          string position = databaseService.GetUserOverlayPosition(username);
          overlayForm.SetOverlayPosition(position);
        }
        catch (Exception ex)
        {
          // Use defaults if loading fails
          System.Diagnostics.Debug.WriteLine($"Failed to load overlay position: {ex.Message}");
        }
      }
      
      // Show idle state (thin pill)
      overlayForm.SetState(SpeechOverlayForm.OverlayState.Idle);
      overlayForm.Show();
      
      // Initialize speech integration service (handles event wiring)
      speechIntegrationService = new SpeechIntegrationService(
        speechService,
        hotkeyManager,
        textInjectionService,
        transcriptionFormattingService,
        transcriptionCorrectionService,
        databaseService,
        overlayForm,
        homePageService!,
        stylePageService,
        username);
      
      // Update settingsPageService with speechService and overlayForm references
      settingsPageService = new SettingsPageService(databaseService, hotkeyManager, speechService, 
        overlayForm, dictionaryPageService, snippetsPageService, transcriptionCorrectionService, 
        username, this, panelSettingsPage);
      settingsPageService.Initialize();

      // Load model asynchronously
      loadingUIService?.UpdateText("Loading Whisper model...");
      await speechService.InitializeAsync();
      
      // Hide loading panel once model is loaded
      loadingUIService?.Hide();
    }
    catch (Exception ex)
    {
      loadingUIService?.Hide();
      MessageBox.Show($"Failed to initialize speech services: {ex.Message}", "Error", 
        MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
  }

  // UpdateLoadingText is now handled by LoadingUIService

  private void LoadAndApplyHotkeyPreference()
  {
    if (databaseService == null || hotkeyManager == null)
      return;

    try
    {
      var (ctrl, alt, shift, win, keyCode) = databaseService.GetUserHotkeyPreference(username);
      hotkeyManager.SetHotkeyConfiguration(ctrl, alt, shift, win, keyCode);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to load hotkey preference: {ex.Message}");
      // Use default Ctrl+Win if loading fails
      hotkeyManager.SetHotkeyConfiguration(true, false, false, true, null);
    }
  }

  private void LoadAndApplyMicrophonePreference()
  {
    if (databaseService == null || speechService == null)
      return;

    try
    {
      string? deviceId = databaseService.GetUserMicrophonePreference(username);
      if (!string.IsNullOrEmpty(deviceId) && int.TryParse(deviceId, out int deviceNumber))
      {
        speechService.SetMicrophoneDevice(deviceNumber);
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to load microphone preference: {ex.Message}");
    }
  }

  protected override void WndProc(ref Message m)
  {
    // Handle hotkey messages
    if (m.Msg == WindowsApiHelper.WM_HOTKEY)
    {
      hotkeyManager?.ProcessMessage(m.Msg, m.WParam, m.LParam);
    }
    // Prevent focus border from showing on borderless form
    else if (m.Msg == WindowsApiHelper.WM_NCACTIVATE)
    {
      m.Result = new IntPtr(1); // Always return true to prevent focus border
      return;
    }
    base.WndProc(ref m);
  }
  
  protected override bool ShowFocusCues => false;
  private void DashboardForm_Resize(object? sender, EventArgs e)
  {
    systemTrayService?.HandleFormResize();
  }

  private void InitializePages()
  {
    // Initialize page services
    dictionaryPageService?.Initialize();
    snippetsPageService?.Initialize();
    stylePageService?.Initialize();
    settingsPageService?.Initialize();
    
    // Set initial page via navigation service
    navigationService?.SetInitialPage(panelHomePage, lblNavHome);
  }


  // Navigation methods are now handled by NavigationService

  protected override void OnFormClosing(FormClosingEventArgs e)
  {
    // Clean up services
    speechService?.Dispose();
    hotkeyManager?.Dispose();
    overlayForm?.Close();
    systemTrayService?.Dispose();
    
    base.OnFormClosing(e);
  }

  // Dictionary page functionality is now handled by DictionaryPageService
}