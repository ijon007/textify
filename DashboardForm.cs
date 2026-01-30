using Microsoft.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Linq;

namespace WinFormTest;

public partial class DashboardForm : Form
{
  private string username;
  private SpeechRecognitionService? speechService;
  private GlobalHotkeyManager? hotkeyManager;
  private TextInjectionService? textInjectionService;
  private SpeechOverlayForm? overlayForm;
  private string? recognizedText;
  private DatabaseService? databaseService;
  private Panel? loadingPanel;
  private Label? loadingLabel;
  private Panel? activePagePanel;
  private Label? activeNavItem;
  private DateTime? recordingStartTime;
  
  // Dictionary page components
  private Label? lblDictionaryTitle;
  private Button? btnAddNew;
  private Panel? panelDictionaryList;
  private Label? lblEmptyDictionary;
  private int? editingDictionaryId;

  // Snippets page components
  private Label? lblSnippetsTitle;
  private Button? btnAddNewSnippet;
  private Panel? panelSnippetsList;
  private Label? lblEmptySnippets;
  private int? editingSnippetId;

  // Style page components
  private Label? lblStyleTitle;
  private ClippingPanel? panelFormalCard;
  private ClippingPanel? panelCasualCard;
  private ClippingPanel? panelVeryCasualCard;
  private string? selectedStylePreference;

  // Settings page components
  private Label? lblSettingsTitle;
  private Label? lblHotkeySectionTitle;
  private Label? lblCurrentHotkey;
  private Label? lblCurrentHotkeyValue;
  private Button? btnChangeHotkey;
  private Label? lblHotkeyDescription;

  public DashboardForm(string username)
  {
    this.username = username;
    InitializeComponent();
    SetupRoundedCornersForPages();
    SetupWindowControls();
    
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
    InitializePages();
    LoadDashboardData();
    InitializeLoadingUI();
    InitializeSpeechServicesAsync();
  }

