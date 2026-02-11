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
    private DataGridView? dgvSnippets;
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

        // DataGridView for snippets (with action buttons)
        dgvSnippets = new DataGridView();
        dgvSnippets.Name = "dgvSnippets";
        dgvSnippets.BackgroundColor = Color.White;
        dgvSnippets.BorderStyle = BorderStyle.None;
        dgvSnippets.Dock = DockStyle.Fill;
        dgvSnippets.AllowUserToAddRows = false;
        dgvSnippets.AllowUserToDeleteRows = false;
        dgvSnippets.ReadOnly = true;
        dgvSnippets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvSnippets.MultiSelect = false;
        dgvSnippets.RowHeadersVisible = false;
        dgvSnippets.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvSnippets.Font = new Font("Poppins", 9F, FontStyle.Regular, GraphicsUnit.Point);
        dgvSnippets.ColumnHeadersHeight = 36;
        dgvSnippets.RowTemplate.Height = 44;
        dgvSnippets.DefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 240, 240);
        dgvSnippets.DefaultCellStyle.SelectionForeColor = UIColors.DarkText;
        dgvSnippets.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
        dgvSnippets.ColumnHeadersDefaultCellStyle.ForeColor = UIColors.DarkText;
        dgvSnippets.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 248, 248);
        dgvSnippets.ColumnHeadersDefaultCellStyle.SelectionForeColor = UIColors.DarkText;
        dgvSnippets.ColumnHeadersDefaultCellStyle.Font = new Font("Poppins", 10F, FontStyle.Regular, GraphicsUnit.Point);
        dgvSnippets.EnableHeadersVisualStyles = false;
        dgvSnippets.CellContentClick += DgvSnippets_CellContentClick;

        var colId = new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Id", Visible = false };
        var colShortcut = new DataGridViewTextBoxColumn { Name = "Shortcut", HeaderText = "Shortcut", ReadOnly = true };
        var colReplacement = new DataGridViewTextBoxColumn { Name = "Replacement", HeaderText = "Replacement", ReadOnly = true };
        var colEdit = new DataGridViewButtonColumn { Name = "Edit", HeaderText = "", Text = "✏️", UseColumnTextForButtonValue = true, Width = 50 };
        var colDelete = new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "🗑️", UseColumnTextForButtonValue = true, Width = 50 };

        dgvSnippets.Columns.Add(colId);
        dgvSnippets.Columns.Add(colShortcut);
        dgvSnippets.Columns.Add(colReplacement);
        dgvSnippets.Columns.Add(colEdit);
        dgvSnippets.Columns.Add(colDelete);

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
        if (databaseService == null || panelSnippetsList == null || dgvSnippets == null)
            return;

        panelSnippetsList.Controls.Clear();

        var snippets = databaseService.GetSnippets(username);

        if (snippets.Count == 0)
        {
            if (lblEmptySnippets != null)
            {
                panelSnippetsList.Controls.Add(lblEmptySnippets);
                lblEmptySnippets.Location = new Point((panelSnippetsList.Width - lblEmptySnippets.Width) / 2, 50);
                lblEmptySnippets.Show();
            }
            return;
        }

        lblEmptySnippets?.Hide();
        dgvSnippets.Rows.Clear();
        foreach (var s in snippets)
            dgvSnippets.Rows.Add(s.id, s.shortcut, s.replacement, "✏️", "🗑️");
        panelSnippetsList.Controls.Add(dgvSnippets);
    }

    private void DgvSnippets_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (dgvSnippets == null || e.RowIndex < 0)
            return;
        var row = dgvSnippets.Rows[e.RowIndex];
        if (row.Cells["Id"].Value is not int id)
            return;
        if (e.ColumnIndex == dgvSnippets.Columns["Edit"].Index)
            EditSnippetById(id);
        else if (e.ColumnIndex == dgvSnippets.Columns["Delete"].Index)
            DeleteSnippetById(id);
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
            EditSnippetById(id);
    }

    private void EditSnippetById(int id)
    {
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
        if (sender is Button btn && btn.Tag is int id)
            DeleteSnippetById(id);
    }

    private void DeleteSnippetById(int id)
    {
        if (databaseService == null)
            return;
        var snippets = databaseService.GetSnippets(username);
        var snippet = snippets.FirstOrDefault(s => s.id == id);
        string shortcutText = snippet.id == id ? snippet.shortcut : "this snippet";
        DialogResult result = MessageBox.Show(
            $"Are you sure you want to delete \"{shortcutText}\"?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
            return;
        try
        {
            databaseService.DeleteSnippet(id, username);
            LoadSnippetsEntries();
            transcriptionCorrectionService?.InvalidateSnippetsCache(username);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete snippet: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
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
