namespace WinFormTest.Services;

public class StylePageService
{
    private readonly DatabaseService? databaseService;
    private readonly string username;
    private readonly Panel panelStylePage;
    
    private Label? lblStyleTitle;
    private ClippingPanel? panelFormalCard;
    private ClippingPanel? panelCasualCard;
    private ClippingPanel? panelVeryCasualCard;
    private string? selectedStylePreference;

    public StylePageService(
        DatabaseService? databaseService,
        string username,
        Panel panelStylePage)
    {
        this.databaseService = databaseService;
        this.username = username;
        this.panelStylePage = panelStylePage;
    }

    public void Initialize()
    {
        panelStylePage.BackColor = Color.White;
        panelStylePage.Dock = DockStyle.Fill;
        panelStylePage.Padding = new Padding(40, 60, 40, 50);

        // Page Title
        lblStyleTitle = new Label();
        lblStyleTitle.Text = "Style";
        lblStyleTitle.Font = new Font("Poppins", 24F, FontStyle.Bold, GraphicsUnit.Point);
        lblStyleTitle.ForeColor = Color.FromArgb(45, 45, 48);
        lblStyleTitle.Location = new Point(40, 60);
        lblStyleTitle.AutoSize = true;
        lblStyleTitle.Name = "lblStyleTitle";

        // Load saved preference
        selectedStylePreference = databaseService?.GetUserStylePreference(username) ?? StylePreferences.Default;

        // Create style cards
        CreateStyleCards();

        // Add controls to style page
        panelStylePage.Controls.Add(lblStyleTitle);

        // Handle resize
        panelStylePage.Resize += PanelStylePage_Resize;
        panelStylePage.Paint += (s, e) => UIStylingService.DrawRoundedBorder(panelStylePage, e.Graphics, 10, Color.FromArgb(200, 200, 200));
    }

    private void PanelStylePage_Resize(object? sender, EventArgs e)
    {
        if (panelStylePage != null)
        {
            UIStylingService.ApplyRoundedCorners(panelStylePage, 10);
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
            StylePreferences.Formal,
            new Point(startX, cardsTop),
            cardWidth,
            cardHeight
        );

        // Create Casual card
        panelCasualCard = CreateStyleCard(
            "Casual",
            "Caps + Less punctuation",
            "Hey are you free for lunch tomorrow? Let's do 12 if that works for you",
            StylePreferences.Casual,
            new Point(startX + cardWidth + cardSpacing, cardsTop),
            cardWidth,
            cardHeight
        );

        // Create Very Casual card
        panelVeryCasualCard = CreateStyleCard(
            "very casual",
            "No Caps + Less punctuation",
            "hey are you free for lunch tomorrow? let's do 12 if that works for you",
            StylePreferences.VeryCasual,
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
        lblTitle.Font = new Font("Poppins", 18F, FontStyle.Bold, GraphicsUnit.Point);
        lblTitle.ForeColor = Color.FromArgb(45, 45, 48);
        lblTitle.BackColor = Color.White;
        lblTitle.Location = new Point(padding, padding);
        lblTitle.AutoSize = true;
        lblTitle.Name = $"lbl{styleValue}Title";
        lblTitle.Cursor = Cursors.Hand;
        lblTitle.Tag = styleValue;
        lblTitle.Click += StyleCard_Click;

        // Subtitle label
        Label lblSubtitle = new Label();
        lblSubtitle.Text = subtitle;
        lblSubtitle.Font = new Font("Poppins", 10F, FontStyle.Regular, GraphicsUnit.Point);
        lblSubtitle.ForeColor = Color.FromArgb(100, 100, 100);
        lblSubtitle.BackColor = Color.White;
        lblSubtitle.Location = new Point(padding, padding + 35);
        lblSubtitle.AutoSize = true;
        lblSubtitle.Name = $"lbl{styleValue}Subtitle";
        lblSubtitle.Cursor = Cursors.Hand;
        lblSubtitle.Tag = styleValue;
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
        lblText.Font = new Font("Poppins", 11F, FontStyle.Regular, GraphicsUnit.Point);
        lblText.ForeColor = Color.FromArgb(45, 45, 48);
        lblText.BackColor = Color.FromArgb(245, 245, 245);
        lblText.Location = new Point(12, 12);
        lblText.AutoSize = false;
        lblText.Size = new Size(width - 24, height - 24);
        lblText.TextAlign = ContentAlignment.TopLeft;
        lblText.UseCompatibleTextRendering = false;
        lblText.Cursor = Cursors.Hand;
        lblText.Tag = styleValue;
        lblText.Click += StyleCard_Click;

        // Add controls to bubble
        bubble.Controls.Add(lblText);

        return bubble;
    }

    private void StyleCard_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is ClippingPanel panel && panel.Tag is string styleValue)
        {
            Color borderColor = (selectedStylePreference == styleValue)
                ? Color.Black
                : Color.FromArgb(200, 200, 200);

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save style preference: {ex.Message}");
                MessageBox.Show($"Failed to save style preference: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Update visual selection
            UpdateCardSelection();
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
            panelFormalCard.Refresh();
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

    public string? GetSelectedStylePreference()
    {
        return selectedStylePreference;
    }
}
