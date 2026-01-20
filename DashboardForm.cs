using Microsoft.Data.SqlClient;
using System.Runtime.InteropServices;

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

  public DashboardForm(string username)
  {
    this.username = username;
    InitializeComponent();
    SetupWindowControls();
    
    // Set custom icon from assets folder
    string iconPath = Path.Combine(Application.StartupPath, "assets", "cp-black.ico");
    if (File.Exists(iconPath))
    {
      this.Icon = new Icon(iconPath);
    }
    
    databaseService = new DatabaseService();
    LoadDashboardData();
    InitializeLoadingUI();
    InitializeSpeechServicesAsync();
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

  protected override void WndProc(ref Message m)
  {
    // Handle hotkey messages
    if (m.Msg == WindowsApiHelper.WM_HOTKEY)
    {
      hotkeyManager?.ProcessMessage(m.Msg, m.WParam, m.LParam);
    }
    base.WndProc(ref m);
  }

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
      
      // Inject recognized text if available
      if (!string.IsNullOrEmpty(recognizedText))
      {
        textInjectionService.InjectText(recognizedText);
        SaveSpeechToDatabase(recognizedText);
        AddSpeechToHistory(recognizedText);
      }

      overlayForm.Hide();
      recognizedText = null;
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

  private Point mouseOffset;
  private bool isDragging = false;

  private void SetupWindowControls()
  {
    // Enable window dragging
    this.MouseDown += DashboardForm_MouseDown;
    this.MouseMove += DashboardForm_MouseMove;
    this.MouseUp += DashboardForm_MouseUp;
  }

  private void DashboardForm_MouseDown(object? sender, MouseEventArgs e)
  {
    // Only allow dragging from the top area (title bar area)
    if (e.Button == MouseButtons.Left && e.Y <= 40)
    {
      mouseOffset = new Point(e.X, e.Y);
      isDragging = true;
    }
  }

  private void DashboardForm_MouseMove(object? sender, MouseEventArgs e)
  {
    if (isDragging)
    {
      Point mousePos = MousePosition;
      mousePos.Offset(-mouseOffset.X, -mouseOffset.Y);
      this.Location = mousePos;
    }
  }

  private void DashboardForm_MouseUp(object? sender, MouseEventArgs e)
  {
    isDragging = false;
  }

  private void btnClose_Click(object? sender, EventArgs e)
  {
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
  }

  private void LoadSpeechHistory()
  {
    if (databaseService == null)
      return;

    var speeches = databaseService.GetSpeeches(username);

    int yOffset = 10;
    foreach (var speech in speeches)
    {
      // Create a temporary label to measure text height
      Label tempLabel = new Label();
      tempLabel.Text = speech.text;
      tempLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
      tempLabel.AutoSize = true;
      tempLabel.MaximumSize = new Size(panelSpeechHistory.Width - 80, 0);
      
      CreateSpeechRow(speech.id, speech.time, speech.text, yOffset);
      
      // Calculate next position based on text height
      yOffset += 20 + tempLabel.Height + 20; // timestamp + text + spacing
      
      tempLabel.Dispose();
    }
  }

  // Sidebar navigation hover effects (non-functional for now)
  private void navItem_MouseEnter(object? sender, EventArgs e)
  {
    if (sender is Label label)
    {
      label.BackColor = Color.FromArgb(245, 245, 245);
    }
  }

  private void navItem_MouseLeave(object? sender, EventArgs e)
  {
    if (sender is Label label && label != lblNavHome)
    {
      label.BackColor = Color.Transparent;
    }
  }

  private void SaveSpeechToDatabase(string text)
  {
    if (databaseService != null && !string.IsNullOrWhiteSpace(text))
    {
      databaseService.SaveSpeech(username, text);
    }
  }

  private void CreateSpeechRow(int id, string time, string text, int yOffset)
  {
    // Create timestamp label
    Label lblTime = new Label();
    lblTime.Text = time;
    lblTime.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    lblTime.ForeColor = Color.FromArgb(100, 100, 100);
    lblTime.Location = new Point(10, yOffset);
    lblTime.AutoSize = true;
    lblTime.Name = $"lblTime_{id}";

    // Create text label
    Label lblText = new Label();
    lblText.Text = text;
    lblText.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
    lblText.ForeColor = Color.FromArgb(45, 45, 48);
    lblText.Location = new Point(10, yOffset + 20);
    lblText.AutoSize = true;
    lblText.MaximumSize = new Size(panelSpeechHistory.Width - 80, 0);
    lblText.Name = $"lblText_{id}";

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
    int buttonX = panelSpeechHistory.Width - 50;
    int buttonY = yOffset + 20;
    btnCopy.Location = new Point(buttonX, buttonY);
    
    // Store the text in Tag for easy access
    btnCopy.Tag = text;
    btnCopy.Click += BtnCopy_Click;
    btnCopy.MouseEnter += BtnCopy_MouseEnter;
    btnCopy.MouseLeave += BtnCopy_MouseLeave;

    panelSpeechHistory.Controls.Add(lblTime);
    panelSpeechHistory.Controls.Add(lblText);
    panelSpeechHistory.Controls.Add(btnCopy);
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

    // Move existing controls down
    int textHeight = 40; // Default height
    foreach (Control control in panelSpeechHistory.Controls)
    {
      control.Location = new Point(control.Location.X, control.Location.Y + textHeight + 40);
    }

    // Create the new speech row
    CreateSpeechRow(tempId, time, text, yOffset);

    // Scroll to top
    panelSpeechHistory.AutoScrollPosition = new Point(0, 0);
  }

  protected override void OnFormClosing(FormClosingEventArgs e)
  {
    // Clean up services
    speechService?.Dispose();
    hotkeyManager?.Dispose();
    overlayForm?.Close();
    
    base.OnFormClosing(e);
  }
}