  private void ApplyRoundedCorners(Panel panel, int radius)
  {
    if (panel.Width <= 0 || panel.Height <= 0) return;
    
    System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
    path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
    path.AddArc(panel.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
    path.AddArc(panel.Width - radius * 2, panel.Height - radius * 2, radius * 2, radius * 2, 0, 90);
    path.AddArc(0, panel.Height - radius * 2, radius * 2, radius * 2, 90, 90);
    path.CloseAllFigures();
    panel.Region = new System.Drawing.Region(path);
  }

  private void PanelPage_Paint(object? sender, PaintEventArgs e)
  {
    if (sender is Panel panel)
    {
      DrawRoundedBorder(panel, e.Graphics, 10, Color.FromArgb(200, 200, 200));
    }
  }

  private void DrawRoundedBorder(Panel panel, Graphics g, int radius, Color borderColor)
  {
    using (Pen pen = new Pen(borderColor, 1))
    {
      // Draw rounded rectangle border
      System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
      Rectangle rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
      
      path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
      path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
      path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
      path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
      path.CloseAllFigures();
      
      g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      g.DrawPath(pen, path);
    }
  }

  private void PanelPage_Resize(object? sender, EventArgs e)
  {
    if (sender is Panel panel)
    {
      ApplyRoundedCorners(panel, 10);
      
      // Ensure panelSpeechHistory stays within homePage border bounds
      if (panel == panelHomePage && panelSpeechHistory != null)
      {
        int paddingLeft = 40;
        int paddingRight = 40;
        int paddingTop = 60;
        int paddingBottom = 50;
        int speechHistoryTop = 220; // Y position of panelSpeechHistory
        
        // Calculate available width and height accounting for padding
        int availableWidth = panel.ClientSize.Width - paddingLeft - paddingRight;
        // Available height = total height - top padding - space above speech history - bottom padding
        int availableHeight = panel.ClientSize.Height - paddingTop - (speechHistoryTop - paddingTop) - paddingBottom;
        
        // Update panelSpeechHistory size to stay within bounds
        panelSpeechHistory.Width = availableWidth;
        panelSpeechHistory.Height = Math.Max(0, availableHeight);
      }
    }
  }

  private void SetupRoundedCornersForPages()
  {
    // Apply rounded corners to all page panels
    Panel[] pagePanels = { panelHomePage, panelDictionaryPage, panelSnippetsPage, panelStylePage, panelSettingsPage };
    
    foreach (Panel panel in pagePanels)
    {
      panel.Resize += PanelPage_Resize;
      panel.Paint += PanelPage_Paint;
      ApplyRoundedCorners(panel, 10);
    }
  }

  private void InitializeLoadingUI()
  {
    // Create loading panel
    loadingPanel = new Panel();
    loadingPanel.Dock = DockStyle.Fill;
    loadingPanel.BackColor = Color.FromArgb(45, 45, 48);
    loadingPanel.Visible = true;
    loadingPanel.BringToFront();

    // Create loading label
    loadingLabel = new Label();
    loadingLabel.Text = "Loading speech recognition model...\nPlease wait...";
    loadingLabel.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
    loadingLabel.ForeColor = Color.White;
    loadingLabel.AutoSize = false;
    loadingLabel.TextAlign = ContentAlignment.MiddleCenter;
    loadingLabel.Dock = DockStyle.Fill;
    loadingLabel.Padding = new Padding(20);

    loadingPanel.Controls.Add(loadingLabel);
    this.Controls.Add(loadingPanel);
    loadingPanel.BringToFront();
  }

  private async void InitializeSpeechServicesAsync()
  {
    try
    {
      // Initialize speech recognition service (model loading happens async)
      speechService = new SpeechRecognitionService();
      speechService.SpeechRecognized += SpeechService_SpeechRecognized;
      speechService.SpeechPartialResult += SpeechService_SpeechPartialResult;

      // Initialize text injection service
      textInjectionService = new TextInjectionService();

      // Initialize hotkey manager
      hotkeyManager = new GlobalHotkeyManager(this.Handle);
      hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;
      hotkeyManager.HotkeyReleased += HotkeyManager_HotkeyReleased;
      
      // Load and apply saved hotkey preference
      LoadAndApplyHotkeyPreference();
      
      hotkeyManager.InstallKeyboardHook();

      // Initialize overlay form
      overlayForm = new SpeechOverlayForm();
      overlayForm.Hide();

      // Load model asynchronously
      UpdateLoadingText("Loading Vosk model...");
      await speechService.InitializeAsync();
      
      // Hide loading panel once model is loaded
      if (loadingPanel != null)
      {
        loadingPanel.Visible = false;
        loadingPanel.Dispose();
        loadingPanel = null;
      }
    }
    catch (Exception ex)
    {
      if (loadingPanel != null)
      {
        loadingPanel.Visible = false;
        loadingPanel.Dispose();
        loadingPanel = null;
      }
      MessageBox.Show($"Failed to initialize speech services: {ex.Message}", "Error", 
        MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
  }

  private void UpdateLoadingText(string text)
  {
    if (loadingLabel != null && loadingLabel.InvokeRequired)
    {
      loadingLabel.Invoke(new Action(() => loadingLabel.Text = text));
    }
    else if (loadingLabel != null)
    {
      loadingLabel.Text = text;
    }
  }

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

  private void HotkeyManager_HotkeyPressed(object? sender, EventArgs e)
  {
    if (speechService == null || overlayForm == null || textInjectionService == null)
      return;

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
      // This ensures we capture the window that was active when user pressed hotkey
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
    if (speechService == null || overlayForm == null || textInjectionService == null)
      return;

    try
    {
      speechService.StopListening();
      
      // Wait a short time to allow pending recognition events to complete
      // Recognition events can fire asynchronously after StopListening()
      // Increased delay to 1000ms to give more time for recognition processing
      await Task.Delay(1000);
      
      // Calculate duration if we have a start time
      int? duration = null;
      if (recordingStartTime.HasValue)
      {
        duration = (int)(DateTime.Now - recordingStartTime.Value).TotalMilliseconds;
      }
      
      // Inject recognized text if available
      if (!string.IsNullOrEmpty(recognizedText))
      {
        textInjectionService.InjectText(recognizedText);
        SaveSpeechToDatabase(recognizedText, duration);
        AddSpeechToHistory(recognizedText);
      }

      overlayForm.Hide();
      recognizedText = null;
      recordingStartTime = null;
    }
    catch (Exception ex)
    {
      MessageBox.Show($"Failed to stop speech recognition: {ex.Message}", "Error",
        MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
  }

  private void SpeechService_SpeechPartialResult(object? sender, string text)
  {
    // Partial results are for real-time display only - don't accumulate
    // Show current accumulated text + partial result for preview
    if (overlayForm != null && !string.IsNullOrWhiteSpace(text))
    {
      string displayText = string.IsNullOrEmpty(recognizedText) 
        ? text 
        : recognizedText + " " + text;
      overlayForm.SetRecognizedText(displayText);
    }
  }

  private void SpeechService_SpeechRecognized(object? sender, string text)
  {
    if (overlayForm == null)
      return;

    // Accumulate final recognized text (append with space if there's existing text)
    if (string.IsNullOrEmpty(recognizedText))
    {
      recognizedText = text;
    }
    else
    {
      recognizedText = recognizedText + " " + text;
    }
    
    overlayForm.SetRecognizedText(recognizedText);
  }

  private void SetupWindowControls()
  {
    // Enable dragging from top ribbon panel (acts like native title bar)
    panelTopRibbon.MouseDown += PanelTopRibbon_MouseDown;
    
    // Ensure buttons stay on top and are clickable
    btnClose.BringToFront();
    btnMinimize.BringToFront();
  }
  
  private void PanelTopRibbon_MouseDown(object? sender, MouseEventArgs e)
  {
    if (e.Button == MouseButtons.Left)
    {
      // Don't drag if clicking directly on buttons
      if (sender == btnClose || sender == btnMinimize)
        return;
      
      // Convert to form coordinates to check button positions
      Control? sourceControl = sender as Control;
      if (sourceControl != null)
      {
        Point formPoint = sourceControl.PointToScreen(e.Location);
        formPoint = this.PointToClient(formPoint);
        
        // Allow dragging everywhere except button area (x >= 1140)
        if (formPoint.X < 1140)
        {
          WindowsApiHelper.ReleaseCapture();
          WindowsApiHelper.SendMessage(this.Handle, WindowsApiHelper.WM_NCLBUTTONDOWN, 
            WindowsApiHelper.HT_CAPTION, 0);
        }
      }
    }
  }

  private void DashboardForm_MouseDown(object? sender, MouseEventArgs e)
  {
    // This method is kept for backward compatibility but is no longer the primary handler
    // The top ribbon panel now handles its own dragging via PanelTopRibbon_MouseDown
  }

  private void btnClose_Click(object? sender, EventArgs e)
  {
    // Remove focus from button before closing
    this.Focus();
    this.Close();
  }

  private void btnClose_MouseEnter(object? sender, EventArgs e)
  {
    btnClose.ForeColor = Color.White;
    btnClose.BackColor = Color.FromArgb(232, 17, 35);
  }

  private void btnClose_MouseLeave(object? sender, EventArgs e)
  {
    btnClose.ForeColor = Color.FromArgb(100, 100, 100);
    btnClose.BackColor = Color.Transparent;
  }

  private void btnMinimize_Click(object? sender, EventArgs e)
  {
    // Remove focus from button before minimizing
    this.Focus();
    this.WindowState = FormWindowState.Minimized;
  }

  private void btnMinimize_MouseEnter(object? sender, EventArgs e)
  {
    btnMinimize.ForeColor = Color.FromArgb(45, 45, 48);
    btnMinimize.BackColor = Color.FromArgb(240, 240, 240);
  }

  private void btnMinimize_MouseLeave(object? sender, EventArgs e)
  {
    btnMinimize.ForeColor = Color.FromArgb(100, 100, 100);
    btnMinimize.BackColor = Color.Transparent;
  }

  private void LoadDashboardData()
  {
    // Set welcome message with username
    lblWelcome.Text = $"Welcome back, {username}!";
    
    // Load sample speech entries
    LoadSpeechHistory();
    
    // Load and display statistics
    RefreshStats();
  }

  private void RefreshStats()
  {
    if (databaseService == null)
      return;

    try
    {
      // Get consecutive weeks streak
      int weeksStreak = databaseService.GetConsecutiveWeeksStreak(username);
      lblStatWeeks.Text = weeksStreak == 1 ? "🔥 1 week" : $"🔥 {weeksStreak} weeks";

      // Get total words
      int totalWords = databaseService.GetTotalWords(username);
      if (totalWords >= 1000)
      {
        double wordsK = totalWords / 1000.0;
        lblStatWords.Text = $"🚀 {wordsK:F1}K words";
      }
      else
      {
        lblStatWords.Text = $"🚀 {totalWords} words";
      }

      // Get average WPM
      int averageWPM = databaseService.GetAverageWPM(username);
      lblStatWPM.Text = $"🏆 {averageWPM} WPM";
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to refresh stats: {ex.Message}");
      // Set default values on error
      lblStatWeeks.Text = "🔥 0 weeks";
      lblStatWords.Text = "🚀 0 words";
      lblStatWPM.Text = "🏆 0 WPM";
    }
  }

  private void LoadSpeechHistory()
  {
    if (databaseService == null)
      return;

    var speeches = databaseService.GetSpeeches(username);

    int yOffset = 10;
    const int spacingBetweenItems = 15; // Reduced spacing between transcription items
    const int fixedPanelHeight = 70; // Fixed height for each item (smaller)
    
    foreach (var speech in speeches)
    {
      CreateSpeechRow(speech.id, speech.time, speech.text, yOffset);
      
      // Calculate next position: fixed panel height + spacing between items
      yOffset += fixedPanelHeight + spacingBetweenItems;
    }
  }

  // Sidebar navigation hover effects
  private void navItem_MouseEnter(object? sender, EventArgs e)
  {
    if (sender is Label label)
    {
      label.BackColor = Color.FromArgb(245, 245, 245);
      label.ForeColor = Color.Black;
    }
  }

  private void navItem_MouseLeave(object? sender, EventArgs e)
  {
    if (sender is Label label)
    {
      // Only restore gray if it's not the active navigation item
      if (activeNavItem == null || label != activeNavItem)
      {
        label.BackColor = Color.Transparent;
        label.ForeColor = Color.FromArgb(100, 100, 100);
      }
    }
  }

  private void SaveSpeechToDatabase(string text, int? duration = null)
  {
    if (databaseService != null && !string.IsNullOrWhiteSpace(text))
    {
      databaseService.SaveSpeech(username, text, duration);
    }
  }

  private string TruncateText(string text, Font font, int maxWidth, int maxHeight)
  {
    if (string.IsNullOrEmpty(text))
      return text;

    // Create a temporary label to measure text accurately
    using (Label tempLabel = new Label())
    {
      tempLabel.Font = font;
      tempLabel.Text = text;
      tempLabel.AutoSize = true;
      tempLabel.MaximumSize = new Size(maxWidth, 0);
      
      // If text fits within maxHeight, return as is
      if (tempLabel.Height <= maxHeight)
        return text;

      // Text is too long, need to truncate
      string ellipsis = "...";
      
      // Binary search approach: find the maximum length that fits
      int minLength = 0;
      int maxLength = text.Length;
      
      while (minLength < maxLength)
      {
        int midLength = (minLength + maxLength + 1) / 2;
        string testText = text.Substring(0, midLength) + ellipsis;
        tempLabel.Text = testText;
        
        if (tempLabel.Height <= maxHeight)
        {
          minLength = midLength;
        }
        else
        {
          maxLength = midLength - 1;
        }
      }
      
      if (minLength == 0)
        return ellipsis;
      
      return text.Substring(0, minLength) + ellipsis;
    }
  }

  private void CreateSpeechRow(int id, string time, string text, int yOffset)
  {
    // Calculate available width accounting for padding and scrollbar
    int availableWidth = panelSpeechHistory.ClientSize.Width - 20; // 10px padding on each side
    
    // Fixed panel height for smaller items
    const int fixedPanelHeight = 70;

    // Create container panel for this speech entry
    ClippingPanel entryPanel = new ClippingPanel();
    entryPanel.BackColor = Color.White;
    entryPanel.Location = new Point(10, yOffset);
    entryPanel.Size = new Size(availableWidth, fixedPanelHeight);
    entryPanel.Name = $"panelEntry_{id}";
    entryPanel.Paint += EntryPanel_Paint;
    entryPanel.AutoSize = false;
    entryPanel.AutoSizeMode = AutoSizeMode.GrowOnly;

    // Create timestamp label
    Label lblTime = new Label();
    lblTime.Text = time;
    lblTime.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    lblTime.ForeColor = Color.FromArgb(100, 100, 100);
    lblTime.BackColor = Color.White;
    lblTime.Location = new Point(10, 10);
    lblTime.AutoSize = true;
    lblTime.Name = $"lblTime_{id}";

    // Create text label - ensure it doesn't overlap with button
    // Button is at Width - 50, button width is 30, so text max width = Width - 50 - 30 - 10 (spacing) - 10 (left padding)
    int textMaxWidth = entryPanel.Width - 100;
    // Calculate available height: panel height (70) - timestamp top (10) - timestamp height (~15) - text top margin (5) - bottom padding (10)
    int textMaxHeight = fixedPanelHeight - 30 - 10; // ~30px for 2 lines max
    Font textFont = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    
    // Truncate text if needed
    string displayText = TruncateText(text, textFont, textMaxWidth, textMaxHeight);
    
    Label lblText = new Label();
    lblText.Text = displayText;
    lblText.Font = textFont;
    lblText.ForeColor = Color.FromArgb(45, 45, 48);
    lblText.BackColor = Color.White;
    lblText.Location = new Point(10, 30);
    lblText.AutoSize = false;
    lblText.Size = new Size(textMaxWidth, textMaxHeight);
    lblText.Name = $"lblText_{id}";
    lblText.UseCompatibleTextRendering = false;
    lblText.TextAlign = ContentAlignment.TopLeft;
    // Ensure text wraps and doesn't overflow
    lblText.MaximumSize = new Size(textMaxWidth, textMaxHeight);
    // Clip the label to its bounds
    System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
    path.AddRectangle(new Rectangle(0, 0, textMaxWidth, textMaxHeight));
    lblText.Region = new System.Drawing.Region(path);

    // Create copy button
    Button btnCopy = new Button();
    btnCopy.Text = "📋";
    btnCopy.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    btnCopy.Size = new Size(30, 30);
    btnCopy.FlatStyle = FlatStyle.Flat;
    btnCopy.FlatAppearance.BorderSize = 0;
    btnCopy.BackColor = Color.FromArgb(245, 245, 245);
    btnCopy.ForeColor = Color.FromArgb(100, 100, 100);
    btnCopy.Cursor = Cursors.Hand;
    btnCopy.Name = $"btnCopy_{id}";
    
    // Position button next to the text (right side)
    int buttonX = entryPanel.Width - 50;
    int buttonY = 30;
    btnCopy.Location = new Point(buttonX, buttonY);
    
    // Store the text in Tag for easy access
    btnCopy.Tag = text;
    btnCopy.Click += BtnCopy_Click;
    btnCopy.MouseEnter += BtnCopy_MouseEnter;
    btnCopy.MouseLeave += BtnCopy_MouseLeave;

    // Add controls to entry panel
    entryPanel.Controls.Add(lblTime);
    entryPanel.Controls.Add(lblText);
    entryPanel.Controls.Add(btnCopy);

    // Add entry panel to speech history panel
    panelSpeechHistory.Controls.Add(entryPanel);
  }

  private void EntryPanel_Paint(object? sender, PaintEventArgs e)
  {
    if (sender is Panel panel)
    {
      // Draw only bottom border with no rounding
      using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
      {
        e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
      }
    }
  }

  private void BtnCopy_Click(object? sender, EventArgs e)
  {
    if (sender is Button btn && btn.Tag is string text)
    {
      try
      {
        Clipboard.SetText(text);
        // Optional: Show brief feedback
        btn.Text = "✓";
        Task.Delay(1000).ContinueWith(_ =>
        {
          if (btn.InvokeRequired)
          {
            btn.Invoke(new Action(() => btn.Text = "📋"));
          }
          else
          {
            btn.Text = "📋";
          }
        });
      }
      catch
      {
        // Ignore clipboard errors
      }
    }
  }

  private void BtnCopy_MouseEnter(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(230, 230, 230);
    }
  }

  private void BtnCopy_MouseLeave(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(245, 245, 245);
    }
  }

  public void AddSpeechToHistory(string text)
  {
    if (string.IsNullOrEmpty(text))
      return;

    // Get current time
    string time = DateTime.Now.ToString("hh:mm tt");
    
    // Use a temporary ID for new entries (they'll get real IDs from DB on next load)
    int tempId = (int)DateTime.Now.Ticks;

    // Calculate position (add at top)
    int yOffset = 10;
    const int spacingBetweenItems = 15; // Reduced spacing between transcription items
    const int fixedPanelHeight = 70; // Fixed height for each item

    // Calculate the height of the new item to move existing controls down
    int newItemHeight = fixedPanelHeight + spacingBetweenItems;
    
    // Move existing controls down by the height of the new item
    foreach (Control control in panelSpeechHistory.Controls)
    {
      control.Location = new Point(control.Location.X, control.Location.Y + newItemHeight);
    }

    // Create the new speech row
    CreateSpeechRow(tempId, time, text, yOffset);

    // Scroll to top
    panelSpeechHistory.AutoScrollPosition = new Point(0, 0);
    
    // Refresh statistics after adding new speech
    RefreshStats();
  }

  private void InitializePages()
  {
    // Set up Dictionary page with full functionality
    InitializeDictionaryPage();
    
    // Set up Snippets page with full functionality
    InitializeSnippetsPage();
    
    // Set up Style page with full functionality
    InitializeStylePage();
    
    // Set up Settings page with full functionality
    InitializeSettingsPage();
    
    // Initially hide all pages except Home
    panelDictionaryPage.Visible = false;
    panelSnippetsPage.Visible = false;
    panelStylePage.Visible = false;
    panelSettingsPage.Visible = false;
    panelHomePage.Visible = true;
    
    // Set Home as default active page
    activePagePanel = panelHomePage;
    activeNavItem = lblNavHome;
    SetActiveNavItem(lblNavHome);
  }

  private void CreatePlaceholderPage(Panel panel, string title)
  {
    panel.BackColor = Color.White;
    panel.Dock = DockStyle.Fill;
    panel.Padding = new Padding(40, 60, 40, 40);
    
    Label lblTitle = new Label();
    lblTitle.Text = title;
    lblTitle.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point);
    lblTitle.ForeColor = Color.FromArgb(45, 45, 48);
    lblTitle.Location = new Point(40, 60);
    lblTitle.AutoSize = true;
    lblTitle.Name = $"lbl{title}Title";
    
    Label lblPlaceholder = new Label();
    lblPlaceholder.Text = $"This is the {title} page.\nContent will be added here.";
    lblPlaceholder.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    lblPlaceholder.ForeColor = Color.FromArgb(100, 100, 100);
    lblPlaceholder.Location = new Point(40, 120);
    lblPlaceholder.AutoSize = true;
    lblPlaceholder.Name = $"lbl{title}Placeholder";
    
    panel.Controls.Add(lblTitle);
    panel.Controls.Add(lblPlaceholder);
  }

  private void SwitchPage(Panel targetPage, Label activeNavLabel)
  {
    // Hide all page panels
    panelHomePage.Visible = false;
    panelDictionaryPage.Visible = false;
    panelSnippetsPage.Visible = false;
    panelStylePage.Visible = false;
    panelSettingsPage.Visible = false;
    
    // Show target page
    targetPage.Visible = true;
    activePagePanel = targetPage;
    
    // Update active navigation item
    SetActiveNavItem(activeNavLabel);
  }

  private void SetActiveNavItem(Label navItem)
  {
    // Reset all navigation items to inactive state
    lblNavHome.ForeColor = Color.FromArgb(100, 100, 100);
    lblNavHome.BackColor = Color.Transparent;
    lblNavDictionary.ForeColor = Color.FromArgb(100, 100, 100);
    lblNavDictionary.BackColor = Color.Transparent;
    lblNavSnippets.ForeColor = Color.FromArgb(100, 100, 100);
    lblNavSnippets.BackColor = Color.Transparent;
    lblNavStyle.ForeColor = Color.FromArgb(100, 100, 100);
    lblNavStyle.BackColor = Color.Transparent;
    lblNavSettings.ForeColor = Color.FromArgb(100, 100, 100);
    lblNavSettings.BackColor = Color.Transparent;
    
    // Set active navigation item styling
    navItem.ForeColor = Color.Black;
    navItem.BackColor = Color.FromArgb(245, 245, 245);
    activeNavItem = navItem;
  }

  private void navItem_Click(object? sender, EventArgs e)
  {
    if (sender is Label label)
    {
      if (label == lblNavHome)
      {
        SwitchPage(panelHomePage, lblNavHome);
      }
      else if (label == lblNavDictionary)
      {
        SwitchPage(panelDictionaryPage, lblNavDictionary);
      }
      else if (label == lblNavSnippets)
      {
        SwitchPage(panelSnippetsPage, lblNavSnippets);
      }
      else if (label == lblNavStyle)
      {
        SwitchPage(panelStylePage, lblNavStyle);
      }
      else if (label == lblNavSettings)
      {
        SwitchPage(panelSettingsPage, lblNavSettings);
      }
    }
  }

  protected override void OnFormClosing(FormClosingEventArgs e)
  {
    // Clean up services
    speechService?.Dispose();
    hotkeyManager?.Dispose();
    overlayForm?.Close();
    
    base.OnFormClosing(e);
  }

  // Dictionary Page Methods

  private void InitializeDictionaryPage()
  {
    panelDictionaryPage.BackColor = Color.White;
    panelDictionaryPage.Dock = DockStyle.Fill;
    panelDictionaryPage.Padding = new Padding(40, 60, 40, 50);

    // Page Title
    lblDictionaryTitle = new Label();
    lblDictionaryTitle.Text = "Dictionary";
    lblDictionaryTitle.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point);
    lblDictionaryTitle.ForeColor = Color.FromArgb(45, 45, 48);
    lblDictionaryTitle.Location = new Point(40, 60);
    lblDictionaryTitle.AutoSize = true;
    lblDictionaryTitle.Name = "lblDictionaryTitle";

    // Add New Button (top-right)
    btnAddNew = new Button();
    btnAddNew.Text = "Add new";
    btnAddNew.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    btnAddNew.FlatStyle = FlatStyle.Flat;
    btnAddNew.FlatAppearance.BorderSize = 0;
    btnAddNew.BackColor = Color.FromArgb(45, 45, 48);
    btnAddNew.ForeColor = Color.White;
    btnAddNew.Cursor = Cursors.Hand;
    btnAddNew.Size = new Size(100, 35);
    btnAddNew.Name = "btnAddNew";
    btnAddNew.Click += BtnAddNew_Click;
    btnAddNew.MouseEnter += BtnAddNew_MouseEnter;
    btnAddNew.MouseLeave += BtnAddNew_MouseLeave;

    // Dictionary List Panel
    panelDictionaryList = new Panel();
    panelDictionaryList.AutoScroll = true;
    panelDictionaryList.BackColor = Color.White;
    panelDictionaryList.Location = new Point(40, 130);
    panelDictionaryList.Size = new Size(870, 610);
    panelDictionaryList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
    panelDictionaryList.Padding = new Padding(0, 0, 0, 10);
    panelDictionaryList.Name = "panelDictionaryList";

    // Empty State Label
    lblEmptyDictionary = new Label();
    lblEmptyDictionary.Text = "No dictionary entries yet. Click 'Add new' to add your first word!";
    lblEmptyDictionary.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    lblEmptyDictionary.ForeColor = Color.FromArgb(100, 100, 100);
    lblEmptyDictionary.AutoSize = false;
    lblEmptyDictionary.TextAlign = ContentAlignment.MiddleCenter;
    lblEmptyDictionary.Size = new Size(870, 50);
    lblEmptyDictionary.Location = new Point(0, 50);
    lblEmptyDictionary.Name = "lblEmptyDictionary";

    // Add controls to dictionary page
    panelDictionaryPage.Controls.Add(lblDictionaryTitle);
    panelDictionaryPage.Controls.Add(btnAddNew);
    panelDictionaryPage.Controls.Add(panelDictionaryList);
    panelDictionaryList.Controls.Add(lblEmptyDictionary);

    // Position Add New button in top-right
    UpdateAddNewButtonPosition();

    // Load dictionary entries
    LoadDictionaryEntries();

    // Handle resize
    panelDictionaryPage.Resize += PanelDictionaryPage_Resize;
  }

  private void PanelDictionaryPage_Resize(object? sender, EventArgs e)
  {
    if (panelDictionaryPage != null)
    {
      // Update dictionary list height
      if (panelDictionaryList != null)
      {
        int paddingLeft = 40;
        int paddingRight = 40;
        int paddingTop = 60;
        int paddingBottom = 50;
        int listTop = 130;

        int availableWidth = panelDictionaryPage.ClientSize.Width - paddingLeft - paddingRight;
        int availableHeight = panelDictionaryPage.ClientSize.Height - paddingTop - (listTop - paddingTop) - paddingBottom;

        panelDictionaryList.Width = availableWidth;
        panelDictionaryList.Height = Math.Max(0, availableHeight);
      }

      UpdateAddNewButtonPosition();
    }
  }

  private void UpdateAddNewButtonPosition()
  {
    if (btnAddNew != null && panelDictionaryPage != null)
    {
      int rightPadding = 50; // Increased padding to prevent cutoff
      int topPadding = 60;
      int maxX = panelDictionaryPage.ClientSize.Width - btnAddNew.Width - rightPadding;
      btnAddNew.Location = new Point(Math.Max(40, maxX), topPadding);
    }
  }

  private void LoadDictionaryEntries()
  {
    if (databaseService == null || panelDictionaryList == null)
      return;

    // Clear existing entries
    panelDictionaryList.Controls.Clear();

    var entries = databaseService.GetDictionaryEntries(username);

    if (entries.Count == 0)
    {
      // Show empty state
      if (lblEmptyDictionary != null)
      {
        panelDictionaryList.Controls.Add(lblEmptyDictionary);
        lblEmptyDictionary.Location = new Point((panelDictionaryList.Width - lblEmptyDictionary.Width) / 2, 50);
      }
      return;
    }

    // Hide empty state
    lblEmptyDictionary?.Hide();

    int yOffset = 10;
    const int spacingBetweenItems = 0;
    const int fixedPanelHeight = 60;

    foreach (var entry in entries)
    {
      CreateDictionaryRow(entry.id, entry.word, yOffset);
      yOffset += fixedPanelHeight + spacingBetweenItems;
    }
  }

  private void CreateDictionaryRow(int id, string word, int yOffset)
  {
    if (panelDictionaryList == null)
      return;

    int availableWidth = panelDictionaryList.ClientSize.Width - 20;
    const int fixedPanelHeight = 60;

    // Create container panel
    ClippingPanel entryPanel = new ClippingPanel();
    entryPanel.BackColor = Color.White;
    entryPanel.Location = new Point(10, yOffset);
    entryPanel.Size = new Size(availableWidth, fixedPanelHeight);
    entryPanel.Name = $"panelEntry_{id}";
    entryPanel.Paint += DictionaryEntryPanel_Paint;
    entryPanel.AutoSize = false;

    // Create word label
    Label lblWord = new Label();
    lblWord.Text = word;
    lblWord.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    lblWord.ForeColor = Color.FromArgb(45, 45, 48);
    lblWord.BackColor = Color.White;
    lblWord.Location = new Point(10, 15);
    lblWord.AutoSize = false;
    lblWord.Size = new Size(availableWidth - 100, 30);
    lblWord.Name = $"lblWord_{id}";
    lblWord.TextAlign = ContentAlignment.MiddleLeft;

    // Create edit button
    Button btnEdit = new Button();
    btnEdit.Text = "✏️";
    btnEdit.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    btnEdit.Size = new Size(30, 30);
    btnEdit.FlatStyle = FlatStyle.Flat;
    btnEdit.FlatAppearance.BorderSize = 0;
    btnEdit.BackColor = Color.FromArgb(245, 245, 245);
    btnEdit.ForeColor = Color.FromArgb(100, 100, 100);
    btnEdit.Cursor = Cursors.Hand;
    btnEdit.Name = $"btnEdit_{id}";
    btnEdit.Location = new Point(entryPanel.Width - 90, 15);
    btnEdit.Tag = id;
    btnEdit.Click += BtnEdit_Click;
    btnEdit.MouseEnter += BtnEdit_MouseEnter;
    btnEdit.MouseLeave += BtnEdit_MouseLeave;

    // Create delete button
    Button btnDelete = new Button();
    btnDelete.Text = "🗑️";
    btnDelete.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    btnDelete.Size = new Size(30, 30);
    btnDelete.FlatStyle = FlatStyle.Flat;
    btnDelete.FlatAppearance.BorderSize = 0;
    btnDelete.BackColor = Color.FromArgb(245, 245, 245);
    btnDelete.ForeColor = Color.FromArgb(100, 100, 100);
    btnDelete.Cursor = Cursors.Hand;
    btnDelete.Name = $"btnDelete_{id}";
    btnDelete.Location = new Point(entryPanel.Width - 50, 15);
    btnDelete.Tag = id;
    btnDelete.Click += BtnDelete_Click;
    btnDelete.MouseEnter += BtnDelete_MouseEnter;
    btnDelete.MouseLeave += BtnDelete_MouseLeave;

    // Add controls to entry panel
    entryPanel.Controls.Add(lblWord);
    entryPanel.Controls.Add(btnEdit);
    entryPanel.Controls.Add(btnDelete);

    // Add entry panel to list
    panelDictionaryList.Controls.Add(entryPanel);
  }

  private void DictionaryEntryPanel_Paint(object? sender, PaintEventArgs e)
  {
    if (sender is Panel panel)
    {
      using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
      {
        e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
      }
    }
  }

  private void BtnAddNew_Click(object? sender, EventArgs e)
  {
    ShowDictionaryTaskDialog(isAdding: true);
  }

  private void BtnAddNew_MouseEnter(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(35, 35, 38);
    }
  }

