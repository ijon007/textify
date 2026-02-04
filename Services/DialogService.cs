namespace WinFormTest.Services;

public static class DialogService
{
    private static Form CreateBackdrop(Form parent, Form dialog)
    {
        Form backdrop = new Form();
        backdrop.FormBorderStyle = FormBorderStyle.None;
        backdrop.WindowState = FormWindowState.Normal;
        backdrop.StartPosition = FormStartPosition.Manual;
        backdrop.Size = parent.Size;
        backdrop.Location = parent.Location;
        backdrop.BackColor = Color.Black;
        backdrop.Opacity = 0.5;
        backdrop.ShowInTaskbar = false;
        backdrop.TopMost = true;
        backdrop.Enabled = true;
        backdrop.Click += (s, e) => dialog.DialogResult = DialogResult.Cancel;
        return backdrop;
    }

    private static Form CreateDialogForm(Size size)
    {
        Form dialog = new Form();
        dialog.Text = "";
        dialog.FormBorderStyle = FormBorderStyle.None;
        dialog.Size = size;
        dialog.StartPosition = FormStartPosition.CenterParent;
        dialog.BackColor = UIColors.White;
        dialog.ShowInTaskbar = false;
        dialog.TopMost = true;
        return dialog;
    }

    private static Button CreateCloseButton(Form dialog)
    {
        Button btnClose = new Button();
        btnClose.Text = "×";
        btnClose.Font = UIFonts.CloseButton;
        btnClose.FlatStyle = FlatStyle.Flat;
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.BackColor = Color.Transparent;
        btnClose.ForeColor = UIColors.SecondaryText;
        btnClose.Size = UILayout.IconButtonSize;
        btnClose.Location = new Point(460, 10);
        btnClose.Cursor = Cursors.Hand;
        btnClose.TextAlign = ContentAlignment.MiddleCenter;
        btnClose.Click += (s, e) => dialog.DialogResult = DialogResult.Cancel;
        btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = UIColors.DarkText; btnClose.BackColor = UIColors.LightGrayBackground; };
        btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = UIColors.SecondaryText; btnClose.BackColor = Color.Transparent; };
        return btnClose;
    }

    private static Button CreateCancelButton(int x, int y)
    {
        Button btnCancel = new Button();
        btnCancel.Text = "Cancel";
        btnCancel.Location = new Point(x, y);
        btnCancel.Size = UILayout.DialogButtonSize;
        btnCancel.Font = UIFonts.Body;
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.BackColor = UIColors.LightButtonBackground;
        btnCancel.ForeColor = UIColors.DarkText;
        btnCancel.Cursor = Cursors.Hand;
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.TextAlign = ContentAlignment.MiddleCenter;
        btnCancel.MouseEnter += (s, e) => btnCancel.BackColor = UIColors.LightButtonHover;
        btnCancel.MouseLeave += (s, e) => btnCancel.BackColor = UIColors.LightButtonBackground;
        return btnCancel;
    }

    private static Button CreateSaveButton(int x, int y, bool isAdding)
    {
        Button btnSave = new Button();
        btnSave.Text = isAdding ? "Add" : "Save changes";
        btnSave.Location = new Point(x, y);
        btnSave.Size = UILayout.DialogSaveButtonSize;
        btnSave.Font = UIFonts.Body;
        btnSave.FlatStyle = FlatStyle.Flat;
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.BackColor = UIColors.DarkPrimary;
        btnSave.ForeColor = Color.White;
        btnSave.Cursor = Cursors.Hand;
        btnSave.DialogResult = DialogResult.OK;
        btnSave.TextAlign = ContentAlignment.MiddleCenter;
        btnSave.MouseEnter += (s, e) => btnSave.BackColor = UIColors.DarkHover;
        btnSave.MouseLeave += (s, e) => btnSave.BackColor = UIColors.DarkPrimary;
        return btnSave;
    }

    private static void SetupDialogBorder(Form dialog)
    {
        dialog.Paint += (s, e) =>
        {
            using (Pen pen = new Pen(UIColors.BorderGray, 1))
            {
                Rectangle rect = new Rectangle(0, 0, dialog.Width - 1, dialog.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        };
    }

    private static void ShowDialogWithBackdrop(Form parent, Form backdrop, Form dialog)
    {
        backdrop.Show();
        backdrop.BringToFront();
        dialog.BringToFront();
        
        dialog.FormClosed += (s, e) => 
        {
            backdrop.Close();
            backdrop.Dispose();
        };
    }
    public static DialogResult ShowDictionaryDialog(Form parent, bool isAdding, out string word, string initialWord = "")
    {
        word = "";
        
        Form dialog = CreateDialogForm(new Size(500, 200));
        Form backdrop = CreateBackdrop(parent, dialog);
        
        // Title label
        Label lblTitle = new Label();
        lblTitle.Text = isAdding ? "Add new word" : "Edit word";
        lblTitle.Font = UIFonts.Heading;
        lblTitle.ForeColor = UIColors.DarkText;
        lblTitle.Location = new Point(20, 20);
        lblTitle.AutoSize = true;

        // Close button
        Button btnClose = CreateCloseButton(dialog);

        // Text input
        TextBox txtWord = new TextBox();
        txtWord.Location = new Point(20, 70);
        txtWord.Size = new Size(460, 25);
        txtWord.Font = UIFonts.Body;
        txtWord.BorderStyle = BorderStyle.FixedSingle;
        txtWord.Text = initialWord;
        txtWord.SelectAll();

        // Cancel and Save buttons
        Button btnCancel = CreateCancelButton(290, 130);
        Button btnSave = CreateSaveButton(380, 130, isAdding);

        // Add controls
        dialog.Controls.Add(lblTitle);
        dialog.Controls.Add(btnClose);
        dialog.Controls.Add(txtWord);
        dialog.Controls.Add(btnCancel);
        dialog.Controls.Add(btnSave);

        // Setup border and show
        SetupDialogBorder(dialog);
        dialog.AcceptButton = btnSave;
        dialog.CancelButton = btnCancel;
        txtWord.Focus();
        ShowDialogWithBackdrop(parent, backdrop, dialog);

        DialogResult result = dialog.ShowDialog(parent);
        if (result == DialogResult.OK)
        {
            word = txtWord.Text.Trim();
        }
        
        dialog.Dispose();
        return result;
    }

    public static DialogResult ShowSnippetDialog(Form parent, bool isAdding, out string shortcut, out string replacement, string initialShortcut = "", string initialReplacement = "")
    {
        shortcut = "";
        replacement = "";
        
        Form dialog = CreateDialogForm(new Size(500, 280));
        Form backdrop = CreateBackdrop(parent, dialog);
        
        // Title label
        Label lblTitle = new Label();
        lblTitle.Text = isAdding ? "Add new snippet" : "Edit snippet";
        lblTitle.Font = UIFonts.Heading;
        lblTitle.ForeColor = UIColors.DarkText;
        lblTitle.Location = new Point(20, 20);
        lblTitle.AutoSize = true;

        // Close button
        Button btnClose = CreateCloseButton(dialog);

        // Shortcut label
        Label lblShortcutLabel = new Label();
        lblShortcutLabel.Text = "Shortcut word:";
        lblShortcutLabel.Font = UIFonts.Medium;
        lblShortcutLabel.ForeColor = UIColors.DarkText;
        lblShortcutLabel.Location = new Point(17, 60);
        lblShortcutLabel.AutoSize = true;

        // Shortcut input
        TextBox txtShortcut = new TextBox();
        txtShortcut.Location = new Point(20, 80);
        txtShortcut.Size = new Size(460, 25);
        txtShortcut.Font = UIFonts.Body;
        txtShortcut.BorderStyle = BorderStyle.FixedSingle;
        txtShortcut.Text = initialShortcut;
        if (isAdding)
            txtShortcut.SelectAll();

        // Replacement label
        Label lblReplacementLabel = new Label();
        lblReplacementLabel.Text = "Replacement text:";
        lblReplacementLabel.Font = UIFonts.Medium;
        lblReplacementLabel.ForeColor = UIColors.DarkText;
        lblReplacementLabel.Location = new Point(17, 115);
        lblReplacementLabel.AutoSize = true;

        // Replacement input (multiline for longer text)
        TextBox txtReplacement = new TextBox();
        txtReplacement.Location = new Point(20, 135);
        txtReplacement.Size = new Size(460, 60);
        txtReplacement.Font = UIFonts.Body;
        txtReplacement.BorderStyle = BorderStyle.FixedSingle;
        txtReplacement.Multiline = true;
        txtReplacement.Text = initialReplacement;
        txtReplacement.ScrollBars = ScrollBars.None;

        // Cancel and Save buttons
        Button btnCancel = CreateCancelButton(290, 210);
        Button btnSave = CreateSaveButton(380, 210, isAdding);

        // Add controls
        dialog.Controls.Add(lblTitle);
        dialog.Controls.Add(btnClose);
        dialog.Controls.Add(lblShortcutLabel);
        dialog.Controls.Add(txtShortcut);
        dialog.Controls.Add(lblReplacementLabel);
        dialog.Controls.Add(txtReplacement);
        dialog.Controls.Add(btnCancel);
        dialog.Controls.Add(btnSave);

        // Setup border and show
        SetupDialogBorder(dialog);
        dialog.AcceptButton = btnSave;
        dialog.CancelButton = btnCancel;
        txtShortcut.Focus();
        ShowDialogWithBackdrop(parent, backdrop, dialog);

        DialogResult result = dialog.ShowDialog(parent);
        if (result == DialogResult.OK)
        {
            shortcut = txtShortcut.Text.Trim();
            replacement = txtReplacement.Text.Trim();
        }
        
        dialog.Dispose();
        return result;
    }
}
