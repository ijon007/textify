namespace WinFormTest.Services;

public static class DialogService
{
    public static DialogResult ShowDictionaryDialog(Form parent, bool isAdding, out string word, string initialWord = "")
    {
        word = "";
        
        // Create backdrop overlay Form (separate window)
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
        txtWord.Text = initialWord;
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
        
        // Create backdrop overlay Form (separate window)
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
        btnClose.Click += (s, ev) => dialog.DialogResult = DialogResult.Cancel;
        btnClose.MouseEnter += (s, ev) => { btnClose.ForeColor = Color.FromArgb(45, 45, 48); btnClose.BackColor = Color.FromArgb(245, 245, 245); };
        btnClose.MouseLeave += (s, ev) => { btnClose.ForeColor = Color.FromArgb(100, 100, 100); btnClose.BackColor = Color.Transparent; };

        // Shortcut label
        Label lblShortcutLabel = new Label();
        lblShortcutLabel.Text = "Shortcut word:";
        lblShortcutLabel.Font = new Font("Poppins", 10F, FontStyle.Regular, GraphicsUnit.Point);
        lblShortcutLabel.ForeColor = Color.FromArgb(45, 45, 48);
        lblShortcutLabel.Location = new Point(17, 60);
        lblShortcutLabel.AutoSize = true;

        // Shortcut input
        TextBox txtShortcut = new TextBox();
        txtShortcut.Location = new Point(20, 80);
        txtShortcut.Size = new Size(460, 25);
        txtShortcut.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
        txtShortcut.BorderStyle = BorderStyle.FixedSingle;
        txtShortcut.Text = initialShortcut;
        if (isAdding)
            txtShortcut.SelectAll();

        // Replacement label
        Label lblReplacementLabel = new Label();
        lblReplacementLabel.Text = "Replacement text:";
        lblReplacementLabel.Font = new Font("Poppins", 10F, FontStyle.Regular, GraphicsUnit.Point);
        lblReplacementLabel.ForeColor = Color.FromArgb(45, 45, 48);
        lblReplacementLabel.Location = new Point(17, 115);
        lblReplacementLabel.AutoSize = true;

        // Replacement input (multiline for longer text)
        TextBox txtReplacement = new TextBox();
        txtReplacement.Location = new Point(20, 135);
        txtReplacement.Size = new Size(460, 60);
        txtReplacement.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
        txtReplacement.BorderStyle = BorderStyle.FixedSingle;
        txtReplacement.Multiline = true;
        txtReplacement.Text = initialReplacement;
        txtReplacement.ScrollBars = ScrollBars.None;

        // Cancel button
        Button btnCancel = new Button();
        btnCancel.Text = "Cancel";
        btnCancel.Location = new Point(290, 210);
        btnCancel.Size = new Size(80, 28);
        btnCancel.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
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
        btnSave.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
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
