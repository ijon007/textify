using System.Linq;

namespace WinFormTest.Services;

public class SnippetsPageService
{
    private readonly DatabaseService? databaseService;
    private readonly TranscriptionCorrectionService? transcriptionCorrectionService;
    private readonly string username;
    private readonly Form parentForm;
    private readonly Panel panelSnippetsPage;
    
    private Label? lblSnippetsTitle;
    private Button? btnAddNewSnippet;
    private Panel? panelSnippetsList;
    private Label? lblEmptySnippets;
    private int? editingSnippetId;

    public SnippetsPageService(
        DatabaseService? databaseService,
        TranscriptionCorrectionService? transcriptionCorrectionService,
        string username,
        Form parentForm,
        Panel panelSnippetsPage)
    {
        this.databaseService = databaseService;
        this.transcriptionCorrectionService = transcriptionCorrectionService;
        this.username = username;
        this.parentForm = parentForm;
        this.panelSnippetsPage = panelSnippetsPage;
    }

    public void Initialize()
    {
        panelSnippetsPage.BackColor = Color.White;
        panelSnippetsPage.Dock = DockStyle.Fill;
        panelSnippetsPage.Padding = new Padding(40, 60, 40, 50);

        // Page Title
        lblSnippetsTitle = new Label();
        lblSnippetsTitle.Text = "Snippets";
        lblSnippetsTitle.Font = new Font("Poppins", 24F, FontStyle.Bold, GraphicsUnit.Point);
        lblSnippetsTitle.ForeColor = UIColors.DarkText;
        lblSnippetsTitle.Location = new Point(40, 60);
        lblSnippetsTitle.AutoSize = true;
        lblSnippetsTitle.Name = "lblSnippetsTitle";

        // Add New Button (top-right)
        btnAddNewSnippet = new Button();
        btnAddNewSnippet.Text = "Add new";
        btnAddNewSnippet.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
        btnAddNewSnippet.FlatStyle = FlatStyle.Flat;
        btnAddNewSnippet.FlatAppearance.BorderSize = 0;
        btnAddNewSnippet.BackColor = UIColors.DarkPrimary;
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
        lblEmptySnippets.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
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
        panelSnippetsPage.Paint += (s, e) => UIStylingService.DrawRoundedBorder(panelSnippetsPage, e.Graphics, UILayout.BorderRadius, UIColors.BorderGray);
    }

    private void PanelSnippetsPage_Resize(object? sender, EventArgs e)
    {
        if (panelSnippetsPage != null)
        {
            UIStylingService.ApplyRoundedCorners(panelSnippetsPage, 10);
            
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

    public void LoadSnippetsEntries()
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
        entryPanel.Paint += UIStylingService.DrawEntryPanelBottomBorder;
        entryPanel.AutoSize = false;

        // Create shortcut label
        Label lblShortcut = new Label();
        lblShortcut.Text = shortcut;
        lblShortcut.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
        lblShortcut.ForeColor = UIColors.DarkText;
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
        lblReplacement.Font = new Font("Poppins", 9F, FontStyle.Regular, GraphicsUnit.Point);
        lblReplacement.ForeColor = Color.FromArgb(100, 100, 100);
        lblReplacement.BackColor = Color.White;
        lblReplacement.Location = new Point(10, 32);
        lblReplacement.AutoSize = false;
        lblReplacement.Size = new Size(availableWidth - 200, 20);
        lblReplacement.Name = $"lblReplacement_{id}";
        lblReplacement.TextAlign = ContentAlignment.MiddleLeft;

        // Create edit button
        Button btnEdit = UIStylingService.CreateEditButton(id, entryPanel.Width, 15, BtnEditSnippet_Click, BtnEditSnippet_MouseEnter, BtnEditSnippet_MouseLeave);

        // Create delete button
        Button btnDelete = UIStylingService.CreateDeleteButton(id, entryPanel.Width, 15, BtnDeleteSnippet_Click, BtnDeleteSnippet_MouseEnter, BtnDeleteSnippet_MouseLeave);

        // Add controls to entry panel
        entryPanel.Controls.Add(lblShortcut);
        entryPanel.Controls.Add(lblReplacement);
        entryPanel.Controls.Add(btnEdit);
        entryPanel.Controls.Add(btnDelete);

        // Add entry panel to list
        panelSnippetsList.Controls.Add(entryPanel);
    }


    private void BtnAddNewSnippet_Click(object? sender, EventArgs e)
    {
        ShowSnippetDialog(isAdding: true);
    }

    private void BtnAddNewSnippet_MouseEnter(object? sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            btn.BackColor = UIColors.DarkHover;
        }
    }

    private void BtnAddNewSnippet_MouseLeave(object? sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            btn.BackColor = UIColors.DarkPrimary;
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
            ShowSnippetDialog(isAdding: false, shortcut: snippet.shortcut, replacement: snippet.replacement);
        }
    }

    private void ShowSnippetDialog(bool isAdding, string shortcut = "", string replacement = "")
    {
        if (databaseService == null)
            return;

        DialogResult result = DialogService.ShowSnippetDialog(parentForm, isAdding, out string shortcutText, out string replacementText, shortcut, replacement);

        if (result == DialogResult.OK)
        {
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

                LoadSnippetsEntries();
                // Invalidate snippets cache
                transcriptionCorrectionService?.InvalidateSnippetsCache(username);
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
            btn.BackColor = UIColors.HoverGray;
        }
    }

    private void BtnEditSnippet_MouseLeave(object? sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            btn.BackColor = UIColors.LightGrayBackground;
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
                    LoadSnippetsEntries();
                    // Invalidate snippets cache
                    transcriptionCorrectionService?.InvalidateSnippetsCache(username);
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
            btn.BackColor = UIColors.HoverGray;
        }
    }

    private void BtnDeleteSnippet_MouseLeave(object? sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            btn.BackColor = UIColors.LightGrayBackground;
        }
    }

    public void RefreshSnippetsList()
    {
        LoadSnippetsEntries();
    }
}
