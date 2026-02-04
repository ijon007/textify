using System.Linq;

namespace WinFormTest.Services;

public class DictionaryPageService
{
    private readonly DatabaseService? databaseService;
    private readonly TranscriptionCorrectionService? transcriptionCorrectionService;
    private readonly string username;
    private readonly Form parentForm;
    private readonly Panel panelDictionaryPage;
    
    private Label? lblDictionaryTitle;
    private Button? btnAddNew;
    private Panel? panelDictionaryList;
    private Label? lblEmptyDictionary;
    private int? editingDictionaryId;

    public DictionaryPageService(
        DatabaseService? databaseService,
        TranscriptionCorrectionService? transcriptionCorrectionService,
        string username,
        Form parentForm,
        Panel panelDictionaryPage)
    {
        this.databaseService = databaseService;
        this.transcriptionCorrectionService = transcriptionCorrectionService;
        this.username = username;
        this.parentForm = parentForm;
        this.panelDictionaryPage = panelDictionaryPage;
    }

    public void Initialize()
    {
        panelDictionaryPage.BackColor = Color.White;
        panelDictionaryPage.Dock = DockStyle.Fill;
        panelDictionaryPage.Padding = new Padding(40, 60, 40, 50);

        // Page Title
        lblDictionaryTitle = new Label();
        lblDictionaryTitle.Text = "Dictionary";
        lblDictionaryTitle.Font = new Font("Poppins", 24F, FontStyle.Bold, GraphicsUnit.Point);
        lblDictionaryTitle.ForeColor = UIColors.DarkText;
        lblDictionaryTitle.Location = new Point(40, 60);
        lblDictionaryTitle.AutoSize = true;
        lblDictionaryTitle.Name = "lblDictionaryTitle";

        // Add New Button (top-right)
        btnAddNew = new Button();
        btnAddNew.Text = "Add new";
        btnAddNew.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
        btnAddNew.FlatStyle = FlatStyle.Flat;
        btnAddNew.FlatAppearance.BorderSize = 0;
        btnAddNew.BackColor = UIColors.DarkPrimary;
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
        lblEmptyDictionary.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
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
        panelDictionaryPage.Paint += (s, e) => UIStylingService.DrawRoundedBorder(panelDictionaryPage, e.Graphics, UILayout.BorderRadius, UIColors.BorderGray);
    }

    private void PanelDictionaryPage_Resize(object? sender, EventArgs e)
    {
        if (panelDictionaryPage != null)
        {
            UIStylingService.ApplyRoundedCorners(panelDictionaryPage, 10);
            
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
            int rightPadding = 50;
            int topPadding = 60;
            int maxX = panelDictionaryPage.ClientSize.Width - btnAddNew.Width - rightPadding;
            btnAddNew.Location = new Point(Math.Max(40, maxX), topPadding);
        }
    }

    public void LoadDictionaryEntries()
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
        entryPanel.Paint += UIStylingService.DrawEntryPanelBottomBorder;
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
        Button btnEdit = UIStylingService.CreateEditButton(id, entryPanel.Width, 15, BtnEdit_Click, BtnEdit_MouseEnter, BtnEdit_MouseLeave);

        // Create delete button
        Button btnDelete = UIStylingService.CreateDeleteButton(id, entryPanel.Width, 15, BtnDelete_Click, BtnDelete_MouseEnter, BtnDelete_MouseLeave);

        // Add controls to entry panel
        entryPanel.Controls.Add(lblWord);
        entryPanel.Controls.Add(btnEdit);
        entryPanel.Controls.Add(btnDelete);

        // Add entry panel to list
        panelDictionaryList.Controls.Add(entryPanel);
    }


    private void BtnAddNew_Click(object? sender, EventArgs e)
    {
        ShowDictionaryDialog(isAdding: true);
    }

    private void BtnAddNew_MouseEnter(object? sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            btn.BackColor = UIColors.DarkHover;
        }
    }

    private void BtnAddNew_MouseLeave(object? sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            btn.BackColor = UIColors.DarkPrimary;
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
            ShowDictionaryDialog(isAdding: false, word: entry.word);
        }
    }

    private void ShowDictionaryDialog(bool isAdding, string word = "")
    {
        if (databaseService == null)
            return;

        DialogResult result = DialogService.ShowDictionaryDialog(parentForm, isAdding, out string wordText, word);

        if (result == DialogResult.OK)
        {
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

                LoadDictionaryEntries();
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
            btn.BackColor = UIColors.HoverGray;
        }
    }

    private void BtnEdit_MouseLeave(object? sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            btn.BackColor = UIColors.LightGrayBackground;
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
                    LoadDictionaryEntries();
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
            btn.BackColor = UIColors.HoverGray;
        }
    }

    private void BtnDelete_MouseLeave(object? sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            btn.BackColor = UIColors.LightGrayBackground;
        }
    }

    public void RefreshDictionaryList()
    {
        LoadDictionaryEntries();
    }
}