  private void BtnAddNew_MouseLeave(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(45, 45, 48);
    }
  }

  private void BtnEdit_Click(object? sender, EventArgs e)
  {
    if (sender is Button btn && btn.Tag is int id)
    {
      // Find the word for this entry
      if (databaseService == null)
        return;

      var entries = databaseService.GetDictionaryEntries(username);
      var entry = entries.FirstOrDefault(e => e.id == id);

      if (entry.id == 0)
      {
        MessageBox.Show("Dictionary entry not found.", "Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }

      editingDictionaryId = id;
      ShowDictionaryTaskDialog(isAdding: false, word: entry.word);
    }
  }

  private void ShowDictionaryTaskDialog(bool isAdding, string word = "")
  {
    if (databaseService == null)
      return;

    // Create backdrop overlay Form (separate window)
    Form backdrop = new Form();
    backdrop.FormBorderStyle = FormBorderStyle.None;
    backdrop.WindowState = FormWindowState.Normal;
    backdrop.StartPosition = FormStartPosition.Manual;
    backdrop.Size = this.Size;
    backdrop.Location = this.Location;
    backdrop.BackColor = Color.Black;
    backdrop.Opacity = 0.5;
    backdrop.ShowInTaskbar = false;
    backdrop.TopMost = true;
    backdrop.Enabled = true;

    // Create a Form-based dialog styled like TaskDialog
    Form dialog = new Form();
    dialog.Text = "";
    dialog.FormBorderStyle = FormBorderStyle.None;
    dialog.Size = new Size(500, 200);
    dialog.StartPosition = FormStartPosition.CenterParent;
    dialog.BackColor = Color.White;
    dialog.ShowInTaskbar = false;
    dialog.TopMost = true;
    
    // Set backdrop click handler to close dialog
    backdrop.Click += (s, e) => dialog.DialogResult = DialogResult.Cancel;

    // Show backdrop first, then dialog
    backdrop.Show();
    backdrop.BringToFront();
    dialog.BringToFront();

    // Title label
    Label lblTitle = new Label();
    lblTitle.Text = isAdding ? "Add new word" : "Edit word";
    lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
    lblTitle.ForeColor = Color.FromArgb(45, 45, 48);
    lblTitle.Location = new Point(20, 20);
    lblTitle.AutoSize = true;

    // Close button
    Button btnClose = new Button();
    btnClose.Text = "×";
    btnClose.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point);
    btnClose.FlatStyle = FlatStyle.Flat;
    btnClose.FlatAppearance.BorderSize = 0;
    btnClose.BackColor = Color.Transparent;
    btnClose.ForeColor = Color.FromArgb(100, 100, 100);
    btnClose.Size = new Size(30, 30);
    btnClose.Location = new Point(460, 10);
    btnClose.Cursor = Cursors.Hand;
    btnClose.TextAlign = ContentAlignment.MiddleCenter;
    btnClose.Click += (s, e) => dialog.DialogResult = DialogResult.Cancel;
    btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.FromArgb(45, 45, 48); btnClose.BackColor = Color.FromArgb(245, 245, 245); };
    btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = Color.FromArgb(100, 100, 100); btnClose.BackColor = Color.Transparent; };

    // Text input
    TextBox txtWord = new TextBox();
    txtWord.Location = new Point(20, 70);
    txtWord.Size = new Size(460, 25);
    txtWord.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    txtWord.BorderStyle = BorderStyle.FixedSingle;
    txtWord.Text = word;
    txtWord.SelectAll();

    // Cancel button
    Button btnCancel = new Button();
    btnCancel.Text = "Cancel";
    btnCancel.Location = new Point(290, 130);
    btnCancel.Size = new Size(80, 28);
    btnCancel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    btnCancel.FlatStyle = FlatStyle.Flat;
    btnCancel.FlatAppearance.BorderSize = 0;
    btnCancel.BackColor = Color.FromArgb(235, 235, 235);
    btnCancel.ForeColor = Color.FromArgb(45, 45, 48);
    btnCancel.Cursor = Cursors.Hand;
    btnCancel.DialogResult = DialogResult.Cancel;
    btnCancel.TextAlign = ContentAlignment.MiddleCenter;
    btnCancel.MouseEnter += (s, e) => btnCancel.BackColor = Color.FromArgb(220, 220, 220);
    btnCancel.MouseLeave += (s, e) => btnCancel.BackColor = Color.FromArgb(235, 235, 235);

    // Save button
    Button btnSave = new Button();
    btnSave.Text = isAdding ? "Add" : "Save changes";
    btnSave.Location = new Point(380, 130);
    btnSave.Size = new Size(100, 28);
    btnSave.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    btnSave.FlatStyle = FlatStyle.Flat;
    btnSave.FlatAppearance.BorderSize = 0;
    btnSave.BackColor = Color.FromArgb(45, 45, 48);
    btnSave.ForeColor = Color.White;
    btnSave.Cursor = Cursors.Hand;
    btnSave.DialogResult = DialogResult.OK;
    btnSave.TextAlign = ContentAlignment.MiddleCenter;
    btnSave.MouseEnter += (s, e) => btnSave.BackColor = Color.FromArgb(35, 35, 38);
    btnSave.MouseLeave += (s, e) => btnSave.BackColor = Color.FromArgb(45, 45, 48);

    // Add controls
    dialog.Controls.Add(lblTitle);
    dialog.Controls.Add(btnClose);
    dialog.Controls.Add(txtWord);
    dialog.Controls.Add(btnCancel);
    dialog.Controls.Add(btnSave);

    // Draw square border
    dialog.Paint += (s, e) =>
    {
      using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
      {
        Rectangle rect = new Rectangle(0, 0, dialog.Width - 1, dialog.Height - 1);
        e.Graphics.DrawRectangle(pen, rect);
      }
    };

    // Set focus and show
    dialog.AcceptButton = btnSave;
    dialog.CancelButton = btnCancel;
    txtWord.Focus();

    // Close backdrop when dialog closes
    dialog.FormClosed += (s, e) => 
    {
      backdrop.Close();
      backdrop.Dispose();
    };

    if (dialog.ShowDialog(this) == DialogResult.OK)
    {
      string wordText = txtWord.Text.Trim();

      if (string.IsNullOrWhiteSpace(wordText))
      {
        MessageBox.Show("Please enter a word or phrase.", "Validation Error",
          MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      try
      {
        if (isAdding)
        {
          databaseService.AddDictionaryEntry(username, wordText);
        }
        else
        {
          if (!editingDictionaryId.HasValue)
          {
            MessageBox.Show("No entry selected for editing.", "Error",
              MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
          }
          databaseService.UpdateDictionaryEntry(editingDictionaryId.Value, username, wordText);
        }

        RefreshDictionaryList();
      }
      catch (InvalidOperationException ex)
      {
        MessageBox.Show(ex.Message, "Error",
          MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
      catch (Exception ex)
      {
        string action = isAdding ? "add" : "update";
        MessageBox.Show($"Failed to {action} dictionary entry: {ex.Message}", "Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }
  }

  private void BtnEdit_MouseEnter(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(230, 230, 230);
    }
  }

  private void BtnEdit_MouseLeave(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(245, 245, 245);
    }
  }

  private void BtnDelete_Click(object? sender, EventArgs e)
  {
    if (sender is Button btn && btn.Tag is int id && databaseService != null)
    {
      // Find the word for confirmation message
      var entries = databaseService.GetDictionaryEntries(username);
      var entry = entries.FirstOrDefault(e => e.id == id);

      string wordText = entry.id == id ? entry.word : "this entry";

      DialogResult result = MessageBox.Show(
        $"Are you sure you want to delete \"{wordText}\"?",
        "Confirm Delete",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question);

      if (result == DialogResult.Yes)
      {
        try
        {
          databaseService.DeleteDictionaryEntry(id, username);
          RefreshDictionaryList();
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Failed to delete dictionary entry: {ex.Message}", "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      }
    }
  }

  private void BtnDelete_MouseEnter(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(230, 230, 230);
    }
  }

  private void BtnDelete_MouseLeave(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(245, 245, 245);
    }
  }


  private void RefreshDictionaryList()
  {
    LoadDictionaryEntries();
  }

  // Snippets Page Methods

  private void InitializeSnippetsPage()
  {
    panelSnippetsPage.BackColor = Color.White;
    panelSnippetsPage.Dock = DockStyle.Fill;
    panelSnippetsPage.Padding = new Padding(40, 60, 40, 50);

    // Page Title
    lblSnippetsTitle = new Label();
    lblSnippetsTitle.Text = "Snippets";
    lblSnippetsTitle.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point);
    lblSnippetsTitle.ForeColor = Color.FromArgb(45, 45, 48);
    lblSnippetsTitle.Location = new Point(40, 60);
    lblSnippetsTitle.AutoSize = true;
    lblSnippetsTitle.Name = "lblSnippetsTitle";

    // Add New Button (top-right)
    btnAddNewSnippet = new Button();
    btnAddNewSnippet.Text = "Add new";
    btnAddNewSnippet.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    btnAddNewSnippet.FlatStyle = FlatStyle.Flat;
    btnAddNewSnippet.FlatAppearance.BorderSize = 0;
    btnAddNewSnippet.BackColor = Color.FromArgb(45, 45, 48);
    btnAddNewSnippet.ForeColor = Color.White;
    btnAddNewSnippet.Cursor = Cursors.Hand;
    btnAddNewSnippet.Size = new Size(100, 35);
    btnAddNewSnippet.Name = "btnAddNewSnippet";
    btnAddNewSnippet.Click += BtnAddNewSnippet_Click;
    btnAddNewSnippet.MouseEnter += BtnAddNewSnippet_MouseEnter;
    btnAddNewSnippet.MouseLeave += BtnAddNewSnippet_MouseLeave;

    // Snippets List Panel
    panelSnippetsList = new Panel();
    panelSnippetsList.AutoScroll = true;
    panelSnippetsList.BackColor = Color.White;
    panelSnippetsList.Location = new Point(40, 130);
    panelSnippetsList.Size = new Size(870, 610);
    panelSnippetsList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
    panelSnippetsList.Padding = new Padding(0, 0, 0, 10);
    panelSnippetsList.Name = "panelSnippetsList";

    // Empty State Label
    lblEmptySnippets = new Label();
    lblEmptySnippets.Text = "No snippets yet. Click 'Add new' to add your first shortcut!";
    lblEmptySnippets.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    lblEmptySnippets.ForeColor = Color.FromArgb(100, 100, 100);
    lblEmptySnippets.AutoSize = false;
    lblEmptySnippets.TextAlign = ContentAlignment.MiddleCenter;
    lblEmptySnippets.Size = new Size(870, 50);
    lblEmptySnippets.Location = new Point(0, 50);
    lblEmptySnippets.Name = "lblEmptySnippets";

    // Add controls to snippets page
    panelSnippetsPage.Controls.Add(lblSnippetsTitle);
    panelSnippetsPage.Controls.Add(btnAddNewSnippet);
    panelSnippetsPage.Controls.Add(panelSnippetsList);
    panelSnippetsList.Controls.Add(lblEmptySnippets);

    // Position Add New button in top-right
    UpdateAddNewSnippetButtonPosition();

    // Load snippets entries
    LoadSnippetsEntries();

    // Handle resize
    panelSnippetsPage.Resize += PanelSnippetsPage_Resize;
  }

  private void PanelSnippetsPage_Resize(object? sender, EventArgs e)
  {
    if (panelSnippetsPage != null)
    {
      // Update snippets list height
      if (panelSnippetsList != null)
      {
        int paddingLeft = 40;
        int paddingRight = 40;
        int paddingTop = 60;
        int paddingBottom = 50;
        int listTop = 130;

        int availableWidth = panelSnippetsPage.ClientSize.Width - paddingLeft - paddingRight;
        int availableHeight = panelSnippetsPage.ClientSize.Height - paddingTop - (listTop - paddingTop) - paddingBottom;

        panelSnippetsList.Width = availableWidth;
        panelSnippetsList.Height = Math.Max(0, availableHeight);
      }

      UpdateAddNewSnippetButtonPosition();
    }
  }

  private void UpdateAddNewSnippetButtonPosition()
  {
    if (btnAddNewSnippet != null && panelSnippetsPage != null)
    {
      int rightPadding = 50;
      int topPadding = 60;
      int maxX = panelSnippetsPage.ClientSize.Width - btnAddNewSnippet.Width - rightPadding;
      btnAddNewSnippet.Location = new Point(Math.Max(40, maxX), topPadding);
    }
  }

  private void LoadSnippetsEntries()
  {
    if (databaseService == null || panelSnippetsList == null)
      return;

    // Clear existing entries
    panelSnippetsList.Controls.Clear();

    var snippets = databaseService.GetSnippets(username);

    if (snippets.Count == 0)
    {
      // Show empty state
      if (lblEmptySnippets != null)
      {
        panelSnippetsList.Controls.Add(lblEmptySnippets);
        lblEmptySnippets.Location = new Point((panelSnippetsList.Width - lblEmptySnippets.Width) / 2, 50);
      }
      return;
    }

    // Hide empty state
    lblEmptySnippets?.Hide();

    int yOffset = 10;
    const int spacingBetweenItems = 0;
    const int fixedPanelHeight = 60;

    foreach (var snippet in snippets)
    {
      CreateSnippetRow(snippet.id, snippet.shortcut, snippet.replacement, yOffset);
      yOffset += fixedPanelHeight + spacingBetweenItems;
    }
  }

  private void CreateSnippetRow(int id, string shortcut, string replacement, int yOffset)
  {
    if (panelSnippetsList == null)
      return;

    int availableWidth = panelSnippetsList.ClientSize.Width - 20;
    const int fixedPanelHeight = 60;

    // Create container panel
    ClippingPanel entryPanel = new ClippingPanel();
    entryPanel.BackColor = Color.White;
    entryPanel.Location = new Point(10, yOffset);
    entryPanel.Size = new Size(availableWidth, fixedPanelHeight);
    entryPanel.Name = $"panelEntry_{id}";
    entryPanel.Paint += SnippetEntryPanel_Paint;
    entryPanel.AutoSize = false;

    // Create shortcut label
    Label lblShortcut = new Label();
    lblShortcut.Text = shortcut;
    lblShortcut.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    lblShortcut.ForeColor = Color.FromArgb(45, 45, 48);
    lblShortcut.BackColor = Color.White;
    lblShortcut.Location = new Point(10, 10);
    lblShortcut.AutoSize = false;
    lblShortcut.Size = new Size(availableWidth - 200, 20);
    lblShortcut.Name = $"lblShortcut_{id}";
    lblShortcut.TextAlign = ContentAlignment.MiddleLeft;

    // Create replacement preview label (truncated)
    string displayReplacement = replacement.Length > 60 ? replacement.Substring(0, 57) + "..." : replacement;
    Label lblReplacement = new Label();
    lblReplacement.Text = displayReplacement;
    lblReplacement.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    lblReplacement.ForeColor = Color.FromArgb(100, 100, 100);
    lblReplacement.BackColor = Color.White;
    lblReplacement.Location = new Point(10, 32);
    lblReplacement.AutoSize = false;
    lblReplacement.Size = new Size(availableWidth - 200, 20);
    lblReplacement.Name = $"lblReplacement_{id}";
    lblReplacement.TextAlign = ContentAlignment.MiddleLeft;

    // Create edit button
    Button btnEdit = new Button();
    btnEdit.Text = "✏️";
    btnEdit.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    btnEdit.Size = new Size(30, 30);
    btnEdit.FlatStyle = FlatStyle.Flat;
    btnEdit.FlatAppearance.BorderSize = 0;
    btnEdit.BackColor = Color.FromArgb(245, 245, 245);
    btnEdit.ForeColor = Color.FromArgb(100, 100, 100);
    btnEdit.Cursor = Cursors.Hand;
    btnEdit.Name = $"btnEdit_{id}";
    btnEdit.Location = new Point(entryPanel.Width - 90, 15);
    btnEdit.Tag = id;
    btnEdit.Click += BtnEditSnippet_Click;
    btnEdit.MouseEnter += BtnEditSnippet_MouseEnter;
    btnEdit.MouseLeave += BtnEditSnippet_MouseLeave;

    // Create delete button
    Button btnDelete = new Button();
    btnDelete.Text = "🗑️";
    btnDelete.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    btnDelete.Size = new Size(30, 30);
    btnDelete.FlatStyle = FlatStyle.Flat;
    btnDelete.FlatAppearance.BorderSize = 0;
    btnDelete.BackColor = Color.FromArgb(245, 245, 245);
    btnDelete.ForeColor = Color.FromArgb(100, 100, 100);
    btnDelete.Cursor = Cursors.Hand;
    btnDelete.Name = $"btnDelete_{id}";
    btnDelete.Location = new Point(entryPanel.Width - 50, 15);
    btnDelete.Tag = id;
    btnDelete.Click += BtnDeleteSnippet_Click;
    btnDelete.MouseEnter += BtnDeleteSnippet_MouseEnter;
    btnDelete.MouseLeave += BtnDeleteSnippet_MouseLeave;

    // Add controls to entry panel
    entryPanel.Controls.Add(lblShortcut);
    entryPanel.Controls.Add(lblReplacement);
    entryPanel.Controls.Add(btnEdit);
    entryPanel.Controls.Add(btnDelete);

    // Add entry panel to list
    panelSnippetsList.Controls.Add(entryPanel);
  }

  private void SnippetEntryPanel_Paint(object? sender, PaintEventArgs e)
  {
    if (sender is Panel panel)
    {
      using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
      {
        e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
      }
    }
  }

  private void BtnAddNewSnippet_Click(object? sender, EventArgs e)
  {
    ShowSnippetTaskDialog(isAdding: true);
  }

  private void BtnAddNewSnippet_MouseEnter(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(35, 35, 38);
    }
  }

  private void BtnAddNewSnippet_MouseLeave(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(45, 45, 48);
    }
  }

  private void BtnEditSnippet_Click(object? sender, EventArgs e)
  {
    if (sender is Button btn && btn.Tag is int id)
    {
      // Find the snippet for this entry
      if (databaseService == null)
        return;

      var snippets = databaseService.GetSnippets(username);
      var snippet = snippets.FirstOrDefault(s => s.id == id);

      if (snippet.id == 0)
      {
        MessageBox.Show("Snippet not found.", "Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }

      editingSnippetId = id;
      ShowSnippetTaskDialog(isAdding: false, shortcut: snippet.shortcut, replacement: snippet.replacement);
    }
  }

  private void ShowSnippetTaskDialog(bool isAdding, string shortcut = "", string replacement = "")
  {
    if (databaseService == null)
      return;

    // Create backdrop overlay Form (separate window)
    Form backdrop = new Form();
    backdrop.FormBorderStyle = FormBorderStyle.None;
    backdrop.WindowState = FormWindowState.Normal;
    backdrop.StartPosition = FormStartPosition.Manual;
    backdrop.Size = this.Size;
    backdrop.Location = this.Location;
    backdrop.BackColor = Color.Black;
    backdrop.Opacity = 0.5;
    backdrop.ShowInTaskbar = false;
    backdrop.TopMost = true;
    backdrop.Enabled = true;

    // Create a Form-based dialog styled like TaskDialog
    Form dialog = new Form();
    dialog.Text = "";
    dialog.FormBorderStyle = FormBorderStyle.None;
    dialog.Size = new Size(500, 280);
    dialog.StartPosition = FormStartPosition.CenterParent;
    dialog.BackColor = Color.White;
    dialog.ShowInTaskbar = false;
    dialog.TopMost = true;
    
    // Set backdrop click handler to close dialog
    backdrop.Click += (s, ev) => dialog.DialogResult = DialogResult.Cancel;

    // Show backdrop first, then dialog
    backdrop.Show();
    backdrop.BringToFront();
    dialog.BringToFront();

    // Title label
    Label lblTitle = new Label();
    lblTitle.Text = isAdding ? "Add new snippet" : "Edit snippet";
    lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
    lblTitle.ForeColor = Color.FromArgb(45, 45, 48);
    lblTitle.Location = new Point(20, 20);
    lblTitle.AutoSize = true;

    // Close button
    Button btnClose = new Button();
    btnClose.Text = "×";
    btnClose.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point);
    btnClose.FlatStyle = FlatStyle.Flat;
    btnClose.FlatAppearance.BorderSize = 0;
    btnClose.BackColor = Color.Transparent;
    btnClose.ForeColor = Color.FromArgb(100, 100, 100);
    btnClose.Size = new Size(30, 30);
    btnClose.Location = new Point(460, 10);
    btnClose.Cursor = Cursors.Hand;
    btnClose.TextAlign = ContentAlignment.MiddleCenter;
    btnClose.Click += (s, ev) => dialog.DialogResult = DialogResult.Cancel;
    btnClose.MouseEnter += (s, ev) => { btnClose.ForeColor = Color.FromArgb(45, 45, 48); btnClose.BackColor = Color.FromArgb(245, 245, 245); };
    btnClose.MouseLeave += (s, ev) => { btnClose.ForeColor = Color.FromArgb(100, 100, 100); btnClose.BackColor = Color.Transparent; };

    // Shortcut label (aligned with text input area inside TextBox)
    Label lblShortcutLabel = new Label();
    lblShortcutLabel.Text = "Shortcut word:";
    lblShortcutLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    lblShortcutLabel.ForeColor = Color.FromArgb(45, 45, 48);
    lblShortcutLabel.Location = new Point(17, 60); // Aligned with text input area (20 + 3px border/padding)
    lblShortcutLabel.AutoSize = true;

    // Shortcut input
    TextBox txtShortcut = new TextBox();
    txtShortcut.Location = new Point(20, 80);
    txtShortcut.Size = new Size(460, 25);
    txtShortcut.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    txtShortcut.BorderStyle = BorderStyle.FixedSingle;
    txtShortcut.Text = shortcut;
    if (isAdding)
      txtShortcut.SelectAll();

    // Replacement label (aligned with text input area inside TextBox)
    Label lblReplacementLabel = new Label();
    lblReplacementLabel.Text = "Replacement text:";
    lblReplacementLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    lblReplacementLabel.ForeColor = Color.FromArgb(45, 45, 48);
    lblReplacementLabel.Location = new Point(17, 115); // Aligned with text input area (20 + 3px border/padding)
    lblReplacementLabel.AutoSize = true;

    // Replacement input (multiline for longer text)
    TextBox txtReplacement = new TextBox();
    txtReplacement.Location = new Point(20, 135);
    txtReplacement.Size = new Size(460, 60);
    txtReplacement.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    txtReplacement.BorderStyle = BorderStyle.FixedSingle;
    txtReplacement.Multiline = true;
    txtReplacement.Text = replacement;
    txtReplacement.ScrollBars = ScrollBars.None; // Hide scrollbar

    // Cancel button
    Button btnCancel = new Button();
    btnCancel.Text = "Cancel";
    btnCancel.Location = new Point(290, 210);
    btnCancel.Size = new Size(80, 28);
    btnCancel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    btnCancel.FlatStyle = FlatStyle.Flat;
    btnCancel.FlatAppearance.BorderSize = 0;
    btnCancel.BackColor = Color.FromArgb(235, 235, 235);
    btnCancel.ForeColor = Color.FromArgb(45, 45, 48);
    btnCancel.Cursor = Cursors.Hand;
    btnCancel.DialogResult = DialogResult.Cancel;
    btnCancel.TextAlign = ContentAlignment.MiddleCenter;
    btnCancel.MouseEnter += (s, ev) => btnCancel.BackColor = Color.FromArgb(220, 220, 220);
    btnCancel.MouseLeave += (s, ev) => btnCancel.BackColor = Color.FromArgb(235, 235, 235);

    // Save button
    Button btnSave = new Button();
    btnSave.Text = isAdding ? "Add" : "Save changes";
    btnSave.Location = new Point(380, 210);
    btnSave.Size = new Size(100, 28);
    btnSave.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    btnSave.FlatStyle = FlatStyle.Flat;
    btnSave.FlatAppearance.BorderSize = 0;
    btnSave.BackColor = Color.FromArgb(45, 45, 48);
    btnSave.ForeColor = Color.White;
    btnSave.Cursor = Cursors.Hand;
    btnSave.DialogResult = DialogResult.OK;
    btnSave.TextAlign = ContentAlignment.MiddleCenter;
    btnSave.MouseEnter += (s, ev) => btnSave.BackColor = Color.FromArgb(35, 35, 38);
    btnSave.MouseLeave += (s, ev) => btnSave.BackColor = Color.FromArgb(45, 45, 48);

    // Add controls
    dialog.Controls.Add(lblTitle);
    dialog.Controls.Add(btnClose);
    dialog.Controls.Add(lblShortcutLabel);
    dialog.Controls.Add(txtShortcut);
    dialog.Controls.Add(lblReplacementLabel);
    dialog.Controls.Add(txtReplacement);
    dialog.Controls.Add(btnCancel);
    dialog.Controls.Add(btnSave);

    // Draw square border
    dialog.Paint += (s, ev) =>
    {
      using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
      {
        Rectangle rect = new Rectangle(0, 0, dialog.Width - 1, dialog.Height - 1);
        ev.Graphics.DrawRectangle(pen, rect);
      }
    };

    // Set focus and show
    dialog.AcceptButton = btnSave;
    dialog.CancelButton = btnCancel;
    txtShortcut.Focus();

    // Close backdrop when dialog closes
    dialog.FormClosed += (s, ev) => 
    {
      backdrop.Close();
      backdrop.Dispose();
    };

    if (dialog.ShowDialog(this) == DialogResult.OK)
    {
      string shortcutText = txtShortcut.Text.Trim();
      string replacementText = txtReplacement.Text.Trim();

      if (string.IsNullOrWhiteSpace(shortcutText) || string.IsNullOrWhiteSpace(replacementText))
      {
        MessageBox.Show("Please enter both shortcut and replacement text.", "Validation Error",
          MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      try
      {
        if (isAdding)
        {
          databaseService.AddSnippet(username, shortcutText, replacementText);
        }
        else
        {
          if (!editingSnippetId.HasValue)
          {
            MessageBox.Show("No snippet selected for editing.", "Error",
              MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
          }
          databaseService.UpdateSnippet(editingSnippetId.Value, username, shortcutText, replacementText);
        }

        RefreshSnippetsList();
      }
      catch (InvalidOperationException ex)
      {
        MessageBox.Show(ex.Message, "Error",
          MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
      catch (Exception ex)
      {
        string action = isAdding ? "add" : "update";
        MessageBox.Show($"Failed to {action} snippet: {ex.Message}", "Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }
  }

  private void BtnEditSnippet_MouseEnter(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(230, 230, 230);
    }
  }

  private void BtnEditSnippet_MouseLeave(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(245, 245, 245);
    }
  }

  private void BtnDeleteSnippet_Click(object? sender, EventArgs e)
  {
    if (sender is Button btn && btn.Tag is int id && databaseService != null)
    {
      // Find the snippet for confirmation message
      var snippets = databaseService.GetSnippets(username);
      var snippet = snippets.FirstOrDefault(s => s.id == id);

      string shortcutText = snippet.id == id ? snippet.shortcut : "this snippet";

      DialogResult result = MessageBox.Show(
        $"Are you sure you want to delete \"{shortcutText}\"?",
        "Confirm Delete",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question);

      if (result == DialogResult.Yes)
      {
        try
        {
          databaseService.DeleteSnippet(id, username);
          RefreshSnippetsList();
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Failed to delete snippet: {ex.Message}", "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
      }
    }
  }

  private void BtnDeleteSnippet_MouseEnter(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(230, 230, 230);
    }
  }

  private void BtnDeleteSnippet_MouseLeave(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(245, 245, 245);
    }
  }

  private void RefreshSnippetsList()
  {
    LoadSnippetsEntries();
  }

  // Style Page Methods

  private void InitializeStylePage()
  {
    panelStylePage.BackColor = Color.White;
    panelStylePage.Dock = DockStyle.Fill;
    panelStylePage.Padding = new Padding(40, 60, 40, 50);

    // Page Title
    lblStyleTitle = new Label();
    lblStyleTitle.Text = "Style";
    lblStyleTitle.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point);
    lblStyleTitle.ForeColor = Color.FromArgb(45, 45, 48);
    lblStyleTitle.Location = new Point(40, 60);
    lblStyleTitle.AutoSize = true;
    lblStyleTitle.Name = "lblStyleTitle";

    // Load saved preference
    selectedStylePreference = databaseService?.GetUserStylePreference(username) ?? "formal";

    // Create style cards
    CreateStyleCards();

    // Add controls to style page
    panelStylePage.Controls.Add(lblStyleTitle);

    // Handle resize
    panelStylePage.Resize += PanelStylePage_Resize;
  }

  private void PanelStylePage_Resize(object? sender, EventArgs e)
  {
    if (panelStylePage != null)
    {
      UpdateCardPositions();
    }
  }

  private void CreateStyleCards()
  {
    // Card dimensions
    const int cardWidth = 280;
    const int cardHeight = 420;
    const int cardSpacing = 30;
    const int cardsTop = 150;

    // Calculate starting X position to center cards
    int totalCardsWidth = (cardWidth * 3) + (cardSpacing * 2);
    int startX = (panelStylePage.ClientSize.Width - totalCardsWidth) / 2;
    if (startX < 40) startX = 40; // Minimum padding

    // Create Formal card
    panelFormalCard = CreateStyleCard(
      "Formal.",
      "Caps + Punctuation",
      "Hey, are you free for lunch tomorrow? Let's do 12 if that works for you.",
      "formal",
      new Point(startX, cardsTop),
      cardWidth,
      cardHeight
    );

    // Create Casual card
    panelCasualCard = CreateStyleCard(
      "Casual",
      "Caps + Less punctuation",
      "Hey are you free for lunch tomorrow? Let's do 12 if that works for you",
      "casual",
      new Point(startX + cardWidth + cardSpacing, cardsTop),
      cardWidth,
      cardHeight
    );

    // Create Very Casual card
    panelVeryCasualCard = CreateStyleCard(
      "very casual",
      "No Caps + Less punctuation",
      "hey are you free for lunch tomorrow? let's do 12 if that works for you",
      "very_casual",
      new Point(startX + (cardWidth + cardSpacing) * 2, cardsTop),
      cardWidth,
      cardHeight
    );

    // Add cards to page
    panelStylePage.Controls.Add(panelFormalCard);
    panelStylePage.Controls.Add(panelCasualCard);
    panelStylePage.Controls.Add(panelVeryCasualCard);

    // Highlight selected card
    UpdateCardSelection();
  }

  private ClippingPanel CreateStyleCard(string title, string subtitle, string exampleText, string styleValue, Point location, int width, int height)
  {
    // Create card panel
    ClippingPanel card = new ClippingPanel();
    card.BackColor = Color.White;
    card.Location = location;
    card.Size = new Size(width, height);
    card.Name = $"panel{styleValue}Card";
    card.Cursor = Cursors.Hand;
    card.Tag = styleValue;
    
    // Add border paint handler
    card.Paint += StyleCard_Paint;
    
    // Add click handler
    card.Click += StyleCard_Click;
    card.MouseEnter += StyleCard_MouseEnter;
    card.MouseLeave += StyleCard_MouseLeave;

    // Card padding
    const int padding = 20;

    // Title label
    Label lblTitle = new Label();
    lblTitle.Text = title;
    lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
    lblTitle.ForeColor = Color.FromArgb(45, 45, 48);
    lblTitle.BackColor = Color.White;
    lblTitle.Location = new Point(padding, padding);
    lblTitle.AutoSize = true;
    lblTitle.Name = $"lbl{styleValue}Title";
    lblTitle.Cursor = Cursors.Hand;
    lblTitle.Tag = styleValue; // Store style value for easy lookup
    lblTitle.Click += StyleCard_Click;

    // Subtitle label
    Label lblSubtitle = new Label();
    lblSubtitle.Text = subtitle;
    lblSubtitle.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    lblSubtitle.ForeColor = Color.FromArgb(100, 100, 100);
    lblSubtitle.BackColor = Color.White;
    lblSubtitle.Location = new Point(padding, padding + 35);
    lblSubtitle.AutoSize = true;
    lblSubtitle.Name = $"lbl{styleValue}Subtitle";
    lblSubtitle.Cursor = Cursors.Hand;
    lblSubtitle.Tag = styleValue; // Store style value for easy lookup
    lblSubtitle.Click += StyleCard_Click;

    // Message bubble with example text
    Panel messageBubble = CreateMessageBubble(exampleText, padding, padding + 70, width - (padding * 2), height - padding - (padding + 70), styleValue);
    messageBubble.Name = $"panel{styleValue}MessageBubble";
    messageBubble.Cursor = Cursors.Hand;
    messageBubble.Tag = styleValue;
    messageBubble.Click += StyleCard_Click;

    // Add controls to card
    card.Controls.Add(lblTitle);
    card.Controls.Add(lblSubtitle);
    card.Controls.Add(messageBubble);

    return card;
  }

  private Panel CreateMessageBubble(string text, int x, int y, int width, int height, string styleValue)
  {
    // Create message bubble panel
    Panel bubble = new Panel();
    bubble.BackColor = Color.FromArgb(245, 245, 245);
    bubble.Location = new Point(x, y);
    bubble.Size = new Size(width, height);
    bubble.Padding = new Padding(12, 12, 12, 12);

    // Example text label
    Label lblText = new Label();
    lblText.Text = text;
    lblText.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    lblText.ForeColor = Color.FromArgb(45, 45, 48);
    lblText.BackColor = Color.FromArgb(245, 245, 245);
    lblText.Location = new Point(12, 12);
    lblText.AutoSize = false;
    lblText.Size = new Size(width - 24, height - 24); // Account for padding only
    lblText.TextAlign = ContentAlignment.TopLeft;
    lblText.UseCompatibleTextRendering = false;
    lblText.Cursor = Cursors.Hand;
    lblText.Tag = styleValue; // Store style value for easy lookup
    lblText.Click += StyleCard_Click; // Add click handler to text label too

    // Add controls to bubble
    bubble.Controls.Add(lblText);

    return bubble;
  }


  private void StyleCard_Paint(object? sender, PaintEventArgs e)
  {
    if (sender is ClippingPanel panel && panel.Tag is string styleValue)
    {
      Color borderColor = (selectedStylePreference == styleValue)
        ? Color.Black // Black for selected
        : Color.FromArgb(200, 200, 200); // Light gray for unselected

      // Draw square border (no rounding) with uniform thickness
      using (Pen pen = new Pen(borderColor, 1))
      {
        Rectangle rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
        e.Graphics.DrawRectangle(pen, rect);
      }
    }
  }

  private void StyleCard_Click(object? sender, EventArgs e)
  {
    string? styleValue = null;
    
    // Get style value from sender or find parent card
    if (sender is ClippingPanel card && card.Tag is string cardValue)
    {
      styleValue = cardValue;
    }
    else if (sender is Panel panel && panel.Tag is string panelValue)
    {
      styleValue = panelValue;
    }
    else if (sender is Label label && label.Tag is string labelValue)
    {
      styleValue = labelValue;
    }
    else if (sender is Control control)
    {
      // Find parent card (ClippingPanel) by traversing up the control tree
      Control? parent = control.Parent;
      while (parent != null)
      {
        if (parent is ClippingPanel parentCard && parentCard.Tag is string parentValue)
        {
          styleValue = parentValue;
          break;
        }
        // Also check if parent is a Panel with Tag
        if (parent is Panel parentPanel && parentPanel.Tag is string panelTagValue)
        {
          styleValue = panelTagValue;
          break;
        }
        parent = parent.Parent;
      }
    }
    
    if (styleValue != null)
    {
      selectedStylePreference = styleValue;
      
      // Save to database
      try
      {
        if (databaseService != null)
        {
          databaseService.SaveUserStylePreference(username, styleValue);
          System.Diagnostics.Debug.WriteLine($"Style preference saved: {styleValue} for user {username}");
        }
        else
        {
          System.Diagnostics.Debug.WriteLine("DatabaseService is null!");
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Failed to save style preference: {ex.Message}");
        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        MessageBox.Show($"Failed to save style preference: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }

      // Update visual selection
      UpdateCardSelection();
    }
    else
    {
      System.Diagnostics.Debug.WriteLine($"StyleCard_Click: Could not determine style value from sender. Sender type: {sender?.GetType().Name}");
    }
  }

  private void StyleCard_MouseEnter(object? sender, EventArgs e)
  {
    if (sender is Panel card)
    {
      card.BackColor = Color.FromArgb(250, 250, 250);
    }
  }

  private void StyleCard_MouseLeave(object? sender, EventArgs e)
  {
    if (sender is Panel card)
    {
      card.BackColor = Color.White;
    }
  }

  private void UpdateCardSelection()
  {
    // Refresh all cards to update border colors
    if (panelFormalCard != null)
    {
      panelFormalCard.Invalidate();
      panelFormalCard.Refresh(); // Use Refresh instead of Update for immediate repaint
    }
    if (panelCasualCard != null)
    {
      panelCasualCard.Invalidate();
      panelCasualCard.Refresh();
    }
    if (panelVeryCasualCard != null)
    {
      panelVeryCasualCard.Invalidate();
      panelVeryCasualCard.Refresh();
    }
    
    System.Diagnostics.Debug.WriteLine($"UpdateCardSelection called. Selected: {selectedStylePreference}");
  }

  private void UpdateCardPositions()
  {
    if (panelFormalCard == null || panelCasualCard == null || panelVeryCasualCard == null)
      return;

    const int cardWidth = 280;
    const int cardSpacing = 30;
    const int cardsTop = 150;

    // Calculate starting X position to center cards
    int totalCardsWidth = (cardWidth * 3) + (cardSpacing * 2);
    int startX = (panelStylePage.ClientSize.Width - totalCardsWidth) / 2;
    if (startX < 40) startX = 40; // Minimum padding

    panelFormalCard.Location = new Point(startX, cardsTop);
    panelCasualCard.Location = new Point(startX + cardWidth + cardSpacing, cardsTop);
    panelVeryCasualCard.Location = new Point(startX + (cardWidth + cardSpacing) * 2, cardsTop);
  }

  // Settings Page Methods

  private void InitializeSettingsPage()
  {
    panelSettingsPage.BackColor = Color.White;
    panelSettingsPage.Dock = DockStyle.Fill;
    panelSettingsPage.Padding = new Padding(40, 60, 40, 50);

    // Page Title
    lblSettingsTitle = new Label();
    lblSettingsTitle.Text = "Settings";
    lblSettingsTitle.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point);
    lblSettingsTitle.ForeColor = Color.FromArgb(45, 45, 48);
    lblSettingsTitle.Location = new Point(40, 60);
    lblSettingsTitle.AutoSize = true;
    lblSettingsTitle.Name = "lblSettingsTitle";

    // Hotkey Section Title
    lblHotkeySectionTitle = new Label();
    lblHotkeySectionTitle.Text = "Push-to-Talk Shortcut";
    lblHotkeySectionTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
    lblHotkeySectionTitle.ForeColor = Color.FromArgb(45, 45, 48);
    lblHotkeySectionTitle.Location = new Point(40, 130);
    lblHotkeySectionTitle.AutoSize = true;
    lblHotkeySectionTitle.Name = "lblHotkeySectionTitle";

    // Current Hotkey Label
    lblCurrentHotkey = new Label();
    lblCurrentHotkey.Text = "Current shortcut:";
    lblCurrentHotkey.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    lblCurrentHotkey.ForeColor = Color.FromArgb(100, 100, 100);
    lblCurrentHotkey.Location = new Point(40, 170);
    lblCurrentHotkey.AutoSize = true;
    lblCurrentHotkey.Name = "lblCurrentHotkey";

    // Current Hotkey Value
    lblCurrentHotkeyValue = new Label();
    lblCurrentHotkeyValue.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    lblCurrentHotkeyValue.ForeColor = Color.FromArgb(45, 45, 48);
    lblCurrentHotkeyValue.Location = new Point(180, 170);
    lblCurrentHotkeyValue.AutoSize = true;
    lblCurrentHotkeyValue.Name = "lblCurrentHotkeyValue";

    // Change Hotkey Button
    btnChangeHotkey = new Button();
    btnChangeHotkey.Text = "Change shortcut";
    btnChangeHotkey.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    btnChangeHotkey.FlatStyle = FlatStyle.Flat;
    btnChangeHotkey.FlatAppearance.BorderSize = 0;
    btnChangeHotkey.BackColor = Color.FromArgb(45, 45, 48);
    btnChangeHotkey.ForeColor = Color.White;
    btnChangeHotkey.Cursor = Cursors.Hand;
    btnChangeHotkey.Size = new Size(140, 35);
    btnChangeHotkey.Location = new Point(40, 210);
    btnChangeHotkey.Name = "btnChangeHotkey";
    btnChangeHotkey.Click += BtnChangeHotkey_Click;
    btnChangeHotkey.MouseEnter += BtnChangeHotkey_MouseEnter;
    btnChangeHotkey.MouseLeave += BtnChangeHotkey_MouseLeave;

    // Description Label
    lblHotkeyDescription = new Label();
    lblHotkeyDescription.Text = "Hold the shortcut keys to start dictating. Release to stop and inject text.";
    lblHotkeyDescription.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    lblHotkeyDescription.ForeColor = Color.FromArgb(100, 100, 100);
    lblHotkeyDescription.Location = new Point(40, 260);
    lblHotkeyDescription.AutoSize = true;
    lblHotkeyDescription.Name = "lblHotkeyDescription";
    lblHotkeyDescription.MaximumSize = new Size(600, 0);

    // Add controls to settings page
    panelSettingsPage.Controls.Add(lblSettingsTitle);
    panelSettingsPage.Controls.Add(lblHotkeySectionTitle);
    panelSettingsPage.Controls.Add(lblCurrentHotkey);
    panelSettingsPage.Controls.Add(lblCurrentHotkeyValue);
    panelSettingsPage.Controls.Add(btnChangeHotkey);
    panelSettingsPage.Controls.Add(lblHotkeyDescription);

    // Load and display current hotkey
    LoadAndDisplayHotkey();

    // Handle resize
    panelSettingsPage.Resize += PanelSettingsPage_Resize;
  }

  private void PanelSettingsPage_Resize(object? sender, EventArgs e)
  {
    // Settings page layout is simple and doesn't need complex resize logic
    // But we can ensure description label doesn't overflow
    if (lblHotkeyDescription != null && panelSettingsPage != null)
    {
      int maxWidth = panelSettingsPage.ClientSize.Width - 80; // Account for padding
      lblHotkeyDescription.MaximumSize = new Size(maxWidth, 0);
    }
  }

  private void LoadAndDisplayHotkey()
  {
    if (databaseService == null || lblCurrentHotkeyValue == null)
      return;

    try
    {
      var (ctrl, alt, shift, win, keyCode) = databaseService.GetUserHotkeyPreference(username);
      string hotkeyDisplay = FormatHotkeyDisplay(ctrl, alt, shift, win, keyCode);
      lblCurrentHotkeyValue.Text = hotkeyDisplay;
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Failed to load hotkey preference: {ex.Message}");
      lblCurrentHotkeyValue.Text = "Ctrl + Win";
    }
  }

  private string FormatHotkeyDisplay(bool ctrl, bool alt, bool shift, bool win, int? keyCode)
  {
    var parts = new List<string>();
    if (ctrl) parts.Add("Ctrl");
    if (alt) parts.Add("Alt");
    if (shift) parts.Add("Shift");
    if (win) parts.Add("Win");
    if (keyCode.HasValue)
    {
      // Convert key code to readable name
      string keyName = GetKeyName(keyCode.Value);
      if (!string.IsNullOrEmpty(keyName))
        parts.Add(keyName);
    }
    return string.Join(" + ", parts);
  }

  private string GetKeyName(int keyCode)
  {
    // Common key codes
    return keyCode switch
    {
      0x20 => "Space",
      0x0D => "Enter",
      0x1B => "Esc",
      0x08 => "Backspace",
      0x09 => "Tab",
      >= 0x30 and <= 0x39 => ((char)keyCode).ToString(), // 0-9
      >= 0x41 and <= 0x5A => ((char)keyCode).ToString(), // A-Z
      _ => $"Key{keyCode}"
    };
  }

  private void BtnChangeHotkey_Click(object? sender, EventArgs e)
  {
    ShowHotkeyCaptureDialog();
  }

  private void BtnChangeHotkey_MouseEnter(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(35, 35, 38);
    }
  }

  private void BtnChangeHotkey_MouseLeave(object? sender, EventArgs e)
  {
    if (sender is Button btn)
    {
      btn.BackColor = Color.FromArgb(45, 45, 48);
    }
  }

  private void ShowHotkeyCaptureDialog()
  {
    if (databaseService == null)
      return;

    // Create backdrop overlay Form (separate window)
    Form backdrop = new Form();
    backdrop.FormBorderStyle = FormBorderStyle.None;
    backdrop.WindowState = FormWindowState.Normal;
    backdrop.StartPosition = FormStartPosition.Manual;
    backdrop.Size = this.Size;
    backdrop.Location = this.Location;
    backdrop.BackColor = Color.Black;
    backdrop.Opacity = 0.5;
    backdrop.ShowInTaskbar = false;
    backdrop.TopMost = true;
    backdrop.Enabled = true;

    // Create a Form-based dialog styled like TaskDialog
    Form dialog = new Form();
    dialog.Text = "";
    dialog.FormBorderStyle = FormBorderStyle.None;
    dialog.Size = new Size(500, 250);
    dialog.StartPosition = FormStartPosition.CenterParent;
    dialog.BackColor = Color.White;
    dialog.ShowInTaskbar = false;
    dialog.TopMost = true;
    
    // Set backdrop click handler to close dialog
    backdrop.Click += (s, e) => dialog.DialogResult = DialogResult.Cancel;

    // Show backdrop first, then dialog
    backdrop.Show();
    backdrop.BringToFront();
    dialog.BringToFront();

    // Title label
    Label lblTitle = new Label();
    lblTitle.Text = "Change Push-to-Talk Shortcut";
    lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
    lblTitle.ForeColor = Color.FromArgb(45, 45, 48);
    lblTitle.Location = new Point(20, 20);
    lblTitle.AutoSize = true;

    // Close button
    Button btnClose = new Button();
    btnClose.Text = "×";
    btnClose.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point);
    btnClose.FlatStyle = FlatStyle.Flat;
    btnClose.FlatAppearance.BorderSize = 0;
    btnClose.BackColor = Color.Transparent;
    btnClose.ForeColor = Color.FromArgb(100, 100, 100);
    btnClose.Size = new Size(30, 30);
    btnClose.Location = new Point(460, 10);
    btnClose.Cursor = Cursors.Hand;
    btnClose.TextAlign = ContentAlignment.MiddleCenter;
    btnClose.Click += (s, e) => dialog.DialogResult = DialogResult.Cancel;
    btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.FromArgb(45, 45, 48); btnClose.BackColor = Color.FromArgb(245, 245, 245); };
    btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = Color.FromArgb(100, 100, 100); btnClose.BackColor = Color.Transparent; };

    // Instructions label
    Label lblInstructions = new Label();
    lblInstructions.Text = "Press the key combination you want to use:";
    lblInstructions.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    lblInstructions.ForeColor = Color.FromArgb(100, 100, 100);
    lblInstructions.Location = new Point(20, 60);
    lblInstructions.AutoSize = true;

    // Display area for captured keys
    Label lblHotkeyDisplay = new Label();
    lblHotkeyDisplay.Text = "Press keys...";
    lblHotkeyDisplay.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
    lblHotkeyDisplay.ForeColor = Color.FromArgb(45, 45, 48);
    lblHotkeyDisplay.Location = new Point(20, 90);
    lblHotkeyDisplay.AutoSize = true;
    lblHotkeyDisplay.Size = new Size(460, 30);
    lblHotkeyDisplay.TextAlign = ContentAlignment.MiddleLeft;

    // Warning label
    Label lblWarning = new Label();
    lblWarning.Text = "At least one modifier key (Ctrl, Alt, Shift, or Win) must be pressed.";
    lblWarning.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    lblWarning.ForeColor = Color.FromArgb(100, 100, 100);
    lblWarning.Location = new Point(20, 130);
    lblWarning.AutoSize = true;
    lblWarning.MaximumSize = new Size(460, 0);

    // Cancel button
    Button btnCancel = new Button();
    btnCancel.Text = "Cancel";
    btnCancel.Location = new Point(290, 180);
    btnCancel.Size = new Size(80, 28);
    btnCancel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    btnCancel.FlatStyle = FlatStyle.Flat;
    btnCancel.FlatAppearance.BorderSize = 0;
    btnCancel.BackColor = Color.FromArgb(235, 235, 235);
    btnCancel.ForeColor = Color.FromArgb(45, 45, 48);
    btnCancel.Cursor = Cursors.Hand;
    btnCancel.DialogResult = DialogResult.Cancel;
    btnCancel.TextAlign = ContentAlignment.MiddleCenter;
    btnCancel.MouseEnter += (s, e) => btnCancel.BackColor = Color.FromArgb(220, 220, 220);
    btnCancel.MouseLeave += (s, e) => btnCancel.BackColor = Color.FromArgb(235, 235, 235);

    // Save button
    Button btnSave = new Button();
    btnSave.Text = "Save";
    btnSave.Location = new Point(380, 180);
    btnSave.Size = new Size(100, 28);
    btnSave.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    btnSave.FlatStyle = FlatStyle.Flat;
    btnSave.FlatAppearance.BorderSize = 0;
    btnSave.BackColor = Color.FromArgb(45, 45, 48);
    btnSave.ForeColor = Color.White;
    btnSave.Cursor = Cursors.Hand;
    btnSave.DialogResult = DialogResult.OK;
    btnSave.TextAlign = ContentAlignment.MiddleCenter;
    btnSave.MouseEnter += (s, e) => btnSave.BackColor = Color.FromArgb(35, 35, 38);
    btnSave.MouseLeave += (s, e) => btnSave.BackColor = Color.FromArgb(45, 45, 48);
    btnSave.Enabled = false; // Disabled until valid combination is captured

    // Add controls
    dialog.Controls.Add(lblTitle);
    dialog.Controls.Add(btnClose);
    dialog.Controls.Add(lblInstructions);
    dialog.Controls.Add(lblHotkeyDisplay);
    dialog.Controls.Add(lblWarning);
    dialog.Controls.Add(btnCancel);
    dialog.Controls.Add(btnSave);

    // Draw square border
    dialog.Paint += (s, e) =>
    {
      using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
      {
        Rectangle rect = new Rectangle(0, 0, dialog.Width - 1, dialog.Height - 1);
        e.Graphics.DrawRectangle(pen, rect);
      }
    };

    // Key capture variables
    bool ctrlPressed = false;
    bool altPressed = false;
    bool shiftPressed = false;
    bool winPressed = false;
    int? capturedKeyCode = null;

    // Timer to poll key states (for Win key detection)
    System.Windows.Forms.Timer keyPollTimer = new System.Windows.Forms.Timer();
    keyPollTimer.Interval = 50; // Check every 50ms
    keyPollTimer.Tick += (s, e) =>
    {
      // Use GetAsyncKeyState for accurate key detection (especially Win key)
      ctrlPressed = (WindowsApiHelper.GetAsyncKeyState(0xA2) & 0x8000) != 0 || 
                    (WindowsApiHelper.GetAsyncKeyState(0xA3) & 0x8000) != 0;
      altPressed = (WindowsApiHelper.GetAsyncKeyState(0xA4) & 0x8000) != 0 || 
                   (WindowsApiHelper.GetAsyncKeyState(0xA5) & 0x8000) != 0;
      shiftPressed = (WindowsApiHelper.GetAsyncKeyState(0xA0) & 0x8000) != 0 || 
                     (WindowsApiHelper.GetAsyncKeyState(0xA1) & 0x8000) != 0;
      winPressed = (WindowsApiHelper.GetAsyncKeyState(0x5B) & 0x8000) != 0 || 
                   (WindowsApiHelper.GetAsyncKeyState(0x5C) & 0x8000) != 0;

      UpdateHotkeyDisplay(lblHotkeyDisplay, ctrlPressed, altPressed, shiftPressed, winPressed, capturedKeyCode);
      
      bool hasModifier = ctrlPressed || altPressed || shiftPressed || winPressed;
      btnSave.Enabled = hasModifier;
    };
    keyPollTimer.Start();

    // Key capture logic
    dialog.KeyPreview = true;
    dialog.KeyDown += (s, e) =>
    {
      // Get the main key (if not a modifier)
      if (e.KeyCode != Keys.ControlKey && e.KeyCode != Keys.Menu && 
          e.KeyCode != Keys.ShiftKey && e.KeyCode != Keys.LWin && e.KeyCode != Keys.RWin)
      {
        capturedKeyCode = (int)e.KeyCode;
      }
      
      e.Handled = true;
    };

    dialog.KeyUp += (s, e) =>
    {
      // Clear captured key code if it was released
      if (capturedKeyCode.HasValue && (int)e.KeyCode == capturedKeyCode.Value)
      {
        // Keep it for now - user might want to use it
      }
      e.Handled = true;
    };

    // Set focus and show
    dialog.CancelButton = btnCancel;
    dialog.Focus();

    // Close backdrop when dialog closes
    dialog.FormClosed += (s, e) => 
    {
      keyPollTimer.Stop();
      keyPollTimer.Dispose();
      backdrop.Close();
      backdrop.Dispose();
    };

    if (dialog.ShowDialog(this) == DialogResult.OK)
    {
      // Validate that at least one modifier is pressed
      if (!ctrlPressed && !altPressed && !shiftPressed && !winPressed)
      {
        MessageBox.Show("At least one modifier key (Ctrl, Alt, Shift, or Win) must be selected.", "Validation Error",
          MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      try
      {
        // Save to database
        databaseService.SaveUserHotkeyPreference(username, ctrlPressed, altPressed, shiftPressed, winPressed, capturedKeyCode);
        
        // Update GlobalHotkeyManager
        if (hotkeyManager != null)
        {
          hotkeyManager.SetHotkeyConfiguration(ctrlPressed, altPressed, shiftPressed, winPressed, capturedKeyCode);
        }
        
        // Refresh display
        LoadAndDisplayHotkey();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Failed to save hotkey preference: {ex.Message}", "Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }
  }

  private void UpdateHotkeyDisplay(Label displayLabel, bool ctrl, bool alt, bool shift, bool win, int? keyCode)
  {
    var parts = new List<string>();
    if (ctrl) parts.Add("Ctrl");
    if (alt) parts.Add("Alt");
    if (shift) parts.Add("Shift");
    if (win) parts.Add("Win");
    if (keyCode.HasValue)
    {
      string keyName = GetKeyName(keyCode.Value);
      if (!string.IsNullOrEmpty(keyName))
        parts.Add(keyName);
    }

    if (parts.Count == 0)
    {
      displayLabel.Text = "Press keys...";
      displayLabel.ForeColor = Color.FromArgb(100, 100, 100);
    }
    else
    {
      displayLabel.Text = string.Join(" + ", parts);
      displayLabel.ForeColor = Color.FromArgb(45, 45, 48);
    }
  }
}
