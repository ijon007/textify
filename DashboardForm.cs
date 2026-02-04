using Microsoft.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Linq;
using WinFormTest.Services;

namespace WinFormTest;

public partial class DashboardForm : Form
{
  [DllImport("user32.dll")]
  private static extern int ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

  private const int SB_VERT = 1;

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
  private Panel? panelSettingsContent;
  private Label? lblSettingsTitle;
  private Label? lblHotkeySectionTitle;
  private Label? lblCurrentHotkey;
  private Label? lblCurrentHotkeyValue;
  private Button? btnChangeHotkey;
  private Label? lblHotkeyDescription;
  
  // Microphone Settings
  private Label? lblMicrophoneSectionTitle;
  private Label? lblMicrophoneDevice;
  private ComboBox? cmbMicrophoneDevice;
  
  // Overlay Settings
  private Label? lblOverlaySectionTitle;
  private Label? lblOverlayPosition;
  private ComboBox? cmbOverlayPosition;
  
  // Application Behavior
  private Label? lblApplicationSectionTitle;
  private CheckBox? chkStartMinimized;
  private CheckBox? chkMinimizeToTray;
  private NotifyIcon? notifyIcon;
  
  
  // Data Management
  private Label? lblDataSectionTitle;
  private Button? btnClearSpeechHistory;
  private Button? btnClearDictionary;
  private Button? btnClearSnippets;
  private Button? btnExportData;

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
      panel.Paint += (s, e) => UIStylingService.DrawRoundedBorder(panel, e.Graphics, 10, Color.FromArgb(200, 200, 200));
      panel.Resize += (s, e) => UIStylingService.ApplyRoundedCorners(panel, 10);
      UIStylingService.ApplyRoundedCorners(panel, 10);
    }
    
    panelSettingsPage.Paint += (s, e) =>
    {
      UIStylingService.DrawRoundedBorder(panelSettingsPage, e.Graphics, 10, Color.FromArgb(200, 200, 200));
      if (panelSettingsContent != null)
        UIStylingService.HideScrollbar(panelSettingsContent);
    };
    panelSettingsPage.Resize += (s, e) => UIStylingService.ApplyRoundedCorners(panelSettingsPage, 10);
    UIStylingService.ApplyRoundedCorners(panelSettingsPage, 10);
    
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

      // Initialize transcription correction service
      transcriptionCorrectionService = new TranscriptionCorrectionService(databaseService);

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
        catch
        {
          // Use defaults if loading fails
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

  private void CreatePlaceholderPage(Panel panel, string title)
  {
    panel.BackColor = Color.White;
    panel.Dock = DockStyle.Fill;
    panel.Padding = new Padding(40, 60, 40, 40);
    
    Label lblTitle = new Label();
    lblTitle.Text = title;
    lblTitle.Font = new Font("Poppins", 24F, FontStyle.Bold, GraphicsUnit.Point);
    lblTitle.ForeColor = Color.FromArgb(45, 45, 48);
    lblTitle.Location = new Point(40, 60);
    lblTitle.AutoSize = true;
    lblTitle.Name = $"lbl{title}Title";
    
    Label lblPlaceholder = new Label();
    lblPlaceholder.Text = $"This is the {title} page.\nContent will be added here.";
    lblPlaceholder.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
    lblPlaceholder.ForeColor = Color.FromArgb(100, 100, 100);
    lblPlaceholder.Location = new Point(40, 120);
    lblPlaceholder.AutoSize = true;
    lblPlaceholder.Name = $"lbl{title}Placeholder";
    
    panel.Controls.Add(lblTitle);
    panel.Controls.Add(lblPlaceholder);
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
    lblWord.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
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
    btnEdit.Font = new Font("Poppins", 10F, FontStyle.Regular, GraphicsUnit.Point);
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
    btnDelete.Font = new Font("Poppins", 10F, FontStyle.Regular, GraphicsUnit.Point);
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
    lblTitle.Font = new Font("Poppins", 16F, FontStyle.Bold, GraphicsUnit.Point);
    lblTitle.ForeColor = Color.FromArgb(45, 45, 48);
    lblTitle.Location = new Point(20, 20);
    lblTitle.AutoSize = true;

    // Close button
    Button btnClose = new Button();
    btnClose.Text = "×";
    btnClose.Font = new Font("Poppins", 18F, FontStyle.Regular, GraphicsUnit.Point);
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
    txtWord.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
    txtWord.BorderStyle = BorderStyle.FixedSingle;
    txtWord.Text = word;
    txtWord.SelectAll();

    // Cancel button
    Button btnCancel = new Button();
    btnCancel.Text = "Cancel";
    btnCancel.Location = new Point(290, 130);
    btnCancel.Size = new Size(80, 28);
    btnCancel.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
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
    btnSave.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
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

        dictionaryPageService?.RefreshDictionaryList();
        // Invalidate dictionary cache
        transcriptionCorrectionService?.InvalidateDictionaryCache(username);
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
          dictionaryPageService?.RefreshDictionaryList();
          // Invalidate dictionary cache
          transcriptionCorrectionService?.InvalidateDictionaryCache(username);
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
}