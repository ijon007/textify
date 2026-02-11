namespace WinFormTest.Services;

public class HomePageService
{
    private readonly DatabaseService? databaseService;
    private readonly string username;
    private readonly Panel panelHomePage;
    private readonly Panel panelSpeechHistory;
    private readonly Label lblWelcome;
    private readonly Label lblStatWeeks;
    private readonly Label lblStatWords;
    private readonly Label lblStatWPM;

    public HomePageService(
        DatabaseService? databaseService,
        string username,
        Panel panelHomePage,
        Panel panelSpeechHistory,
        Label lblWelcome,
        Label lblStatWeeks,
        Label lblStatWords,
        Label lblStatWPM)
    {
        this.databaseService = databaseService;
        this.username = username;
        this.panelHomePage = panelHomePage;
        this.panelSpeechHistory = panelSpeechHistory;
        this.lblWelcome = lblWelcome;
        this.lblStatWeeks = lblStatWeeks;
        this.lblStatWords = lblStatWords;
        this.lblStatWPM = lblStatWPM;
        
        // Wire up resize handler for home page
        panelHomePage.Resize += PanelHomePage_Resize;
    }

    public void LoadDashboardData()
    {
        // Set welcome message with username
        lblWelcome.Text = $"Welcome back, {username}!";
        
        // Load sample speech entries
        LoadSpeechHistory();
        
        // Load and display statistics
        RefreshStats();
    }

    public void RefreshStats()
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

    public void LoadSpeechHistory()
    {
        if (databaseService == null)
            return;

        panelSpeechHistory.Controls.Clear();

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
        entryPanel.Paint += UIStylingService.DrawEntryPanelBottomBorder;
        entryPanel.AutoSize = false;
        entryPanel.AutoSizeMode = AutoSizeMode.GrowOnly;

        // Create timestamp label
        Label lblTime = new Label();
        lblTime.Text = time;
        lblTime.Font = new Font("Poppins", 9F, FontStyle.Regular, GraphicsUnit.Point);
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
        Font textFont = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
        
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
        btnCopy.Font = new Font("Poppins", 10F, FontStyle.Regular, GraphicsUnit.Point);
        btnCopy.Size = new Size(30, 30);
        btnCopy.FlatStyle = FlatStyle.Flat;
        btnCopy.FlatAppearance.BorderSize = 0;
        btnCopy.FlatAppearance.BorderColor = Color.FromArgb(245, 245, 245);
        btnCopy.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 230, 230);
        btnCopy.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 220, 220);
        btnCopy.BackColor = Color.FromArgb(245, 245, 245);
        btnCopy.ForeColor = Color.FromArgb(100, 100, 100);
        btnCopy.Cursor = Cursors.Hand;
        btnCopy.TabStop = false;
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

    private void PanelHomePage_Resize(object? sender, EventArgs e)
    {
        if (sender is Panel panel && panel == panelHomePage)
        {
            UIStylingService.ApplyRoundedCorners(panel, 10);
            
            // Ensure panelSpeechHistory stays within homePage border bounds
            if (panelSpeechHistory != null)
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
}
