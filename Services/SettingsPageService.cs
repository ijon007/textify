using System.Linq;

namespace WinFormTest.Services;

public class SettingsPageService
{
    private readonly DatabaseService? databaseService;
    private readonly GlobalHotkeyManager? hotkeyManager;
    private readonly SpeechRecognitionService? speechService;
    private readonly SpeechOverlayForm? overlayForm;
    private readonly DictionaryPageService? dictionaryPageService;
    private readonly SnippetsPageService? snippetsPageService;
    private readonly TranscriptionCorrectionService? transcriptionCorrectionService;
    private readonly string username;
    private readonly Form parentForm;
    private readonly Panel panelSettingsPage;
    
    private Panel? panelSettingsContent;
    private Label? lblSettingsTitle;
    private Label? lblHotkeySectionTitle;
    private Label? lblCurrentHotkey;
    private Label? lblCurrentHotkeyValue;
    private Button? btnChangeHotkey;
    private Label? lblHotkeyDescription;
    private Label? lblMicrophoneSectionTitle;
    private Label? lblMicrophoneDevice;
    private ComboBox? cmbMicrophoneDevice;
    private Label? lblOverlaySectionTitle;
    private Label? lblOverlayPosition;
    private ComboBox? cmbOverlayPosition;
    private Label? lblApplicationSectionTitle;
    private CheckBox? chkStartMinimized;
    private CheckBox? chkMinimizeToTray;
    private Label? lblDataSectionTitle;
    private Button? btnClearSpeechHistory;
    private Button? btnClearDictionary;
    private Button? btnClearSnippets;
    private Button? btnExportData;

    public SettingsPageService(
        DatabaseService? databaseService,
        GlobalHotkeyManager? hotkeyManager,
        SpeechRecognitionService? speechService,
        SpeechOverlayForm? overlayForm,
        DictionaryPageService? dictionaryPageService,
        SnippetsPageService? snippetsPageService,
        TranscriptionCorrectionService? transcriptionCorrectionService,
        string username,
        Form parentForm,
        Panel panelSettingsPage)
    {
        this.databaseService = databaseService;
        this.hotkeyManager = hotkeyManager;
        this.speechService = speechService;
        this.overlayForm = overlayForm;
        this.dictionaryPageService = dictionaryPageService;
        this.snippetsPageService = snippetsPageService;
        this.transcriptionCorrectionService = transcriptionCorrectionService;
        this.username = username;
        this.parentForm = parentForm;
        this.panelSettingsPage = panelSettingsPage;
    }

    public void Initialize()
    {
        // This is a simplified version - the full implementation would include all UI initialization
        // For now, we'll keep the settings page initialization in DashboardForm but extract key methods
        // The full refactoring can be done incrementally
        panelSettingsPage.Paint += (s, e) =>
        {
            UIStylingService.DrawRoundedBorder(panelSettingsPage, e.Graphics, 10, Color.FromArgb(200, 200, 200));
            if (panelSettingsContent != null)
                UIStylingService.HideScrollbar(panelSettingsContent);
        };
        
        panelSettingsPage.Resize += PanelSettingsPage_Resize;
    }

    public void SetControls(
        Panel? panelSettingsContent,
        Label? lblSettingsTitle,
        Label? lblHotkeySectionTitle,
        Label? lblCurrentHotkey,
        Label? lblCurrentHotkeyValue,
        Button? btnChangeHotkey,
        Label? lblHotkeyDescription,
        Label? lblMicrophoneSectionTitle,
        Label? lblMicrophoneDevice,
        ComboBox? cmbMicrophoneDevice,
        Label? lblOverlaySectionTitle,
        Label? lblOverlayPosition,
        ComboBox? cmbOverlayPosition,
        Label? lblApplicationSectionTitle,
        CheckBox? chkStartMinimized,
        CheckBox? chkMinimizeToTray,
        Label? lblDataSectionTitle,
        Button? btnClearSpeechHistory,
        Button? btnClearDictionary,
        Button? btnClearSnippets,
        Button? btnExportData)
    {
        this.panelSettingsContent = panelSettingsContent;
        this.lblSettingsTitle = lblSettingsTitle;
        this.lblHotkeySectionTitle = lblHotkeySectionTitle;
        this.lblCurrentHotkey = lblCurrentHotkey;
        this.lblCurrentHotkeyValue = lblCurrentHotkeyValue;
        this.btnChangeHotkey = btnChangeHotkey;
        this.lblHotkeyDescription = lblHotkeyDescription;
        this.lblMicrophoneSectionTitle = lblMicrophoneSectionTitle;
        this.lblMicrophoneDevice = lblMicrophoneDevice;
        this.cmbMicrophoneDevice = cmbMicrophoneDevice;
        this.lblOverlaySectionTitle = lblOverlaySectionTitle;
        this.lblOverlayPosition = lblOverlayPosition;
        this.cmbOverlayPosition = cmbOverlayPosition;
        this.lblApplicationSectionTitle = lblApplicationSectionTitle;
        this.chkStartMinimized = chkStartMinimized;
        this.chkMinimizeToTray = chkMinimizeToTray;
        this.lblDataSectionTitle = lblDataSectionTitle;
        this.btnClearSpeechHistory = btnClearSpeechHistory;
        this.btnClearDictionary = btnClearDictionary;
        this.btnClearSnippets = btnClearSnippets;
        this.btnExportData = btnExportData;
        
        // Wire up event handlers if controls are provided
        if (btnChangeHotkey != null)
        {
            btnChangeHotkey.Click += BtnChangeHotkey_Click;
        }
        if (cmbMicrophoneDevice != null)
        {
            cmbMicrophoneDevice.SelectedIndexChanged += CmbMicrophoneDevice_SelectedIndexChanged;
        }
        if (cmbOverlayPosition != null)
        {
            cmbOverlayPosition.SelectedIndexChanged += CmbOverlayPosition_SelectedIndexChanged;
        }
        if (chkStartMinimized != null)
        {
            chkStartMinimized.CheckedChanged += ChkStartMinimized_CheckedChanged;
        }
        if (chkMinimizeToTray != null)
        {
            chkMinimizeToTray.CheckedChanged += ChkMinimizeToTray_CheckedChanged;
        }
        if (btnClearSpeechHistory != null)
        {
            btnClearSpeechHistory.Click += BtnClearSpeechHistory_Click;
        }
        if (btnClearDictionary != null)
        {
            btnClearDictionary.Click += BtnClearDictionary_Click;
        }
        if (btnClearSnippets != null)
        {
            btnClearSnippets.Click += BtnClearSnippets_Click;
        }
        if (btnExportData != null)
        {
            btnExportData.Click += BtnExportData_Click;
        }
    }

    public CheckBox? GetMinimizeToTrayCheckbox()
    {
        return chkMinimizeToTray;
    }

    private void PanelSettingsPage_Resize(object? sender, EventArgs e)
    {
        if (panelSettingsPage != null && panelSettingsContent != null)
        {
            int paddingLeft = 40;
            int paddingRight = 40;
            int paddingTop = 60;
            int paddingBottom = 50;
            int availableHeight = panelSettingsPage.ClientSize.Height - paddingTop - paddingBottom;
            int availableWidth = panelSettingsPage.ClientSize.Width - paddingLeft - paddingRight;
            
            panelSettingsContent.Height = Math.Max(0, availableHeight);
            panelSettingsContent.Top = paddingTop;
            panelSettingsContent.Left = paddingLeft;
            panelSettingsContent.Width = Math.Max(0, availableWidth);
            
            UIStylingService.HideScrollbar(panelSettingsContent);
        }
        
        if (lblHotkeyDescription != null && panelSettingsContent != null)
        {
            int maxWidth = panelSettingsContent.ClientSize.Width - 80;
            lblHotkeyDescription.MaximumSize = new Size(maxWidth, 0);
        }
    }

    // Note: The actual implementation of these methods would be extracted from DashboardForm
    // For now, these are placeholders that can be implemented incrementally
    private void BtnChangeHotkey_Click(object? sender, EventArgs e) { }
    private void CmbMicrophoneDevice_SelectedIndexChanged(object? sender, EventArgs e) { }
    private void CmbOverlayPosition_SelectedIndexChanged(object? sender, EventArgs e) { }
    private void ChkStartMinimized_CheckedChanged(object? sender, EventArgs e) { }
    private void ChkMinimizeToTray_CheckedChanged(object? sender, EventArgs e) { }
    
    private void BtnClearSpeechHistory_Click(object? sender, EventArgs e)
    {
        if (databaseService == null)
            return;

        DialogResult result = MessageBox.Show(
            "Are you sure you want to clear all speech history? This action cannot be undone.",
            "Confirm Clear",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            try
            {
                databaseService.ClearAllSpeechHistory(username);
                MessageBox.Show("Speech history cleared successfully.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to clear speech history: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void BtnClearDictionary_Click(object? sender, EventArgs e)
    {
        if (databaseService == null)
            return;

        DialogResult result = MessageBox.Show(
            "Are you sure you want to clear all dictionary entries? This action cannot be undone.",
            "Confirm Clear",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            try
            {
                databaseService.ClearAllDictionary(username);
                dictionaryPageService?.RefreshDictionaryList();
                transcriptionCorrectionService?.InvalidateDictionaryCache(username);
                MessageBox.Show("Dictionary cleared successfully.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to clear dictionary: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void BtnClearSnippets_Click(object? sender, EventArgs e)
    {
        if (databaseService == null)
            return;

        DialogResult result = MessageBox.Show(
            "Are you sure you want to clear all snippets? This action cannot be undone.",
            "Confirm Clear",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            try
            {
                databaseService.ClearAllSnippets(username);
                snippetsPageService?.RefreshSnippetsList();
                transcriptionCorrectionService?.InvalidateSnippetsCache(username);
                MessageBox.Show("Snippets cleared successfully.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to clear snippets: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void BtnExportData_Click(object? sender, EventArgs e)
    {
        if (databaseService == null)
            return;

        try
        {
            string jsonData = databaseService.ExportUserData(username);
            
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            saveDialog.FileName = $"WinFormTest_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveDialog.FileName, jsonData);
                MessageBox.Show($"Data exported successfully to:\n{saveDialog.FileName}", "Export Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export data: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
